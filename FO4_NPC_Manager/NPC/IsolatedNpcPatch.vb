Option Strict On
Imports System.IO
Imports System.Linq
Imports System.Security.Cryptography
Imports System.Text.Json
Imports FO4_Base_Library

Namespace ExperimentalTransformation
    ' Record-only engineering milestone. Not a complete female appearance conversion.
    Public Module IsolatedNpcPatch
        Public Function WriteAppearancePatch(dataRoot As String, loadOrder As IEnumerable(Of String),
                                             sourcePlugin As String, objectId As UInteger,
                                             outputRoot As String, age As AgeEvidence, adultEvidence As String,
                                             presetPath As String, humanRacePlugin As String, humanRaceObjectId As UInteger) As String
            If String.IsNullOrWhiteSpace(presetPath) OrElse String.IsNullOrWhiteSpace(humanRacePlugin) OrElse humanRaceObjectId = 0UI Then
                Throw New ArgumentException("Explicit preset and approved adult human race identity required.")
            End If
            Return WritePatch(dataRoot, loadOrder, sourcePlugin, objectId, outputRoot, age, adultEvidence,
                              presetPath, humanRacePlugin, humanRaceObjectId)
        End Function

        Public Function WriteRecordOnlyPatch(dataRoot As String, loadOrder As IEnumerable(Of String),
                                             sourcePlugin As String, objectId As UInteger,
                                             outputRoot As String, age As AgeEvidence,
                                             adultEvidence As String) As String
            Return WritePatch(dataRoot, loadOrder, sourcePlugin, objectId, outputRoot, age, adultEvidence)
        End Function

        Private Function WritePatch(dataRoot As String, loadOrder As IEnumerable(Of String),
                                    sourcePlugin As String, objectId As UInteger, outputRoot As String,
                                    age As AgeEvidence, adultEvidence As String,
                                    Optional presetPath As String = "", Optional humanRacePlugin As String = "",
                                    Optional humanRaceObjectId As UInteger = 0UI) As String
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
            If fid = 0UI OrElse PluginManager.ToFaceGenLocalFormID(fid) <> objectId Then Throw New ArgumentException("Object ID does not fit the originating plugin's full/light slot space.")
            Dim raw = pm.GetRecord(fid)
            If raw Is Nothing OrElse raw.Header.Signature <> "NPC_" OrElse raw.Header.IsDeleted Then Throw New InvalidOperationException("Expected a live NPC_ record.")
            Dim npc = RecordParsers.ParseNPC(raw, pm)
            If npc Is Nothing OrElse Not npc.Record.ConfigurationFlagsPresente OrElse Not npc.Record.RacePresente Then Throw New InvalidOperationException("Incomplete NPC record.")
            Dim raceRecord = pm.GetRecord(npc.Record.Race)
            If raceRecord Is Nothing OrElse raceRecord.Header.Signature <> "RACE" OrElse raceRecord.Header.IsDeleted Then Throw New InvalidOperationException("Missing or deleted NPC race.")
            Dim raceView = TryCast(Canon.CanonRecords.Race(raceRecord, pm), Canon.RaceFO4)
            If raceView IsNot Nothing AndAlso raceView.DataFlagsPresente AndAlso raceView.DataFlagsChild Then Throw New ArgumentException("Child race requires a separately verified adult redesign.")
            If npc.Record.ConfigurationTemplateFlags <> 0US Then Throw New InvalidOperationException("Template materialization is not integrated yet.")
            If npc.Record.ConfigurationFlagsFemale Then Throw New InvalidOperationException("Already female.")
            Dim appearance = Not String.IsNullOrEmpty(presetPath)
            Dim presetHash As String = Nothing
            Dim edits As New Dictionary(Of UInteger, NpcRecordOverride) From {
                {fid, New NpcRecordOverride With {.AcbsFlags = npc.Record.ConfigurationFlags Or 1UI, .TraitsChanged = True}}}
            ' Reflect the edited session sex before overlay, as the GUI does; never mutate the raw record.
            NpcRecordOverrideApplier.AplicarEscalares(npc, fid, edits)
            If appearance Then
                Dim approvedRace = pm.GlobalFormIDFromObjectID(humanRacePlugin, humanRaceObjectId)
                If approvedRace = 0UI OrElse PluginManager.ToFaceGenLocalFormID(approvedRace) <> humanRaceObjectId Then Throw New ArgumentException("Approved race object ID does not fit its originating plugin.")
                If npc.Record.Race <> approvedRace Then Throw New InvalidOperationException("Approved human race does not match target.")
                RejectReparseAncestors(presetPath)
                presetHash = HashFile(presetPath)
                Dim preset = LooksmenuLoader.ParseFile(presetPath, pm)
                ValidateAppearancePreset(preset)
                Dim options As New PresetCategories.PresetCategoryOptions With {
                    .FaceParts = True, .HairColor = True, .FaceTints = True, .FaceVertexMorphs = True, .FaceBoneRegions = True}
                Dim selected = PresetCategoryFilter.BuildFiltered(preset, npc, Nothing, options, False)
                Dim ctx As New NpcOverrideSaver.SaveContext With {
                    .MaterializarCategorias = Function(n, id, strict) NpcRecordOverrideApplier.MaterializarCategorias(n, id, edits, strict),
                    .AplicarEscalares = Sub(n, id) NpcRecordOverrideApplier.AplicarEscalares(n, id, edits),
                    .ApplyPresetOverlayToNpcData = Function(n, id, context) NpcRecordOverlay.AplicarOverlay(n, selected, id, pm, estricto:=True, sexoEfectivoFemale:=True,
                                                                                                         resHeadParts:=ResolucionDeHeadParts.SinBorradores(pm))}
                Dim failure As String = Nothing
                npc = NpcOverrideSaver.ComposeSaveShadow(npc, fid, ctx, True, failure)
                If npc Is Nothing OrElse Not String.IsNullOrEmpty(failure) Then Throw New InvalidOperationException("Incomplete save shadow: " & failure)
                If HashFile(presetPath) <> presetHash Then Throw New IOException("Preset changed during processing.")
            End If
            Dim canonicalWarnings As New List(Of String)
            Dim expectedGlobal = SaveNpcEspWriter.CuerpoDeNpcSobrescrito(npc, Function(x) x, names.Count, canonicalWarnings)
            If canonicalWarnings.Count <> 0 Then Throw New InvalidDataException("Input cannot be verified without reviewing canonical findings.")
            Directory.CreateDirectory(destination)
            Dim patch = Path.Combine(destination, If(appearance, "EXPERIMENTAL-Appearance.esp", "EXPERIMENTAL-RecordOnly.esp"))
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
            If Not appearance AndAlso Not raw.Subrecords.Select(Function(sr) sr.Signature).SequenceEqual(written.Subrecords.Select(Function(sr) sr.Signature)) Then Throw New InvalidDataException("Subrecord structure changed.")
            If written.Header.Flags <> (raw.Header.Flags And Not &H40000UI) OrElse
               written.Header.Version <> raw.Header.Version OrElse written.Header.VCS1 <> raw.Header.VCS1 OrElse
               written.Header.VCS2 <> raw.Header.VCS2 Then Throw New InvalidDataException("Unexpected record header change.")
            ' CleanMasters can remove an unused master, changing every file-local FormID byte.
            ' Reparse through the same manager and compare both trees emitted in GLOBAL FormID space.
            ' No second serializer, hardcoded field whitelist or raw-byte exception list is used.
            pm.MergeOverridePlugin(patch)
            Dim readback = RecordParsers.ParseNPC(pm.GetRecord(fid), pm)
            canonicalWarnings.Clear()
            Dim actualGlobal = SaveNpcEspWriter.CuerpoDeNpcSobrescrito(readback, Function(x) x, names.Count, canonicalWarnings)
            If canonicalWarnings.Count <> 0 OrElse Not expectedGlobal.SequenceEqual(actualGlobal) Then
                Throw New InvalidDataException("Semantic NPC content changed after master remapping.")
            End If
            For Each item In hashes
                If HashFile(Path.Combine(inputRoot, item.Key)) <> item.Value Then Throw New IOException("Input changed during processing.")
            Next
            Dim report = New With {.schemaVersion = 1, .status = If(appearance, "EXPERIMENTAL-APPEARANCE-RECORD-NOT-FACEGEN-VALIDATED", "EXPERIMENTAL-RECORD-ONLY-NOT-IN-GAME-VALIDATED"),
                .presetSha256 = presetHash,
                .adultEvidence = adultEvidence, .sourcePlugin = sourcePlugin, .objectId = objectId,
                .inputSha256 = hashes, .patchSha256 = HashFile(patch), .masters = reopened.Masters,
                .verified = New String() {"existing-reader-reopen", "identity", "global-FormID-semantic-preservation", If(appearance, "existing-preset-overlay-save-shadow", "subrecord-structure-female-bit-only"), "header-preservation", "source-unchanged"},
                .unvalidated = New String() {"adult-classifier", "FaceGen", "body", "equipment", "voices", "quests", "collection"}}
            Using stream As New FileStream(Path.Combine(destination, "verification.json"), FileMode.CreateNew, FileAccess.Write)
                JsonSerializer.Serialize(stream, report, New JsonSerializerOptions With {.WriteIndented = True})
            End Using
            Return patch
        End Function

        Friend Sub ValidateAppearancePreset(preset As LooksmenuLoader.LooksmenuPreset)
            If preset Is Nothing OrElse Not preset.HasGender OrElse preset.Gender <> 1 Then Throw New InvalidOperationException("Explicit female preset required.")
            If preset.UnresolvedHeadParts.Count <> 0 OrElse Not String.IsNullOrEmpty(preset.UnresolvedHeadTexture) OrElse
               Not String.IsNullOrEmpty(preset.UnresolvedHairColor) OrElse Not String.IsNullOrEmpty(preset.UnresolvedSkin) OrElse
               Not String.IsNullOrEmpty(preset.UnresolvedDefaultOutfit) OrElse Not String.IsNullOrEmpty(preset.UnresolvedSleepOutfit) Then Throw New InvalidOperationException("Unresolved appearance references.")
            If preset.HasBodyMorphSliders OrElse preset.UnsupportedCounts.Overlays <> 0 OrElse Not String.IsNullOrEmpty(preset.SkinTemplateId) OrElse
               preset.SkinFormIDOverride.HasValue OrElse preset.DefaultOutfitFormIDOverride.HasValue OrElse preset.SleepOutfitFormIDOverride.HasValue Then
                Throw New InvalidOperationException("Runtime body/skin/overlay/outfit delivery is not integrated.")
            End If
            If preset.ChargenFaceMorphs.Count = 0 AndAlso preset.HeadPartFormIDs.Count = 0 AndAlso preset.FaceTintLayers.Count = 0 Then
                Throw New InvalidOperationException("Preset must carry an explicit face change.")
            End If
        End Sub

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
