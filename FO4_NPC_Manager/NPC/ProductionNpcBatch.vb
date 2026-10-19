Option Strict On
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Text.Json
Imports System.Security.Cryptography
Imports FO4_Base_Library

Namespace ExperimentalTransformation
    Public Class NpcBatchProfile
        Public Property DataRoot As String = ""
        Public Property LoadOrder As New List(Of String)
        ' Include every original game/mod/save root. No installation is performed.
        Public Property ProtectedRoots As New List(Of String)
        Public Property RequireFaceGen As Boolean = True
        Public Property Targets As New List(Of NpcBatchTarget)
    End Class
    Public Class NpcBatchTarget
        Public Property SourcePlugin As String = ""
        Public Property ObjectId As UInteger
        Public Property Age As AgeEvidence = AgeEvidence.Unknown
        Public Property AdultEvidence As String = ""
        Public Property AppearancePreset As String = ""
        Public Property HumanRacePlugin As String = ""
        Public Property HumanRaceObjectId As UInteger
        Public ReadOnly Property Key As String
            Get
                Return SourcePlugin.ToUpperInvariant() & "|" & ObjectId.ToString("X6")
            End Get
        End Property
    End Class
    Public Class NpcBatchItemResult
        Public Property Key As String = ""
        Public Property Status As String = ""
        Public Property Detail As String = ""
        Public Property AttemptDirectory As String = ""
    End Class
    Public Class NpcBatchResult
        Public Property Status As String = "EXPERIMENTAL-NOT-GAME-VALIDATED"
        Public Property StagingDirectory As String = ""
        Public Property ProfileSha256 As String = ""
        Public Property Items As New List(Of NpcBatchItemResult)
    End Class

    ' Dedicated headless process only: original renderer/index/output context is process-global.
    ' One isolated mod shard per NPC; aggregate ESP consolidation is a subsequent milestone.
    Public Module ProductionNpcBatch
        Private ReadOnly processGate As New Object
        Public Function Run(profile As NpcBatchProfile, stagingParent As String, Optional resumeStage As String = Nothing) As NpcBatchResult
            SyncLock processGate
                Return RunExclusive(profile, stagingParent, resumeStage)
            End SyncLock
        End Function
        Private Function RunExclusive(profile As NpcBatchProfile, stagingParent As String, resumeStage As String) As NpcBatchResult
            If profile Is Nothing OrElse profile.Targets Is Nothing OrElse profile.Targets.Count = 0 OrElse profile.Targets.Count > 25 Then Throw New ArgumentException("Expected 1..25 explicit targets.")
            If profile.Targets.Any(Function(t) t Is Nothing) OrElse profile.Targets.Select(Function(t) t.Key).Distinct().Count() <> profile.Targets.Count Then Throw New ArgumentException("Duplicate or null targets.")
            If profile.LoadOrder Is Nothing OrElse profile.LoadOrder.Count = 0 OrElse profile.LoadOrder.Distinct(StringComparer.OrdinalIgnoreCase).Count() <> profile.LoadOrder.Count Then Throw New ArgumentException("Unique explicit load order required.")
            If profile.ProtectedRoots Is Nothing OrElse profile.ProtectedRoots.Count = 0 Then Throw New ArgumentException("Explicit protected roots required.")
            Dim roots = profile.ProtectedRoots.Concat({profile.DataRoot}).ToList()
            Dim identity As New StringBuilder(JsonSerializer.Serialize(profile))
            For Each name In profile.LoadOrder
                If Path.GetFileName(name) <> name OrElse String.IsNullOrWhiteSpace(name) OrElse name.Contains("/") OrElse name.Contains("\") Then Throw New ArgumentException("Plugin basenames required.")
                SealInput(Path.Combine(profile.DataRoot, name), identity)
            Next
            For Each target In profile.Targets
                If Not String.IsNullOrWhiteSpace(target.AppearancePreset) Then
                    SealInput(target.AppearancePreset, identity)
                    roots.Add(Path.GetFullPath(target.AppearancePreset))
                End If
            Next
            Dim seal = Encoding.UTF8.GetBytes(identity.ToString())
            Dim journal = If(String.IsNullOrEmpty(resumeStage), BatchJournal.Create(stagingParent, roots, seal), BatchJournal.Recover(resumeStage, seal))
            Dim result As New NpcBatchResult With {.StagingDirectory = journal.StagingDirectory, .ProfileSha256 = journal.ProfileSha256}
            Dim priorGame = Config_App.Current.Game
            Try
                Config_App.Current.Game = Config_App.Game_Enum.Fallout4
                For Each target In profile.Targets
                    Dim item As New NpcBatchItemResult With {.Key = target.Key}
                    result.Items.Add(item)
                    If journal.ContainsVerifiedCheckpoint(target.Key) Then
                        item.Status = "RESUMED-VERIFIED-OUTPUTS"
                        Continue For
                    End If
                    Dim attempt = Path.Combine(journal.StagingDirectory, "attempt-" & Guid.NewGuid().ToString("N"))
                    item.AttemptDirectory = attempt
                    Try
                        If profile.RequireFaceGen AndAlso String.IsNullOrWhiteSpace(target.AppearancePreset) Then Throw New InvalidOperationException("FaceGen requires an explicit female appearance preset.")
                        Dim patch As String
                        If String.IsNullOrWhiteSpace(target.AppearancePreset) Then
                            patch = IsolatedNpcPatch.WriteRecordOnlyPatch(profile.DataRoot, profile.LoadOrder, target.SourcePlugin, target.ObjectId,
                                attempt, target.Age, target.AdultEvidence)
                        Else
                            patch = IsolatedNpcPatch.WriteAppearancePatch(profile.DataRoot, profile.LoadOrder, target.SourcePlugin, target.ObjectId,
                                attempt, target.Age, target.AdultEvidence, target.AppearancePreset, target.HumanRacePlugin, target.HumanRaceObjectId)
                        End If
                        Dim outputs As New List(Of String) From {patch, Path.Combine(attempt, "verification.json")}
                        If profile.RequireFaceGen Then
                            outputs.AddRange(BakeStaged(profile, target, patch, attempt))
                            item.Status = "FACEGEN-GENERATED-NOT-GAME-VALIDATED"
                        Else
                            item.Status = If(String.IsNullOrWhiteSpace(target.AppearancePreset), "RECORD-ONLY-VERIFIED", "APPEARANCE-RECORD-VERIFIED-NO-FACEGEN")
                        End If
                        journal.Checkpoint(target.Key, outputs.Select(Function(p) Path.GetRelativePath(journal.StagingDirectory, p)))
                    Catch ex As Exception
                        item.Status = "BLOCKED-PARTIAL-NOT-INSTALLED"
                        item.Detail = ex.GetType().Name & ": " & ex.Message
                        ' No checkpoint: a resume creates a new attempt, never overwrites partial evidence.
                    End Try
                Next
                Dim reportPath = Path.Combine(journal.StagingDirectory, "report-" & Guid.NewGuid().ToString("N") & ".json")
                Using stream As New FileStream(reportPath, FileMode.CreateNew, FileAccess.Write)
                    JsonSerializer.Serialize(stream, result, New JsonSerializerOptions With {.WriteIndented = True})
                End Using
                Return result
            Finally
                Config_App.Current.Game = priorGame
            End Try
        End Function
        Private Sub SealInput(filePath As String, identity As StringBuilder)
            Dim current = Path.GetFullPath(filePath)
            While Not String.IsNullOrEmpty(current)
                If (File.Exists(current) OrElse Directory.Exists(current)) AndAlso (File.GetAttributes(current) And FileAttributes.ReparsePoint) <> 0 Then Throw New IOException("Reparse input is not allowed.")
                current = Path.GetDirectoryName(current)
            End While
            Using stream = File.OpenRead(filePath)
                identity.AppendLine(Path.GetFullPath(filePath)).AppendLine(Convert.ToHexString(SHA256.HashData(stream)))
            End Using
        End Sub
        Private Function BakeStaged(profile As NpcBatchProfile, target As NpcBatchTarget, patch As String, attempt As String) As IEnumerable(Of String)
            Dim priorRoot = BakeOutputRoot.Elegida
            Dim priorCache = FilesDictionary_class.CacheDirectory
            Dim priorLog = Logger.Enabled
            Dim priorSkip = FaceGenBuilder.SkipDdsEncode
            Dim priorTextures = FaceGenBuilder.BakeFaceTexturesEnabled
            Dim priorGpu = FaceGenBuilder.WriteGPUSandboxOutput
            Try
                Dim output = Directory.CreateDirectory(Path.Combine(attempt, "Data")).FullName
                BakeOutputRoot.Elegida = output
                FilesDictionary_class.CacheDirectory = Directory.CreateDirectory(Path.Combine(attempt, "index-cache")).FullName
                Logger.Enabled = False
                FaceGenBuilder.SkipDdsEncode = False
                FaceGenBuilder.BakeFaceTexturesEnabled = True
                FaceGenBuilder.WriteGPUSandboxOutput = False
                FilesDictionary_class.Fill_DictionaryAsync(profile.DataRoot, Nothing, loadedPlugins:=profile.LoadOrder).GetAwaiter().GetResult()
                Dim pm As New PluginManager()
                pm.LoadAllPlugins(profile.DataRoot, profile.LoadOrder)
                pm.MergeOverridePlugin(patch)
                NpcSessionCatalogs.EnsureLoaded(pm)
                Dim fid = pm.GlobalFormIDFromObjectID(target.SourcePlugin, target.ObjectId)
                Dim ctx As New NpcRenderContext(pm, ResolucionDeHeadParts.SinBorradores(pm), profile.DataRoot)
                Dim resolver As New NpcMaterialResolver(ctx, Function(st As MainForm.NPCVisualState) st.RecordBase)
                Dim baked = FaceGenBuilder.BuildCharGen(fid, pm, ctx.HeadParts, New LecturaDeCadena With {.PluginManager = pm},
                    New Dictionary(Of UInteger, LooksmenuLoader.LooksmenuPreset), Nothing, AddressOf resolver.ApplyShapeMaterialOverrides,
                    willBePacked:=False, lutDataPath:=profile.DataRoot)
                If baked Is Nothing OrElse Not baked.Success OrElse baked.Skipped OrElse baked.TextureSlotsFailed <> 0 OrElse baked.ShapesKept <= 0 Then
                    Throw New InvalidDataException("Incomplete FaceGen: " & If(baked Is Nothing, "no result", baked.Summary & "; " & baked.TextureFailureDetail))
                End If
                Dim files = baked.TexturasEscritas.Concat({baked.OutputPath}).Concat(baked.ExtraLooseFiles.Select(Function(p) Path.Combine(output, p))).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                If Not files.Any(Function(p) Path.GetExtension(p).Equals(".dds", StringComparison.OrdinalIgnoreCase)) Then Throw New InvalidDataException("FaceGen has no emitted DDS textures.")
                For Each filePath In files
                    Dim relative = Path.GetRelativePath(output, filePath)
                    If Path.IsPathRooted(relative) OrElse relative = ".." OrElse relative.StartsWith(".." & Path.DirectorySeparatorChar) OrElse New FileInfo(filePath).Length = 0 Then Throw New InvalidDataException("Invalid staged FaceGen output.")
                Next
                Return files
            Finally
                BakeOutputRoot.Elegida = priorRoot
                FilesDictionary_class.CacheDirectory = priorCache
                Logger.Enabled = priorLog
                FaceGenBuilder.SkipDdsEncode = priorSkip
                FaceGenBuilder.BakeFaceTexturesEnabled = priorTextures
                FaceGenBuilder.WriteGPUSandboxOutput = priorGpu
            End Try
        End Function
    End Module
End Namespace
