Option Strict On
Imports System.IO
Imports System.Linq
Imports System.Security.Cryptography
Imports System.Text.Json
Imports FO4_Base_Library

Namespace ExperimentalTransformation
    ' Record-only engineering milestone. Not a complete female appearance conversion.
    Public Module IsolatedNpcPatch
        Public Function WriteRecordOnlyPatch(dataRoot As String, loadOrder As IEnumerable(Of String),
                                             sourcePlugin As String, objectId As UInteger,
                                             outputRoot As String, age As AgeEvidence,
                                             adultEvidence As String) As String
            If Config_App.Current.Game <> Config_App.Game_Enum.Fallout4 Then Throw New InvalidOperationException("FO4 context required.")
            If age <> AgeEvidence.Adult OrElse String.IsNullOrWhiteSpace(adultEvidence) Then Throw New ArgumentException("Explicit adult evidence required.")
            If objectId = 0UI OrElse objectId > &HFFFFFFUI Then Throw New ArgumentException("Expected a file-local object ID.")
            Dim inputRoot = Path.GetFullPath(dataRoot)
            Dim destination = Path.GetFullPath(outputRoot)
            RejectReparseAncestors(inputRoot)
            RejectReparseAncestors(destination)
            If destination.Equals(inputRoot, StringComparison.OrdinalIgnoreCase) OrElse
               destination.StartsWith(inputRoot.TrimEnd(Path.DirectorySeparatorChar) & Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) Then
                Throw New ArgumentException("Output must be outside the input tree.")
            End If
            If Directory.Exists(destination) OrElse File.Exists(destination) Then Throw New IOException("Output must be a new directory.")
            Dim names = loadOrder.ToList()
            If names.Count = 0 OrElse names.Distinct(StringComparer.OrdinalIgnoreCase).Count() <> names.Count Then Throw New ArgumentException("Unique explicit load order required.")
            Dim hashes As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each name In names
                If String.IsNullOrWhiteSpace(name) OrElse Path.GetFileName(name) <> name OrElse name.Contains("/") OrElse name.Contains("\") Then Throw New ArgumentException("Plugin basename required.")
                Dim inputFile = Path.Combine(inputRoot, name)
                RejectReparseAncestors(inputFile)
                Dim reader As New PluginReader()
                reader.Load(inputFile)
                If reader.IsLocalized Then Throw New InvalidOperationException("Localized input requires a separate strings validation stage.")
                For Each master In reader.Masters
                    If Not seen.Contains(master) Then Throw New InvalidOperationException("Missing or out-of-order master: " & master)
                Next
                seen.Add(name)
                hashes.Add(name, HashFile(inputFile))
            Next
            If Not seen.Contains(sourcePlugin) Then Throw New ArgumentException("Source plugin not loaded.")
            Dim pm As New PluginManager()
            pm.LoadAllPlugins(inputRoot, names)
            If names.Any(Function(n) Not pm.IsLoaded(n)) Then Throw New InvalidOperationException("Incomplete load order.")
            Dim fid = pm.GlobalFormIDFromObjectID(sourcePlugin, objectId)
            Dim raw = pm.GetRecord(fid)
            If raw Is Nothing OrElse raw.Header.Signature <> "NPC_" OrElse raw.Header.IsDeleted Then Throw New InvalidOperationException("Expected a live NPC_ record.")
            Dim npc = RecordParsers.ParseNPC(raw, pm)
            If npc Is Nothing OrElse Not npc.Record.ConfigurationFlagsPresente OrElse Not npc.Record.RacePresente Then Throw New InvalidOperationException("Incomplete NPC record.")
            If npc.Record.ConfigurationTemplateFlags <> 0US Then Throw New InvalidOperationException("Template materialization is not integrated yet.")
            If npc.Record.ConfigurationFlagsFemale Then Throw New InvalidOperationException("Already female.")
            Dim flagsBefore = npc.Record.ConfigurationFlags
            npc.Record.ConfigurationFlagsFemale = True
            Directory.CreateDirectory(destination)
            Dim patch = Path.Combine(destination, "EXPERIMENTAL-RecordOnly.esp")
            ' Reserve our own path; never let the existing writer overwrite a user's file.
            Using reservation As New FileStream(patch, FileMode.CreateNew, FileAccess.Write)
            End Using
            Dim saved = SaveNpcEspWriter.SaveOverridePlugin(patch, Config_App.Game_Enum.Fallout4, False, False,
                New List(Of SaveNpcEspWriter.NpcOverrideEntry) From {
                    New SaveNpcEspWriter.NpcOverrideEntry With {.Npc = npc, .SourcePluginName = sourcePlugin, .OriginalHeader = raw.Header}},
                Nothing, Nothing, pm)
            Dim reopened As New PluginReader()
            reopened.Load(patch)
            If reopened.Records.Count <> 1 Then Throw New InvalidDataException("Patch must contain exactly one record.")
            Dim written = reopened.Records.Values.Single()
            Dim mapped = pm.ResolveFormID(written.Header.FormID, reopened)
            If mapped <> fid OrElse written.Header.Signature <> "NPC_" Then Throw New InvalidDataException("Identity changed during round-trip.")
            If saved.Advertencias.Count <> 0 Then Throw New InvalidDataException("Writer warnings require review: " & String.Join("; ", saved.Advertencias))
            If Not raw.Subrecords.Select(Function(sr) sr.Signature).SequenceEqual(written.Subrecords.Select(Function(sr) sr.Signature)) Then Throw New InvalidDataException("Subrecord structure changed.")
            For i = 0 To raw.Subrecords.Count - 1
                Dim before = raw.Subrecords(i)
                Dim after = written.Subrecords(i)
                ' In this narrow milestone master order must remain identical, making raw preservation testable.
                Dim original = pm.GetPluginByName(raw.SourcePluginName)
                If Not original.Masters.SequenceEqual(reopened.Masters.Take(original.Masters.Count), StringComparer.OrdinalIgnoreCase) Then Throw New InvalidDataException("Master remapping needs a separate verification stage.")
                If before.Signature = "ACBS" Then
                    Dim expected = CType(before.Data.Clone(), Byte())
                    Array.Copy(BitConverter.GetBytes(flagsBefore Or 1UI), 0, expected, 0, 4)
                    If Not expected.SequenceEqual(after.Data) Then Throw New InvalidDataException("Unexpected ACBS change.")
                ElseIf Not before.Data.SequenceEqual(after.Data) Then
                    Throw New InvalidDataException("Unexpected subrecord change: " & before.Signature)
                End If
            Next
            For Each item In hashes
                If HashFile(Path.Combine(inputRoot, item.Key)) <> item.Value Then Throw New IOException("Input changed during processing.")
            Next
            Dim report = New With {.schemaVersion = 1, .status = "EXPERIMENTAL-RECORD-ONLY-NOT-IN-GAME-VALIDATED",
                .adultEvidence = adultEvidence, .sourcePlugin = sourcePlugin, .objectId = objectId,
                .inputSha256 = hashes, .patchSha256 = HashFile(patch), .masters = reopened.Masters,
                .verified = New String() {"existing-reader-reopen", "identity", "subrecord-preservation", "female-bit-only", "source-unchanged"},
                .unvalidated = New String() {"adult-classifier", "FaceGen", "body", "equipment", "voices", "quests", "collection"}}
            Using stream As New FileStream(Path.Combine(destination, "verification.json"), FileMode.CreateNew, FileAccess.Write)
                JsonSerializer.Serialize(stream, report, New JsonSerializerOptions With {.WriteIndented = True})
            End Using
            Return patch
        End Function

        Private Function HashFile(filePath As String) As String
            Using stream = File.OpenRead(filePath)
                Return Convert.ToHexString(SHA256.HashData(stream))
            End Using
        End Function

        Private Sub RejectReparseAncestors(filePath As String)
            Dim current = Path.GetFullPath(filePath)
            While current IsNot Nothing
                If (Directory.Exists(current) OrElse File.Exists(current)) AndAlso
                   (File.GetAttributes(current) And FileAttributes.ReparsePoint) <> 0 Then Throw New IOException("Reparse paths are not allowed.")
                current = Path.GetDirectoryName(current)
            End While
        End Sub
    End Module
End Namespace
