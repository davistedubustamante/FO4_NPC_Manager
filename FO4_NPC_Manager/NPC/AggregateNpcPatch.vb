Option Strict On
Imports System.IO
Imports System.Linq
Imports System.Text.Json
Imports System.Security.Cryptography
Imports FO4_Base_Library

Namespace ExperimentalTransformation
    ' Consolidate already verified shards with the original multi-entry writer.
    ' Never copy original assets: only outputs named in verified staging checkpoints.
    Public Module AggregateNpcPatch
        Public Const CheckpointKey As String = "@aggregate-experimental-bundle"
        Public Function Export(profile As NpcBatchProfile, journal As BatchJournal) As String
            If journal.ContainsVerifiedCheckpoint(CheckpointKey) Then
                Return Path.GetDirectoryName(journal.VerifiedOutputPaths(CheckpointKey).Single(Function(p) Path.GetExtension(p).Equals(".esp", StringComparison.OrdinalIgnoreCase)))
            End If
            Dim pm As New PluginManager()
            pm.LoadAllPlugins(profile.DataRoot, profile.LoadOrder)
            Dim originals As New Dictionary(Of UInteger, PluginRecord)
            Dim shardPaths As New List(Of String())
            For Each target In profile.Targets
                Dim fid = pm.GlobalFormIDFromObjectID(target.SourcePlugin, target.ObjectId)
                Dim raw = pm.GetRecord(fid)
                If raw Is Nothing OrElse raw.Header.Signature <> "NPC_" Then Throw New InvalidDataException("Missing aggregate input NPC.")
                originals.Add(fid, raw)
                Dim files = journal.VerifiedOutputPaths(target.Key)
                Dim shard = files.Single(Function(p) Path.GetExtension(p).Equals(".esp", StringComparison.OrdinalIgnoreCase))
                pm.MergeOverridePlugin(shard)
                shardPaths.Add(files)
            Next
            Dim expected As New Dictionary(Of UInteger, Byte())
            Dim entries As New List(Of SaveNpcEspWriter.NpcOverrideEntry)
            For Each target In profile.Targets
                Dim fid = pm.GlobalFormIDFromObjectID(target.SourcePlugin, target.ObjectId)
                Dim npc = RecordParsers.ParseNPC(pm.GetRecord(fid), pm)
                If npc Is Nothing OrElse Not npc.Record.ConfigurationFlagsFemale Then Throw New InvalidDataException("Aggregate source is not a verified female NPC.")
                Dim warnings As New List(Of String)
                expected.Add(fid, SaveNpcEspWriter.CuerpoDeNpcSobrescrito(npc, Function(x) x, profile.LoadOrder.Count, warnings))
                If warnings.Count <> 0 Then Throw New InvalidDataException("Aggregate canonical warnings: " & String.Join("; ", warnings))
                entries.Add(New SaveNpcEspWriter.NpcOverrideEntry With {.Npc = npc, .SourcePluginName = target.SourcePlugin, .OriginalHeader = originals(fid).Header})
            Next
            Dim destination = Directory.CreateDirectory(Path.Combine(journal.StagingDirectory, "bundle-" & Guid.NewGuid().ToString("N"))).FullName
            Dim patch = Path.Combine(destination, "EXPERIMENTAL-NPC-Batch.esp")
            Using reservation As New FileStream(patch, FileMode.CreateNew, FileAccess.Write)
            End Using
            Dim saved = SaveNpcEspWriter.SaveOverridePlugin(patch, Config_App.Game_Enum.Fallout4, False, False, entries, Nothing, Nothing, pm)
            If saved.Advertencias.Count <> 0 Then Throw New InvalidDataException("Aggregate writer warnings: " & String.Join("; ", saved.Advertencias))
            Dim reader As New PluginReader() : reader.Load(patch)
            If reader.Records.Count <> entries.Count Then Throw New InvalidDataException("Aggregate record count mismatch.")
            Dim identities As New HashSet(Of UInteger)
            For Each written In reader.Records.Values
                Dim fid = pm.ResolveFormID(written.Header.FormID, reader)
                If written.Header.Signature <> "NPC_" OrElse Not expected.ContainsKey(fid) OrElse Not identities.Add(fid) Then Throw New InvalidDataException("Aggregate identity mismatch.")
                Dim before = originals(fid).Header
                If written.Header.Flags <> (before.Flags And Not &H40000UI) OrElse written.Header.Version <> before.Version OrElse written.Header.VCS1 <> before.VCS1 OrElse written.Header.VCS2 <> before.VCS2 Then Throw New InvalidDataException("Aggregate header mismatch.")
            Next
            pm.MergeOverridePlugin(patch)
            For Each pair In expected
                Dim warnings As New List(Of String)
                Dim npc = RecordParsers.ParseNPC(pm.GetRecord(pair.Key), pm)
                Dim actual = SaveNpcEspWriter.CuerpoDeNpcSobrescrito(npc, Function(x) x, profile.LoadOrder.Count, warnings)
                If warnings.Count <> 0 OrElse Not pair.Value.SequenceEqual(actual) Then Throw New InvalidDataException("Aggregate semantic NPC mismatch.")
            Next
            Dim outputs As New List(Of String) From {patch}
            For Each files In shardPaths
                Dim shard = files.Single(Function(p) Path.GetExtension(p).Equals(".esp", StringComparison.OrdinalIgnoreCase))
                Dim sourceData = Path.Combine(Path.GetDirectoryName(shard), "Data")
                For Each filePath In files
                    If filePath = shard OrElse Path.GetFileName(filePath) = "verification.json" Then Continue For
                    Dim relative = Path.GetRelativePath(sourceData, filePath)
                    If Path.IsPathRooted(relative) OrElse relative = ".." OrElse relative.StartsWith(".." & Path.DirectorySeparatorChar) Then Throw New InvalidDataException("Bundle source is outside staged Data.")
                    Dim destFile = Path.Combine(destination, "Data", relative)
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile))
                    If File.Exists(destFile) Then
                        If Not SHA256.HashData(File.ReadAllBytes(destFile)).SequenceEqual(SHA256.HashData(File.ReadAllBytes(filePath))) Then Throw New InvalidDataException("Conflicting generated resource: " & relative)
                    Else
                        File.Copy(filePath, destFile, False)
                        outputs.Add(destFile)
                    End If
                Next
            Next
            Dim report = Path.Combine(destination, "bundle-verification.json")
            Using stream As New FileStream(report, FileMode.CreateNew, FileAccess.Write)
                JsonSerializer.Serialize(stream, New With {.status = "EXPERIMENTAL-AGGREGATE-NOT-GAME-VALIDATED", .npcCount = entries.Count,
                    .requireFaceGen = profile.RequireFaceGen, .profileSha256 = journal.ProfileSha256,
                    .verified = New String() {"existing-multi-entry-writer", "existing-reader-reopen", "canonical-global-FormID-content", "header-preservation", "staged-resource-conflict-check"}})
            End Using
            outputs.Add(report)
            For Each target In profile.Targets
                If Not journal.ContainsVerifiedCheckpoint(target.Key) Then Throw New InvalidDataException("Source checkpoint changed during aggregation.")
            Next
            journal.Checkpoint(CheckpointKey, outputs.Select(Function(p) Path.GetRelativePath(journal.StagingDirectory, p)))
            Return destination
        End Function
    End Module
End Namespace
