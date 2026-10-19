Option Strict On
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Text.Json
Imports System.Security.Cryptography
Imports FO4_Base_Library

Namespace ExperimentalTransformation
    Public Module GlobalNpcDiscovery
        Public Function Scan(input As CampaignInput) As CampaignPlan
            If input Is Nothing OrElse input.Policy Is Nothing OrElse input.LoadOrder Is Nothing OrElse input.LoadOrder.Count = 0 OrElse input.ProtectedRoots Is Nothing OrElse input.ProtectedRoots.Count = 0 Then Throw New ArgumentException("Explicit isolated roots, load order and policy required.")
            ' Validate policy before loading potentially expensive inputs.
            GlobalCampaign.Build(Array.Empty(Of DiscoveredNpc)(), input.Policy, "preflight")
            Dim paths As New List(Of String)
            Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Dim total As Long = 0
            For Each name In input.LoadOrder
                If String.IsNullOrWhiteSpace(name) OrElse Path.GetFileName(name) <> name OrElse name.Contains("/") OrElse name.Contains("\") OrElse Not seen.Add(name) Then Throw New ArgumentException("Unique plugin basenames required.")
                Dim filePath = Path.Combine(input.DataRoot, name)
                CheckPath(filePath)
                total = checkedAdd(total, New FileInfo(filePath).Length)
                If total > input.Policy.MaxPluginBytes Then Throw New IOException("Plugin input-byte budget exceeded before loading.")
                paths.Add(filePath)
            Next
            Dim presetBytes As Long = 0
            For Each preset In input.Policy.FemalePresets
                CheckPath(preset)
                If New FileInfo(preset).Length > 4L * 1024 * 1024 Then Throw New IOException("Preset byte budget exceeded.")
                presetBytes = checkedAdd(presetBytes, New FileInfo(preset).Length)
                If presetBytes > 64L * 1024 * 1024 Then Throw New IOException("Preset pool exceeds 64 MiB.")
                paths.Add(preset)
            Next
            If input.Localization IsNot Nothing Then paths.AddRange(input.Localization.Tables.Select(Function(t) t.FilePath))
            Dim identity = Seal(input, paths)
            Using strings = LocalizedExportScope.Open(input.Localization, input.LoadOrder)
            Dim prior = Config_App.Current.Game
            Try
                Config_App.Current.Game = Config_App.Game_Enum.Fallout4
                Dim pm As New PluginManager()
                ' Existing loader and FileID allocator determine every winning override, including ESL.
                pm.LoadAllPlugins(input.DataRoot, input.LoadOrder, sigFilter:=New HashSet(Of String)(StringComparer.Ordinal) From {
                    "NPC_", "RACE", "FACT", "HDPT", "TXST", "CLFM", "CLAS", "VTYP", "ARMO", "ARMA", "OTFT", "LVLN", "LVLI", "KYWD"})
                If input.LoadOrder.Any(Function(n) Not pm.IsLoaded(n)) Then Throw New InvalidDataException("Incomplete load order.")
                Dim mastersSeen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                For Each name In input.LoadOrder
                    Dim reader = pm.GetPluginByName(name)
                    If reader.Masters.Any(Function(m) Not mastersSeen.Contains(m)) Then Throw New InvalidDataException("Missing or out-of-order masters: " & name)
                    mastersSeen.Add(name)
                Next
                For Each presetPath In input.Policy.FemalePresets
                    Dim preset = LooksmenuLoader.ParseFile(presetPath, pm)
                    IsolatedNpcPatch.ValidateAppearancePreset(preset)
                Next
                Dim winners = pm.GetNPCs()
                If winners.Count > input.Policy.MaxRecords Then Throw New IOException("NPC inventory budget exceeded.")
                Dim chains As New Dictionary(Of UInteger, List(Of String))
                For Each plugin In pm.Plugins
                    For Each record In plugin.Records.Values.Where(Function(r) r.Header.Signature = "NPC_")
                        ' PluginManager has already remapped these headers into global FileID space.
                        Dim fid = record.Header.FormID
                        If Not chains.ContainsKey(fid) Then chains(fid) = New List(Of String)
                        chains(fid).Add(plugin.FileName)
                    Next
                Next
                Dim raceCache As New Dictionary(Of UInteger, Tuple(Of Boolean, Boolean))
                Dim inventory As New List(Of DiscoveredNpc)
                For Each raw In winners
                    Dim fid = raw.Header.FormID
                    Dim origin = pm.GetOriginatingPluginName(fid)
                    Dim item As New DiscoveredNpc With {.Key = Key(origin, PluginManager.ToFaceGenLocalFormID(fid)), .Origin = origin,
                        .ObjectId = PluginManager.ToFaceGenLocalFormID(fid), .Winner = raw.SourcePluginName, .Deleted = raw.Header.IsDeleted,
                        .OverrideChain = chains(fid)}
                    inventory.Add(item)
                    If item.Deleted Then Continue For
                    Try
                        Dim npc = RecordParsers.ParseNPC(raw, pm)
                        If npc Is Nothing OrElse Not npc.Record.ConfigurationFlagsPresente Then Throw New InvalidDataException("Incomplete NPC.")
                        item.OriginalTemplateFlags = npc.Record.ConfigurationTemplateFlags
                        npc = StrictTraitsMaterialization.Materialize(npc, pm)
                        If Not npc.Record.RacePresente Then Throw New InvalidDataException("Missing effective NPC race.")
                        item.TraitsMaterialized = item.OriginalTemplateFlags <> 0US
                        If raw.SourcePluginIsLocalized AndAlso input.Localization IsNot Nothing Then
                            Dim warnings As New List(Of String)
                            SaveNpcEspWriter.CuerpoDeNpcSobrescrito(npc, Function(x) x, input.LoadOrder.Count, warnings)
                            If warnings.Count <> 0 Then Throw New InvalidDataException("Localized NPC cannot be exported without findings: " & String.Join("; ", warnings))
                        End If
                        item.Unique = npc.Record.ConfigurationFlagsUnique
                        item.Essential = npc.Record.ConfigurationFlagsEssential
                        item.Female = npc.Record.ConfigurationFlagsFemale
                        item.Factions = npc.Record.Factions.Where(Function(f) f.FactionPresente).Select(Function(f) Key(pm.GetOriginatingPluginName(f.Faction), PluginManager.ToFaceGenLocalFormID(f.Faction))).ToList()
                        item.TemplateFlags = npc.Record.ConfigurationTemplateFlags
                        item.HasScripts = raw.GetSubrecord("VMAD").HasValue AndAlso raw.GetSubrecord("VMAD").Value.Data.Length > 6
                        item.RaceOrigin = pm.GetOriginatingPluginName(npc.Record.Race)
                        item.RaceObjectId = PluginManager.ToFaceGenLocalFormID(npc.Record.Race)
                        item.RaceKey = Key(item.RaceOrigin, item.RaceObjectId)
                        Dim raceRecord = pm.GetRecord(npc.Record.Race)
                        If raceRecord Is Nothing OrElse raceRecord.Header.Signature <> "RACE" OrElse raceRecord.Header.IsDeleted Then Throw New InvalidDataException("Missing or deleted race.")
                        Dim flags As Tuple(Of Boolean, Boolean) = Nothing
                        If Not raceCache.TryGetValue(npc.Record.Race, flags) Then
                            Dim race = TryCast(Canon.CanonRecords.Race(raceRecord, pm), Canon.RaceFO4)
                            If race Is Nothing Then Throw New InvalidDataException("Unresolved FO4 race.")
                            flags = Tuple.Create(race.DataFlagsPresente, race.DataFlagsPresente AndAlso race.DataFlagsChild)
                            raceCache.Add(npc.Record.Race, flags)
                        End If
                        item.RaceFlagsKnown = flags.Item1
                        item.ChildRace = flags.Item2
                    Catch ex As Exception
                        item.ErrorDetail = ex.GetType().Name & ": " & ex.Message
                    End Try
                Next
                If Seal(input, paths) <> identity Then Throw New IOException("Input changed during discovery.")
                Dim blockers As New List(Of String)
                If input.Localization Is Nothing AndAlso pm.Plugins.Any(Function(p) p.IsLocalized) Then blockers.Add("localized-load-order-export-not-integrated")
                strings.VerifyUnchanged()
                Return GlobalCampaign.Build(inventory, input.Policy, identity, blockers, pm.AllRecords.Count)
            Finally
                Config_App.Current.Game = prior
            End Try
            End Using
        End Function
        Public Sub PreparePage(input As CampaignInput, savedPlan As CampaignPlan, page As Integer, outputPath As String)
            Dim current = Scan(input)
            If savedPlan Is Nothing OrElse current.PlanSha256 <> savedPlan.PlanSha256 OrElse current.InputSha256 <> savedPlan.InputSha256 Then Throw New InvalidDataException("Discovery plan is stale or policy changed.")
            WriteBounded(input, GlobalCampaign.CreateBatch(input, current, page), outputPath)
        End Sub
        Public Sub WriteReport(input As CampaignInput, plan As CampaignPlan, outputPath As String)
            WriteBounded(input, plan, outputPath)
        End Sub
        Public Sub WriteNarrativeReport(input As CampaignInput, report As NarrativeReport, outputPath As String)
            WriteBounded(input, report, outputPath)
        End Sub
        Private Sub WriteBounded(input As CampaignInput, value As Object, outputPath As String)
            GlobalCampaign.Build(Array.Empty(Of DiscoveredNpc)(), input.Policy, "report-preflight")
            Dim destination = Path.GetFullPath(outputPath)
            CheckPath(destination)
            For Each root In input.ProtectedRoots.Concat({input.DataRoot})
                Dim fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root))
                If destination.Equals(fullRoot, StringComparison.OrdinalIgnoreCase) OrElse destination.StartsWith(If(Path.EndsInDirectorySeparator(fullRoot), fullRoot, fullRoot & Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) Then Throw New IOException("Report must be outside all original roots.")
            Next
            Dim bytes = JsonSerializer.SerializeToUtf8Bytes(value, value.GetType())
            If bytes.LongLength > input.Policy.MaxReportBytes Then Throw New IOException("Report byte budget exceeded; nothing written.")
            Using stream As New FileStream(destination, FileMode.CreateNew, FileAccess.Write)
                stream.Write(bytes) : stream.Flush(True)
            End Using
        End Sub
        Private Function Seal(input As CampaignInput, paths As IEnumerable(Of String)) As String
            Using hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256)
                hash.AppendData(JsonSerializer.SerializeToUtf8Bytes(input))
                For Each filePath In paths
                    CheckPath(filePath)
                    hash.AppendData(Encoding.UTF8.GetBytes(Path.GetFullPath(filePath)))
                    Using stream = File.OpenRead(filePath)
                        hash.AppendData(SHA256.HashData(stream))
                    End Using
                Next
                Return Convert.ToHexString(hash.GetHashAndReset())
            End Using
        End Function
        Private Function Key(plugin As String, objectId As UInteger) As String
            Return plugin.ToUpperInvariant() & "|" & objectId.ToString("X6")
        End Function
        Private Function checkedAdd(left As Long, right As Long) As Long
            If right < 0 OrElse left > Long.MaxValue - right Then Throw New IOException("Input size overflow.")
            Return left + right
        End Function
        Private Sub CheckPath(value As String)
            Dim current = Path.GetFullPath(value)
            While Not String.IsNullOrEmpty(current)
                If (File.Exists(current) OrElse Directory.Exists(current)) AndAlso (File.GetAttributes(current) And FileAttributes.ReparsePoint) <> 0 Then Throw New IOException("Reparse path rejected.")
                current = Path.GetDirectoryName(current)
            End While
        End Sub
    End Module
End Namespace
