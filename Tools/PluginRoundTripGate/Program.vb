Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Security.Cryptography
Imports System.Text.Json
Imports FO4_Base_Library
Imports BSA_BA2_Library_DLL.BethesdaArchive.Core
Imports FO4_NPC_Manager.ExperimentalTransformation

Module Program
    Private checks As Integer
    Private lastCheck As String = "none"
    Private Sub Check(ok As Boolean, name As String)
        If Not ok Then Throw New InvalidDataException("FAIL: " & name)
        checks += 1
        lastCheck = name
        Console.WriteLine("PASS: " & name)
    End Sub
    Private Sub Reject(action As Action, name As String, Optional expectedReason As String = Nothing)
        Dim rejected = False
        Try
            action()
        Catch ex As ArgumentException
            rejected = expectedReason Is Nothing OrElse ex.Message.Contains(expectedReason, StringComparison.OrdinalIgnoreCase)
        Catch ex As InvalidOperationException
            rejected = expectedReason Is Nothing OrElse ex.Message.Contains(expectedReason, StringComparison.OrdinalIgnoreCase)
        Catch ex As InvalidDataException
            rejected = expectedReason Is Nothing OrElse ex.Message.Contains(expectedReason, StringComparison.OrdinalIgnoreCase)
        Catch ex As IOException
            rejected = expectedReason Is Nothing OrElse ex.Message.Contains(expectedReason, StringComparison.OrdinalIgnoreCase)
        End Try
        Check(rejected, name)
    End Sub
    ' Only the fixture factory assembles test bytes. Production patching uses the existing writer.
    Private Function Sr(sig As String, data As Byte()) As Byte()
        Using ms As New MemoryStream()
            Using bw As New BinaryWriter(ms, Encoding.UTF8, True)
                bw.Write(Encoding.ASCII.GetBytes(sig)) : bw.Write(CUShort(data.Length)) : bw.Write(data)
            End Using
            Return ms.ToArray()
        End Using
    End Function
    Private Function Text(value As String) As Byte()
        Return Encoding.UTF8.GetBytes(value & ChrW(0))
    End Function
    Private Sub Fixture(data As String, Optional templates As UShort = 0US, Optional twoNpcs As Boolean = False, Optional light As Boolean = False, Optional genericHuman As Boolean = False)
        Dim game = Config_App.Game_Enum.Fallout4
        File.WriteAllBytes(Path.Combine(data, "Fallout4.esm"), GenericPluginWriter.BuildTes4Header(game, True, False, New List(Of String), 0, &H800UI, "fixture", False))
        Dim acbs(19) As Byte
        Array.Copy(BitConverter.GetBytes(If(genericHuman, &H80000000UI, &H80000002UI)), acbs, 4)
        Array.Copy(BitConverter.GetBytes(templates), 0, acbs, 14, 2)
        Dim body = Sr("EDID", Text("SyntheticAdult")) _
            .Concat(Sr("VMAD", New Byte() {6, 0, 2, 0, 0, 0})) _
            .Concat(Sr("OBND", New Byte(11) {})) _
            .Concat(Sr("ACBS", acbs)) _
            .Concat(Sr("VTCK", BitConverter.GetBytes(&H1000802UI))) _
            .Concat(Sr("RNAM", BitConverter.GetBytes(&H1000801UI))) _
            .Concat(Sr("AIDT", New Byte(23) {})) _
            .Concat(Sr("CNAM", BitConverter.GetBytes(&H1000803UI))) _
            .Concat(Sr("FULL", Text("Adult fixture"))) _
            .Concat(Sr("DATA", Array.Empty(Of Byte)())) _
            .Concat(Sr("NAM6", BitConverter.GetBytes(1.0F))) _
            .Concat(Sr("NAM7", BitConverter.GetBytes(1.0F))) _
            .Concat(Sr("NAM8", BitConverter.GetBytes(0UI))).ToArray()
        Dim npc = GenericPluginWriter.WrapRecord("NPC_", body, 0UI, &H1000800UI, 0UI, 0US, game)
        Dim npcRecords As New List(Of Byte()) From {npc}
        If twoNpcs Then
            Dim secondBody = Sr("EDID", Text("SyntheticAdultSecond")).Concat(body.Skip(Sr("EDID", Text("SyntheticAdult")).Length)).ToArray()
            npcRecords.Add(GenericPluginWriter.WrapRecord("NPC_", secondBody, 0UI, &H1000804UI, 0UI, 0US, game))
        End If
        Dim raceBody = Sr("EDID", Text("AdultRaceFixture"))
        If genericHuman Then
            Dim view = DirectCast(FO4_Base_Library.Canon.CanonRecords.RaceNuevo(FO4_Base_Library.Canon.WbGame.Fallout4), FO4_Base_Library.Canon.RaceFO4)
            view.EditorID = "AdultRaceFixture"
            view.DataFlags = 0UI
            raceBody = FO4_Base_Library.Canon.WbWriter.EmitBody(view.Node, view.Context)
        End If
        Dim race = GenericPluginWriter.WrapRecord("RACE", raceBody, 0UI, &H1000801UI, 0UI, 0US, game)
        Dim voice = GenericPluginWriter.WrapRecord("VTYP", Sr("EDID", Text("QuestVoiceFixture")), 0UI, &H1000802UI, 0UI, 0US, game)
        Dim npcClass = GenericPluginWriter.WrapRecord("CLAS", Sr("EDID", Text("ClassFixture")), 0UI, &H1000803UI, 0UI, 0US, game)
        Dim header = GenericPluginWriter.BuildTes4Header(game, True, light, New List(Of String) From {"Fallout4.esm"}, If(twoNpcs, 9, 8), &H805UI, "fixture", False)
        File.WriteAllBytes(Path.Combine(data, "Fixture.esm"), header.Concat(GenericPluginWriter.BuildGrup("CLAS", New List(Of Byte()) From {npcClass})) _
            .Concat(GenericPluginWriter.BuildGrup("RACE", New List(Of Byte()) From {race})) _
            .Concat(GenericPluginWriter.BuildGrup("NPC_", npcRecords)) _
            .Concat(GenericPluginWriter.BuildGrup("VTYP", New List(Of Byte()) From {voice})).ToArray())
    End Sub
    Private Function CreateCopyJournal(profile As NpcBatchProfile, parent As String, shards As String(), conflict As Boolean) As BatchJournal
        Dim journal = BatchJournal.Create(parent, {profile.DataRoot}, Encoding.UTF8.GetBytes("controlled-copy-semantics-only"))
        For index As Integer = 0 To profile.Targets.Count - 1
            Dim attempt = Directory.CreateDirectory(Path.Combine(journal.StagingDirectory, "npc-" & index.ToString())).FullName
            Dim esp = Path.Combine(attempt, "CopyShard-" & index.ToString() & ".esp")
            File.Copy(shards(index), esp, False)
            Dim proof = Path.Combine(attempt, "verification.json")
            File.WriteAllText(proof, "Controlled staging-copy test; no FaceGen or game evidence.")
            Dim resource = Path.Combine(attempt, "Data", "Textures", "copy-fixture.bin")
            Directory.CreateDirectory(Path.GetDirectoryName(resource))
            File.WriteAllText(resource, If(conflict AndAlso index = 1, "different fixture bytes", "identical fixture bytes"))
            journal.Checkpoint(profile.Targets(index).Key, {Path.GetRelativePath(journal.StagingDirectory, esp), Path.GetRelativePath(journal.StagingDirectory, proof), Path.GetRelativePath(journal.StagingDirectory, resource)})
        Next
        Return journal
    End Function
    Private Function StringTable(filePath As String, value As String, Optional id As UInteger = 42UI, Optional pluginName As String = "Fixture.esm") As LocalizationTable
        Dim bytes = Text(value)
        Using output As New BinaryWriter(File.Create(filePath))
            output.Write(1UI) : output.Write(CUInt(bytes.Length))
            output.Write(id) : output.Write(0UI) : output.Write(bytes)
        End Using
        Return New LocalizationTable With {.Plugin = pluginName, .Kind = "strings", .FilePath = filePath, .Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(filePath)))}
    End Function
    Private Sub ArchiveLocalizationChecks(root As String, data As String, loose As LocalizationTable)
        Dim entries As New List(Of VirtualEntry)
        Dim manifest As New LocalizationProfile With {.Language = "spanish"}
        Dim archive = Path.Combine(root, "Fixture - Main.ba2")
        For Each kind In {"strings", "dlstrings", "ilstrings"}
            Dim payload = Text("Texto BA2 " & kind & " Ángela")
            Using memory As New MemoryStream(), writer As New BinaryWriter(memory, Encoding.UTF8, True)
                writer.Write(1UI) : writer.Write(CUInt(payload.Length + If(kind = "strings", 0, 4)))
                writer.Write(42UI) : writer.Write(0UI)
                If kind <> "strings" Then writer.Write(CUInt(payload.Length))
                writer.Write(payload) : writer.Flush()
                Dim bytes = memory.ToArray()
                entries.Add(New VirtualEntry With {.Directory = "Strings", .FileName = "Fixture_spanish." & kind, .Data = bytes})
                manifest.Tables.Add(New LocalizationTable With {.Plugin = "Fixture.esm", .Kind = kind, .FilePath = archive,
                    .ResourcePath = "Strings\Fixture_spanish." & kind, .ResourceSha256 = Convert.ToHexString(SHA256.HashData(bytes))})
            End Using
        Next
        entries.Add(New VirtualEntry With {.Directory = "Strings", .FileName = "Fixture_english.strings", .Data = File.ReadAllBytes(loose.FilePath)})
        File.WriteAllBytes(archive, Ba2WriterGNRL.Build(entries, New Ba2WriterGNRL.Options With {.Version = 1UI}))
        Dim archiveHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(archive)))
        For Each table In manifest.Tables
            table.Sha256 = archiveHash
        Next
        Dim sealedSelection = LocalizedExportScope.SealSelection(manifest, {"Fallout4.esm", "Fixture.esm"})
        Check(sealedSelection.Tables.All(Function(t) t.Sha256 = archiveHash AndAlso t.ResourceSha256.Length = 64), "local sealer fingerprints individual BA2 tables without copying archive")
        Dim sealArchive = Path.Combine(root, "selection-only.ba2")
        File.WriteAllBytes(sealArchive, Ba2WriterGNRL.Build(entries, New Ba2WriterGNRL.Options With {.Version = 1UI}))
        Dim unsealed = JsonSerializer.Deserialize(Of LocalizationProfile)(JsonSerializer.Serialize(manifest))
        For Each table In unsealed.Tables
            table.FilePath = sealArchive : table.Sha256 = "" : table.ResourceSha256 = ""
        Next
        Dim sealCampaign As New CampaignInput With {.DataRoot = data, .LoadOrder = New List(Of String) From {"Fallout4.esm", "Fixture.esm"}, .ProtectedRoots = New List(Of String) From {data}, .Localization = unsealed}
        File.WriteAllText(Path.Combine(root, "ba2-selection-profile.json"), JsonSerializer.Serialize(sealCampaign))
        Dim oldDictionary = FilesDictionary_class.Dictionary
        Dim oldCache = FilesDictionary_class.CacheDirectory
        Using scope = LocalizedExportScope.Open(manifest, {"Fallout4.esm", "Fixture.esm"})
            Dim pm As New PluginManager()
            pm.LoadAllPlugins(data, {"Fallout4.esm", "Fixture.esm"})
            For Each kind In {LocalizedStringTableKind.Strings, LocalizedStringTableKind.DLStrings, LocalizedStringTableKind.ILStrings}
                Dim value As String = ""
                Check(pm.TryResolveLocalizedString("Fixture.esm", 42UI, kind, value) AndAlso value.StartsWith("Texto BA2", StringComparison.Ordinal), "original resolver reads selected BA2 " & kind.ToString())
                Check(Not pm.TryResolveLocalizedString("Fixture.esm", 99UI, kind, value), "missing ID in BA2 " & kind.ToString() & " cannot fallback")
            Next
            scope.VerifyUnchanged()
        End Using
        Check(ReferenceEquals(oldDictionary, FilesDictionary_class.Dictionary) AndAlso FilesDictionary_class.CacheDirectory = oldCache, "BA2 resource and cache context restored")
        Dim patch = WriteRecordOnlyPatch(data, {"Fallout4.esm", "Fixture.esm"}, "Fixture.esm", &H800UI, Path.Combine(root, "localized-ba2"), AgeEvidence.Adult, "controlled adult", manifest)
        Dim reopened As New PluginReader() : reopened.Load(patch)
        Check(Encoding.UTF8.GetString(reopened.Records.Values.Single().GetSubrecord("FULL").Value.Data).StartsWith("Texto BA2 strings", StringComparison.Ordinal), "BA2 selected language exports and reopens through original ESP writer")
        Check(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(archive))) = archiveHash AndAlso Not Directory.Exists(Path.Combine(root, "Strings")), "BA2 unchanged and never extracted to disk")
        Dim alternateArchive = Path.Combine(root, "alternate-priority.ba2")
        Dim alternateBytes = entries(0).Data.ToArray()
        Dim alternate = JsonSerializer.Deserialize(Of LocalizationProfile)(JsonSerializer.Serialize(manifest))
        ' The declared source wins even when another archive containing the same key is present.
        File.WriteAllBytes(alternateArchive, Ba2WriterGNRL.Build(New List(Of VirtualEntry) From {New VirtualEntry With {.Directory = "Strings", .FileName = "Fixture_spanish.strings", .Data = alternateBytes}}, New Ba2WriterGNRL.Options With {.Version = 1UI}))
        alternate.Tables = alternate.Tables.Take(1).ToList()
        alternate.Tables(0).FilePath = alternateArchive
        alternate = LocalizedExportScope.SealSelection(alternate, {"Fallout4.esm", "Fixture.esm"})
        Using selectedScope = LocalizedExportScope.Open(alternate, {"Fallout4.esm", "Fixture.esm"})
            Check(FilesDictionary_class.TryGetEntry("Strings\Fixture_spanish.strings").BA2File = alternateArchive, "explicit resource priority selects declared archive, not another archive with same table key")
        End Using

        manifest.Tables.Add(loose)
        Reject(Sub() LocalizedExportScope.Open(manifest, {"Fallout4.esm", "Fixture.esm"}), "loose and BA2 competing winners require explicit unambiguous selection")
        manifest.Tables.Remove(loose)
        Dim first = manifest.Tables(0)
        first.ResourceSha256 = New String("0"c, 64)
        Reject(Sub() LocalizedExportScope.Open(manifest, {"Fallout4.esm", "Fixture.esm"}), "BA2 entry content hash mismatch rejected")
        first.ResourceSha256 = Convert.ToHexString(SHA256.HashData(entries(0).Data))
        first.ResourcePath = "Strings\Fixture_english.strings"
        Reject(Sub() LocalizedExportScope.Open(manifest, {"Fallout4.esm", "Fixture.esm"}), "BA2 language cannot fallback to English")
        first.ResourcePath = "Strings\Fixture_spanish.strings"
        first.Sha256 = New String("0"c, 64)
        Reject(Sub() LocalizedExportScope.Open(manifest, {"Fallout4.esm", "Fixture.esm"}), "archive integrity mismatch rejected before extraction")
        Check(ReferenceEquals(oldDictionary, FilesDictionary_class.Dictionary), "failed BA2 operations restore original context")
        entries(0).Data = New Byte(15) {}
        File.WriteAllBytes(archive, Ba2WriterGNRL.Build(entries, New Ba2WriterGNRL.Options With {.Version = 1UI}))
        For Each table In manifest.Tables
            table.Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(archive)))
        Next
        first.ResourceSha256 = Convert.ToHexString(SHA256.HashData(entries(0).Data))
        Reject(Sub() WriteRecordOnlyPatch(data, {"Fallout4.esm", "Fixture.esm"}, "Fixture.esm", &H800UI, Path.Combine(root, "ba2-invalid-table"), AgeEvidence.Adult, "controlled adult", manifest), "hash-valid BA2 with missing string data cannot emit ESP")
        Check(Not Directory.Exists(Path.Combine(root, "ba2-invalid-table")), "invalid BA2 localization leaves no export directory")
    End Sub
    Private Sub LocalizationChecks(root As String)
        Dim data = Directory.CreateDirectory(Path.Combine(root, "localized-inputs")).FullName
        Fixture(data, genericHuman:=True)
        Dim order = {"Fallout4.esm", "Fixture.esm"}
        Dim original As New PluginReader() : original.Load(Path.Combine(data, "Fixture.esm"))
        Dim game = Config_App.Game_Enum.Fallout4
        Dim header = GenericPluginWriter.BuildTes4Header(game, True, False, New List(Of String) From {"Fallout4.esm"}, 8, &H805UI, "localized controlled fixture", False)
        header(8) = header(8) Or CByte(&H80)
        Dim allBytes = header.ToList()
        For Each group In original.Records.Values.GroupBy(Function(r) r.Header.Signature)
            Dim members As New List(Of Byte())
            For Each record In group
                Dim body As New List(Of Byte)
                For Each part In record.Subrecords
                    Dim bytes = If(part.Signature = "FULL", BitConverter.GetBytes(42UI), part.Data)
                    If part.Signature = "VMAD" Then bytes = New Byte() {6, 0, 2, 0, 1, 0, 5, 0, 80, 114, 111, 98, 101, 0, 0, 0}
                    body.AddRange(Sr(part.Signature, bytes))
                Next
                members.Add(GenericPluginWriter.WrapRecord(record.Header.Signature, body.ToArray(), record.Header.Flags, record.Header.FormID, record.Header.VCS1, record.Header.VCS2, game))
            Next
            allBytes.AddRange(GenericPluginWriter.BuildGrup(group.Key, members))
        Next
        File.WriteAllBytes(Path.Combine(data, "Fixture.esm"), allBytes.ToArray())
        Dim tables = Directory.CreateDirectory(Path.Combine(root, "selected-tables")).FullName
        Dim spanish = StringTable(Path.Combine(tables, "Fixture_spanish.strings"), "Adulta de prueba Ángela")
        Dim english = StringTable(Path.Combine(tables, "Fixture_english.strings"), "Wrong fallback language")
        Dim localization As New LocalizationProfile With {.Language = "spanish", .Tables = New List(Of LocalizationTable) From {spanish}}
        ArchiveLocalizationChecks(root, data, spanish)
        Dim beforeDictionary = FilesDictionary_class.Dictionary
        Dim beforeRoot = FilesDictionary_class.FO4Path
        Dim patch As String
        Using foreignEncoding = PluginEncodingSettings.PushTranslatableOverride("1251")
            patch = WriteRecordOnlyPatch(data, order, "Fixture.esm", &H800UI, Path.Combine(root, "localized-positive"), AgeEvidence.Adult, "controlled adult", localization)
            Check(PluginEncodingSettings.Translatable.CodePage = 1251, "localized UTF8 output scope restores caller encoding")
        End Using
        Dim reopened As New PluginReader() : reopened.Load(patch)
        Dim written = reopened.Records.Values.Single()
        Check(Not reopened.IsLocalized AndAlso Encoding.UTF8.GetString(written.GetSubrecord("FULL").Value.Data).TrimEnd(ChrW(0)) = "Adulta de prueba Ángela", "localized source emits explicitly selected text and reopens as self-contained inline ESP")
        Check(ReferenceEquals(beforeDictionary, FilesDictionary_class.Dictionary) AndAlso beforeRoot = FilesDictionary_class.FO4Path, "localized context restores original process resources")
        Check(written.GetSubrecord("VMAD").Value.Data.SequenceEqual(New Byte() {6, 0, 2, 0, 1, 0, 5, 0, 80, 114, 111, 98, 101, 0, 0, 0}), "localized round-trip preserves nonempty script attachment")
        Dim localizedPm As New PluginManager() : localizedPm.LoadAllPlugins(data, order)
        Check(localizedPm.ResolveFormID(BitConverter.ToUInt32(written.GetSubrecord("VTCK").Value.Data, 0), reopened) = &H1000802UI, "localized round-trip preserves voice FormID")
        Check(File.ReadAllBytes(Path.Combine(data, "Fixture.esm")).SequenceEqual(allBytes), "localized master and original string IDs remain untouched")
        Using proof = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root, "localized-positive", "verification.json")))
            Check(proof.RootElement.GetProperty("localizedStrings").EnumerateArray().Any(Function(e) e.GetProperty("Id").GetUInt32() = 42UI AndAlso e.GetProperty("SourcePlugin").GetString() = "Fixture.esm"), "localized verification traces source string ID and owner while delivering inline text")
        End Using
        Dim localizedReader As New PluginReader() : localizedReader.Load(Path.Combine(data, "Fixture.esm"))
        Dim sourceNpc = localizedReader.Records.Values.Single(Function(r) r.Header.Signature = "NPC_")
        Dim lateHeader = GenericPluginWriter.BuildTes4Header(game, False, False, New List(Of String) From {"Fallout4.esm", "Fixture.esm"}, 2, &H800UI, "localized winning override", False)
        lateHeader(8) = lateHeader(8) Or CByte(&H80)
        Dim lateNpc = GenericPluginWriter.WrapRecord("NPC_", RecordBytes(sourceNpc), 0UI, &H1000800UI, 0UI, 0US, game)
        File.WriteAllBytes(Path.Combine(data, "LocalizedLate.esp"), lateHeader.Concat(GenericPluginWriter.BuildGrup("NPC_", New List(Of Byte()) From {lateNpc})).ToArray())
        Dim lateTable = StringTable(Path.Combine(tables, "LocalizedLate_spanish.strings"), "Nombre del override ganador", 42UI, "LocalizedLate.esp")
        localization.Tables.Add(lateTable)
        Dim winningPatch = WriteRecordOnlyPatch(data, order.Concat({"LocalizedLate.esp"}), "Fixture.esm", &H800UI, Path.Combine(root, "localized-winning-override"), AgeEvidence.Adult, "controlled adult", localization)
        Dim winningReader As New PluginReader() : winningReader.Load(winningPatch)
        Check(Encoding.UTF8.GetString(winningReader.Records.Values.Single().GetSubrecord("FULL").Value.Data).TrimEnd(ChrW(0)) = "Nombre del override ganador", "localized string resolves from winning override owner rather than originating master")
        localization.Tables.Remove(lateTable)
        Dim profile As New NpcBatchProfile With {.DataRoot = data, .LoadOrder = order.ToList(), .ProtectedRoots = New List(Of String) From {data, tables}, .RequireFaceGen = False, .Localization = localization,
            .Targets = New List(Of NpcBatchTarget) From {New NpcBatchTarget With {.SourcePlugin = "Fixture.esm", .ObjectId = &H800UI, .Age = AgeEvidence.Adult, .AdultEvidence = "controlled adult"}}}
        Dim result = ProductionNpcBatch.Run(profile, root)
        Check(result.BundleStatus = "RECORDS-ONLY-BUNDLE-NOT-A-COMPLETE-CONVERSION", "localized input passes original production shard and aggregate writers")
        Check(ProductionNpcBatch.Run(profile, root, result.StagingDirectory).Items.Single().Status = "RESUMED-VERIFIED-OUTPUTS", "localized production checkpoint resumes without regeneration")
        Dim campaign As New CampaignInput With {.DataRoot = data, .LoadOrder = order.ToList(), .ProtectedRoots = New List(Of String) From {data}, .Localization = localization}
        campaign.Policy.HumanRaces.Add("FIXTURE.ESM|000801")
        campaign.Policy.AdultEvidence.Add("FIXTURE.ESM|000800", "controlled adult")
        Dim plan = GlobalNpcDiscovery.Scan(campaign)
        Check(plan.ExecutionBlockers.Count = 0 AndAlso plan.Decisions.Single().Npc.ErrorDetail = "", "global discovery removes localized admission only with explicit verified resource context")
        Reject(Sub() WriteRecordOnlyPatch(data, order, "Fixture.esm", &H800UI, Path.Combine(root, "localized-no-selection"), AgeEvidence.Adult, "controlled adult"), "localized export without explicit language remains blocked")
        Dim missingTablePath = spanish.FilePath
        spanish.FilePath = Path.Combine(root, "absent", "Fixture_spanish.strings")
        Reject(Sub() WriteRecordOnlyPatch(data, order, "Fixture.esm", &H800UI, Path.Combine(root, "localized-absent-table"), AgeEvidence.Adult, "controlled adult", localization), "missing language table cannot use ambient fallback")
        spanish.FilePath = missingTablePath
        spanish = StringTable(spanish.FilePath, "Unrelated ID", 99UI)
        localization.Tables(0) = spanish
        Reject(Sub() WriteRecordOnlyPatch(data, order, "Fixture.esm", &H800UI, Path.Combine(root, "localized-missing-id"), AgeEvidence.Adult, "controlled adult", localization), "absent localized ID fails before ESP creation")
        Check(Not Directory.Exists(Path.Combine(root, "localized-missing-id")), "unresolved strings produce no output directory")
        Check(GlobalNpcDiscovery.Scan(campaign).Decisions.Single().Npc.ErrorDetail.Contains("Localized NPC"), "global inventory blocks NPC with unresolved string instead of silently scheduling")
        spanish = StringTable(spanish.FilePath, "Adulta de prueba Ángela") : localization.Tables(0) = spanish
        localization.Language = "english"
        Reject(Sub() WriteRecordOnlyPatch(data, order, "Fixture.esm", &H800UI, Path.Combine(root, "localized-wrong-language"), AgeEvidence.Adult, "controlled adult", localization), "language mismatch cannot fall back to other installed table")
        localization.Language = "spanish"
        localization.Tables.Add(spanish)
        Reject(Sub() WriteRecordOnlyPatch(data, order, "Fixture.esm", &H800UI, Path.Combine(root, "localized-ambiguous"), AgeEvidence.Adult, "controlled adult", localization), "duplicate table provenance is rejected")
        localization.Tables.RemoveAt(1)
        spanish.Sha256 = New String("0"c, 64)
        Reject(Sub() WriteRecordOnlyPatch(data, order, "Fixture.esm", &H800UI, Path.Combine(root, "localized-hash"), AgeEvidence.Adult, "controlled adult", localization), "incorrect table hash fails closed")
        spanish.Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(spanish.FilePath)))
        spanish.Kind = "dlstrings"
        Reject(Sub() WriteRecordOnlyPatch(data, order, "Fixture.esm", &H800UI, Path.Combine(root, "localized-kind"), AgeEvidence.Adult, "controlled adult", localization), "wrong table kind is not treated as STRINGS")
        spanish.Kind = "strings"
        spanish.Plugin = "Missing.esm"
        Reject(Sub() WriteRecordOnlyPatch(data, order, "Fixture.esm", &H800UI, Path.Combine(root, "localized-owner"), AgeEvidence.Adult, "controlled adult", localization), "table owner must belong to explicit masters and load order")
        spanish.Plugin = "Fixture.esm"
        Reject(Sub() WriteRecordOnlyPatch(data, {"Fixture.esm"}, "Fixture.esm", &H800UI, Path.Combine(root, "localized-master"), AgeEvidence.Adult, "controlled adult", localization), "localized plugin missing its master is rejected")
        File.WriteAllBytes(spanish.FilePath, New Byte() {1, 2, 3})
        spanish.Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(spanish.FilePath)))
        Reject(Sub() WriteRecordOnlyPatch(data, order, "Fixture.esm", &H800UI, Path.Combine(root, "localized-corrupt"), AgeEvidence.Adult, "controlled adult", localization), "malformed localized table is not silently emitted empty")
        StringTable(spanish.FilePath, "Adulta de prueba Ángela")
        File.WriteAllText(Path.ChangeExtension(spanish.FilePath, ".cpoverride"), "1252")
        Reject(Sub() WriteRecordOnlyPatch(data, order, "Fixture.esm", &H800UI, Path.Combine(root, "localized-encoding"), AgeEvidence.Adult, "controlled adult", localization), "unsealed encoding override cannot change reproducible localized output")
        Check(ReferenceEquals(beforeDictionary, FilesDictionary_class.Dictionary), "failed localization restores original resource context")
    End Sub

    Private Sub RewriteFixture(data As String, rewrite As Func(Of PluginRecord, Byte()))
        Dim input = Path.Combine(data, "Fixture.esm")
        Dim reader As New PluginReader() : reader.Load(input)
        Dim header = GenericPluginWriter.BuildTes4Header(Config_App.Game_Enum.Fallout4, True, False, reader.Masters, reader.Records.Count + 4, &H900UI, "controlled traits fixture", False)
        Dim allBytes = header.ToList()
        For Each group In reader.Records.Values.GroupBy(Function(r) r.Header.Signature)
            Dim members As New List(Of Byte())
            For Each record In group
                members.Add(GenericPluginWriter.WrapRecord(record.Header.Signature, rewrite(record), record.Header.Flags, record.Header.FormID, record.Header.VCS1, record.Header.VCS2, Config_App.Game_Enum.Fallout4))
            Next
            allBytes.AddRange(GenericPluginWriter.BuildGrup(group.Key, members))
        Next
        File.WriteAllBytes(input, allBytes.ToArray())
    End Sub
    Private Function RecordBytes(record As PluginRecord) As Byte()
        Return record.Subrecords.SelectMany(Function(part) Sr(part.Signature, part.Data)).ToArray()
    End Function
    Private Sub TraitsChecks(root As String)
        Dim data = Directory.CreateDirectory(Path.Combine(root, "traits-inputs")).FullName
        Dim order = {"Fallout4.esm", "Fixture.esm"}
        Fixture(data, twoNpcs:=True, genericHuman:=True)
        Dim script = New Byte() {6, 0, 2, 0, 1, 0, 5, 0, 80, 114, 111, 98, 101, 0, 0, 0}
        RewriteFixture(data,
            Function(record)
                If record.Header.Signature <> "NPC_" OrElse record.Header.FormID <> &H1000804UI Then Return RecordBytes(record)
                Dim parts As New List(Of Byte)
                For Each part In record.Subrecords
                    Dim bytes = part.Data.ToArray()
                    If part.Signature = "ACBS" Then Array.Copy(BitConverter.GetBytes(1US), 0, bytes, 14, 2)
                    If part.Signature = "VMAD" Then bytes = script
                    If part.Signature = "RNAM" OrElse part.Signature = "VTCK" Then bytes = BitConverter.GetBytes(0UI)
                    If part.Signature = "VTCK" Then parts.AddRange(Sr("SNAM", BitConverter.GetBytes(&H1000805UI).Concat(New Byte() {0}).ToArray()))
                    parts.AddRange(Sr(part.Signature, bytes))
                    If part.Signature = "VTCK" Then parts.AddRange(Sr("TPLT", BitConverter.GetBytes(&H1000800UI)))
                    If part.Signature = "RNAM" Then
                        parts.AddRange(Sr("COCT", BitConverter.GetBytes(1UI)))
                        parts.AddRange(Sr("CNTO", BitConverter.GetBytes(&H1000806UI).Concat(BitConverter.GetBytes(1UI)).ToArray()))
                    End If
                    If part.Signature = "AIDT" Then parts.AddRange(Sr("PKID", BitConverter.GetBytes(&H1000807UI)))
                Next
                Return parts.ToArray()
            End Function)
        Dim fixturePath = Path.Combine(data, "Fixture.esm")
        Dim expanded = File.ReadAllBytes(fixturePath).ToList()
        Dim kinds = {"FACT", "ARMO", "PACK"}
        For i = 0 To kinds.Length - 1
            Dim extra = GenericPluginWriter.WrapRecord(kinds(i), Sr("EDID", Text("Unrelated" & kinds(i))), 0UI, &H1000805UI + CUInt(i), 0UI, 0US, Config_App.Game_Enum.Fallout4)
            expanded.AddRange(GenericPluginWriter.BuildGrup(kinds(i), New List(Of Byte()) From {extra}))
        Next
        Dim inputBytes = expanded.ToArray()
        Array.Copy(BitConverter.GetBytes(15UI), 0, inputBytes, 34, 4)
        File.WriteAllBytes(fixturePath, inputBytes)
        Dim patch = WriteRecordOnlyPatch(data, order, "Fixture.esm", &H804UI, Path.Combine(root, "traits-positive"), AgeEvidence.Adult, "controlled adult target")
        Dim reader As New PluginReader() : reader.Load(patch)
        Dim pm As New PluginManager() : pm.LoadAllPlugins(data, order)
        Dim written = reader.Records.Values.Single()
        Check(BitConverter.ToUInt16(written.GetSubrecord("ACBS").Value.Data, 14) = 0US, "original materializer clears only verified Traits inheritance")
        Check(pm.ResolveFormID(BitConverter.ToUInt32(written.GetSubrecord("RNAM").Value.Data, 0), reader) = &H1000801UI AndAlso pm.ResolveFormID(BitConverter.ToUInt32(written.GetSubrecord("VTCK").Value.Data, 0), reader) = &H1000802UI, "materialized effective race and voice survive master remapping")
        Check(written.GetSubrecord("VMAD").Value.Data.SequenceEqual(script), "materializing Traits preserves target nonempty VMAD rather than terminal script")
        Check(pm.ResolveFormID(BitConverter.ToUInt32(written.GetSubrecord("SNAM").Value.Data, 0), reader) = &H1000805UI, "Traits materialization preserves target faction")
        Check(pm.ResolveFormID(BitConverter.ToUInt32(written.GetSubrecord("CNTO").Value.Data, 0), reader) = &H1000806UI AndAlso BitConverter.ToUInt32(written.GetSubrecord("CNTO").Value.Data, 4) = 1UI, "Traits materialization preserves target inventory")
        Check(pm.ResolveFormID(BitConverter.ToUInt32(written.GetSubrecord("PKID").Value.Data, 0), reader) = &H1000807UI, "Traits materialization preserves target package")
        Check(File.ReadAllBytes(Path.Combine(data, "Fixture.esm")).SequenceEqual(inputBytes), "template source and inheriting original plugin unchanged")
        Dim campaign As New CampaignInput With {.DataRoot = data, .LoadOrder = order.ToList(), .ProtectedRoots = New List(Of String) From {data}}
        campaign.Policy.HumanRaces.Add("FIXTURE.ESM|000801")
        campaign.Policy.AdultEvidence.Add("FIXTURE.ESM|000804", "controlled adult")
        Dim plan = GlobalNpcDiscovery.Scan(campaign)
        Check(plan.Decisions.Single(Function(d) d.Npc.ObjectId = &H804UI).Npc.TraitsMaterialized, "global classification uses materialized effective Traits without mutating source")
        AdditionalInheritanceChecks(root, data, inputBytes, script)
        RewriteFixture(data,
            Function(record)
                Dim parts = RecordBytes(record).ToList()
                If record.Header.Signature = "NPC_" AndAlso record.Header.FormID = &H1000800UI Then
                    parts.Clear()
                    For Each part In record.Subrecords
                        Dim bytes = part.Data.ToArray()
                        If part.Signature = "ACBS" Then Array.Copy(BitConverter.GetBytes(1US), 0, bytes, 14, 2)
                        parts.AddRange(Sr(part.Signature, bytes))
                        If part.Signature = "VTCK" Then parts.AddRange(Sr("TPLT", BitConverter.GetBytes(&H1000804UI)))
                    Next
                End If
                Return parts.ToArray()
            End Function)
        Reject(Sub() WriteRecordOnlyPatch(data, order, "Fixture.esm", &H804UI, Path.Combine(root, "traits-cycle"), AgeEvidence.Adult, "controlled adult"), "original chain cycle detection prevents autonomous template export", "cyclic")
        File.WriteAllBytes(Path.Combine(data, "Fixture.esm"), inputBytes)
        RewriteFixture(data, Function(record) If(record.Header.FormID = &H1000804UI, record.Subrecords.SelectMany(Function(part) Sr(part.Signature, If(part.Signature = "TPLT", BitConverter.GetBytes(&H1000FFFUI), part.Data))).ToArray(), RecordBytes(record)))
        Reject(Sub() WriteRecordOnlyPatch(data, order, "Fixture.esm", &H804UI, Path.Combine(root, "traits-incomplete"), AgeEvidence.Adult, "controlled adult"), "dead Traits pointer cannot use editor fallback", "Incomplete Traits")
        File.WriteAllBytes(Path.Combine(data, "Fixture.esm"), inputBytes)
        RewriteFixture(data, Function(record) If(record.Header.FormID = &H1000804UI, RecordBytes(record).Concat(Sr("TPLT", BitConverter.GetBytes(&H1000800UI))).ToArray(), RecordBytes(record)))
        Reject(Sub() WriteRecordOnlyPatch(data, order, "Fixture.esm", &H804UI, Path.Combine(root, "traits-ambiguous"), AgeEvidence.Adult, "controlled adult"), "duplicate template pointer is rejected as ambiguous", "Ambiguous duplicate")
        Check(Not Directory.Exists(Path.Combine(root, "traits-cycle")) AndAlso Not Directory.Exists(Path.Combine(root, "traits-incomplete")), "invalid template chains write no output directories")
    End Sub

    Private Sub AdditionalInheritanceChecks(root As String, data As String, originalBytes As Byte(), script As Byte())
        Dim input = Path.Combine(data, "Fixture.esm")
        Dim mask As UShort = 0US
        For Each category In {NPC_TemplateCategory.Traits, NPC_TemplateCategory.Factions, NPC_TemplateCategory.Inventory, NPC_TemplateCategory.AIData}
            mask = mask Or CUShort(1 << CInt(category))
        Next
        RewriteFixture(data,
            Function(record)
                If record.Header.Signature <> "NPC_" Then Return RecordBytes(record)
                Dim parts As New List(Of Byte)
                For Each part In record.Subrecords
                    Dim bytes = part.Data.ToArray()
                    If record.Header.FormID = &H1000804UI AndAlso part.Signature = "ACBS" Then Array.Copy(BitConverter.GetBytes(mask), 0, bytes, 14, 2)
                    If record.Header.FormID = &H1000800UI AndAlso part.Signature = "AIDT" Then bytes(0) = 1
                    If record.Header.FormID = &H1000800UI AndAlso part.Signature = "VTCK" Then parts.AddRange(Sr("SNAM", BitConverter.GetBytes(&H1000805UI).Concat(New Byte() {2}).ToArray()))
                    parts.AddRange(Sr(part.Signature, bytes))
                    If record.Header.FormID = &H1000800UI AndAlso part.Signature = "RNAM" Then
                        parts.AddRange(Sr("COCT", BitConverter.GetBytes(1UI)))
                        parts.AddRange(Sr("CNTO", BitConverter.GetBytes(&H1000806UI).Concat(BitConverter.GetBytes(3UI)).ToArray()))
                    End If
                Next
                Return parts.ToArray()
            End Function)
        Dim inputSnapshot = File.ReadAllBytes(input)
        Dim patch = WriteRecordOnlyPatch(data, {"Fallout4.esm", "Fixture.esm"}, "Fixture.esm", &H804UI, Path.Combine(root, "inherited-buckets"), AgeEvidence.Adult, "controlled adult")
        Dim reader As New PluginReader() : reader.Load(patch)
        Dim written = reader.Records.Values.Single()
        Check(BitConverter.ToUInt16(written.GetSubrecord("ACBS").Value.Data, 14) = 0US, "combined verified inherited buckets become owned before transformation")
        Check(written.GetSubrecord("SNAM").Value.Data(4) = 2, "effective inherited faction rank replaces target own rank")
        Check(BitConverter.ToUInt32(written.GetSubrecord("CNTO").Value.Data, 4) = 3UI, "effective inherited inventory count preserved through original ESP writer")
        Check(written.GetSubrecord("AIDT").Value.Data(0) = 1, "original AIData materializer preserves effective behavior")
        Check(written.GetSubrecord("VMAD").Value.Data.SequenceEqual(script) AndAlso written.GetSubrecord("PKID").HasValue, "additional materialization preserves target scripts and noninherited packages")
        Check(File.ReadAllBytes(input).SequenceEqual(inputSnapshot), "additional category export leaves original plugin untouched")
        Using report = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root, "inherited-buckets", "verification.json")))
            Check(report.RootElement.GetProperty("templateCategoriesMaterialized").GetArrayLength() = 4, "export evidence identifies each inherited category rather than calling all inheritance Traits")
        End Using
        RewriteFixture(data, Function(record) If(record.Header.FormID = &H1000804UI, record.Subrecords.SelectMany(Function(part) Sr(part.Signature, If(part.Signature = "TPLT", BitConverter.GetBytes(&H1000FFFUI), part.Data))).ToArray(), RecordBytes(record)))
        Reject(Sub() WriteRecordOnlyPatch(data, {"Fallout4.esm", "Fixture.esm"}, "Fixture.esm", &H804UI, Path.Combine(root, "inherited-dead"), AgeEvidence.Adult, "controlled adult"), "combined inheritance rejects incomplete category source")
        File.WriteAllBytes(input, inputSnapshot)
        RewriteFixture(data, Function(record)
                                 If record.Header.FormID <> &H1000804UI Then Return RecordBytes(record)
                                 Return record.Subrecords.SelectMany(Function(part)
                                                                          Dim bytes = part.Data.ToArray()
                                                                          If part.Signature = "ACBS" Then Array.Copy(BitConverter.GetBytes(CUShort(&HFFFFUS)), 0, bytes, 14, 2)
                                                                          Return Sr(part.Signature, bytes)
                                                                      End Function).ToArray()
                             End Function)
        Reject(Sub() WriteRecordOnlyPatch(data, {"Fallout4.esm", "Fixture.esm"}, "Fixture.esm", &H804UI, Path.Combine(root, "inherited-unsupported"), AgeEvidence.Adult, "controlled adult"), "unverified inherited categories remain blocked", "Unverified inherited category")
        File.WriteAllBytes(input, originalBytes)
    End Sub

    Private Sub NarrativeChecks(root As String)
        Dim data = Directory.CreateDirectory(Path.Combine(root, "narrative-inputs")).FullName
        Fixture(data, genericHuman:=True)
        Dim game = Config_App.Game_Enum.Fallout4
        Dim questBody = Sr("EDID", Text("SyntheticQuest")) _
            .Concat(Sr("DNAM", New Byte(11) {})) _
            .Concat(Sr("ANAM", BitConverter.GetBytes(1UI))) _
            .Concat(Sr("ALST", BitConverter.GetBytes(0UI))) _
            .Concat(Sr("ALID", Text("ActorAlias"))) _
            .Concat(Sr("FNAM", BitConverter.GetBytes(0UI))) _
            .Concat(Sr("ALUA", BitConverter.GetBytes(&H800UI))) _
            .Concat(Sr("ALED", Array.Empty(Of Byte)())).ToArray()
        Dim quest = GenericPluginWriter.WrapRecord("QUST", questBody, 0UI, &H1000800UI, 0UI, 0US, game)
        Dim sceneBody = Sr("EDID", Text("SyntheticScene")).Concat(Sr("PNAM", BitConverter.GetBytes(&H1000800UI))).ToArray()
        Dim scene = GenericPluginWriter.WrapRecord("SCEN", sceneBody, 0UI, &H1000801UI, 0UI, 0US, game)
        Dim relationBody = Sr("EDID", Text("SyntheticRelation")).Concat(Sr("DATA", BitConverter.GetBytes(&H800UI).Concat(BitConverter.GetBytes(&H800UI)).Concat(New Byte(7) {}).ToArray())).ToArray()
        Dim relation = GenericPluginWriter.WrapRecord("RELA", relationBody, 0UI, &H1000802UI, 0UI, 0US, game)
        Dim header = GenericPluginWriter.BuildTes4Header(game, False, False, New List(Of String) From {"Fixture.esm"}, 6, &H803UI, "static graph fixture", False)
        File.WriteAllBytes(Path.Combine(data, "Narrative.esp"), header.Concat(GenericPluginWriter.BuildGrup("QUST", New List(Of Byte()) From {quest})).Concat(GenericPluginWriter.BuildGrup("SCEN", New List(Of Byte()) From {scene})).Concat(GenericPluginWriter.BuildGrup("RELA", New List(Of Byte()) From {relation})).ToArray())
        Dim campaign As New CampaignInput With {.DataRoot = data, .LoadOrder = New List(Of String) From {"Fallout4.esm", "Fixture.esm", "Narrative.esp"}, .ProtectedRoots = New List(Of String) From {data}}
        campaign.Policy.HumanRaces.Add("FIXTURE.ESM|000801")
        campaign.Policy.AdultEvidence.Add("FIXTURE.ESM|000800", "controlled adult")
        Dim report = NarrativeDependencies.Analyze(campaign)
        Check(report.Edges.Any(Function(e) e.Signature = "QUST" AndAlso e.Target = "FIXTURE.ESM|000800" AndAlso e.Subrecord = "ALUA"), "original canonical walker discovers quest unique-actor alias reference")
        Check(report.Edges.Any(Function(e) e.Signature = "SCEN" AndAlso e.Target = "NARRATIVE.ESP|000800"), "original canonical walker discovers scene quest dependency")
        Check(report.Edges.Any(Function(e) e.Signature = "RELA" AndAlso e.Target = "FIXTURE.ESM|000800"), "original canonical walker discovers relationship actor references")
        Check(report.Npcs.Single().Upstream.Contains("NARRATIVE.ESP|000801") AndAlso report.Npcs.Single().ReviewRequired, "reverse transitive analysis requires review for quest-linked generic NPC")
        Check(report.Npcs.Single().Voice = "FIXTURE.ESM|000802" AndAlso report.Status.Contains("NOT-QUEST-COMPATIBILITY"), "static impact records effective voice without asserting quest compatibility")
        Dim reviewed = NarrativeDependencies.DiscoverReviewed(campaign)
        Check(reviewed.NarrativeAnalyzed AndAlso reviewed.Decisions.Single().Npc.NarrativeState <> "not-analyzed", "narrative findings participate in global classification seal")
        Check(reviewed.Decisions.Single().Route <> "adult-human-production-pipeline" AndAlso reviewed.Pages.Count = 0, "quest-linked or incomplete narrative analysis cannot enter generic batch pages")
        Reject(Sub() NarrativeDependencies.Analyze(campaign, 1), "narrative edge budget fails closed")
        Dim reportPath = Path.Combine(root, "narrative-report.json")
        GlobalNpcDiscovery.WriteNarrativeReport(campaign, report, reportPath)
        Reject(Sub() GlobalNpcDiscovery.WriteNarrativeReport(campaign, report, reportPath), "narrative report cannot overwrite prior evidence")
    End Sub

    Private Sub AssetValidationChecks(root As String)
        Dim dds = DirectXTextureConversionHelper.Bgra32BytesToDdsBytes(4, 4, New Byte(63) {}, 28)
        Dim valid = LocalAuthenticTrial.ValidateAsset(dds, ".dds")
        Check(valid.Width = 4 AndAlso valid.Height = 4 AndAlso valid.Sha256.Length = 64, "native DDS encoder and decoder validate bounded controlled texture (not authentic FaceGen)")
        Reject(Sub() LocalAuthenticTrial.ValidateAsset(dds.Take(128).ToArray(), ".dds"), "truncated DDS rejected before resource admission")
        Dim excessive = dds.ToArray()
        Array.Copy(BitConverter.GetBytes(65536UI), 0, excessive, 16, 4)
        Reject(Sub() LocalAuthenticTrial.ValidateAsset(excessive, ".dds"), "DDS allocation bounds enforced before native decoder")
        Reject(Sub() LocalAuthenticTrial.ValidateAsset(New Byte() {1, 2, 3}, ".nif"), "original NIF reader rejects invalid resource")
        Reject(Sub() LocalAuthenticTrial.ValidateAsset(dds, ".nif"), "DDS cannot masquerade as FaceGen NIF")
        Reject(Sub() LocalAuthenticTrial.Inspect(New AuthenticTrialInput()), "incomplete authentic trial never produces conversion evidence")
        File.WriteAllText(Path.Combine(root, "invalid-authentic-trial.json"), JsonSerializer.Serialize(New AuthenticTrialInput()))
        Dim sourceArchive = Path.Combine(root, "selected-authentic-resource-fixture.ba2")
        File.WriteAllBytes(sourceArchive, Ba2WriterGNRL.Build(New List(Of VirtualEntry) From {New VirtualEntry With {.Directory = "Textures/Face", .FileName = "head.dds", .Data = dds}}, New Ba2WriterGNRL.Options With {.Version = 1UI}))
        Dim sourceHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sourceArchive)))
        Dim selectedResource As New TrialResource With {.Role = "facegen-dds", .FilePath = sourceArchive, .Sha256 = sourceHash, .ResourcePath = "Textures/Face/head.dds"}
        Dim packedAsset = LocalAuthenticTrial.ValidateSelectedResource(selectedResource)
        Check(packedAsset.Width = 4 AndAlso packedAsset.SourceSha256 = sourceHash AndAlso packedAsset.Sha256 = valid.Sha256, "original BA2 reader validates selected DDS member and records source/member provenance")
        Check(Not Directory.Exists(Path.Combine(root, "Textures")) AndAlso Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sourceArchive))) = sourceHash, "authentic resource preflight never extracts archive to disk or changes source")
        Reject(Sub() LocalAuthenticTrial.ValidateSelectedResource(selectedResource, 1), "selected asset pool exhaustion rejects before extraction")
        selectedResource.ResourcePath = "Textures/Face/absent.dds"
        Reject(Sub() LocalAuthenticTrial.ValidateSelectedResource(selectedResource), "missing authentic resource member cannot use fallback")
        selectedResource.ResourcePath = "Textures/Face/head.dds" : selectedResource.Sha256 = New String("0"c, 64)
        Reject(Sub() LocalAuthenticTrial.ValidateSelectedResource(selectedResource), "authentic source archive hash mismatch rejected")
        Dim cubeHeader = DirectXTexWrapperCLI.Loader.EncodeDDSHeader(98, 4, 4, 1, 1, False)
        Check(cubeHeader.Length = 148 AndAlso Encoding.ASCII.GetString(cubeHeader, 84, 4) = "DX10", "original DDS header API provides DX10 validation fixture")
        Dim cube = cubeHeader.Concat(New Byte(15) {}).ToArray()
        Array.Copy(BitConverter.GetBytes(4UI), 0, cube, 136, 4)
        Reject(Sub() LocalAuthenticTrial.ValidateAsset(cube, ".dds"), "DX10 cube flag cannot bypass single-face trial guard")
        Dim stage = Path.Combine(root, "asset-quota")
        Dim outside = Path.Combine(root, "asset-quota-escape.dds")
        Using budget = ReservedWriteBudget.Create(stage, 8192, {Path.Combine(root, "inputs")})
            Using scope = FaceGenOutputAdapter.BeginControlledOutput(budget)
                FaceGenOutputAdapter.EnsureDirectory(Path.Combine(stage, "Data"))
                Check(Not Directory.Exists(Path.Combine(stage, "Data")), "FaceGen directory creation deferred until payload reservation")
                FaceGenOutputAdapter.WriteBytes(Path.Combine(stage, "Data", "face.dds"), dds)
                Check(budget.VerifiedNames.Contains("Data/face.dds"), "actual FaceGen DDS output adapter uses reserved completion checkpoint")
                Reject(Sub() FaceGenOutputAdapter.WriteBytes(outside, dds), "FaceGen output adapter rejects outside path before writing")
                Check(Not File.Exists(outside), "controlled FaceGen cannot create an escaped output")
                Reject(Sub() FaceGenOutputAdapter.WriteBytes(Path.Combine(stage, "Data", "over.dds"), New Byte(8191) {}), "actual FaceGen byte adapter enforces quota before payload")
                Check(Not File.Exists(Path.Combine(stage, "Data", "over.dds")), "quota failure produces no oversized FaceGen file")
                Reject(Sub() FaceGenOutputAdapter.RejectUncontrolledDiagnostic(), "TGA diagnostic path fails closed in controlled output")
                Reject(Sub() FaceGenOutputAdapter.BeginControlledOutput(budget), "concurrent FaceGen resource scopes rejected")
            End Using
            Reject(Sub() FaceGenOutputAdapter.WriteBytes(outside, dds), "closed controlled FaceGen scope never falls back to unbudgeted writer")
        End Using

    End Sub
    Private Sub ControlledPipelineChecks(root As String)
        Dim data = Directory.CreateDirectory(Path.Combine(root, "controlled-inputs")).FullName
        Fixture(data, genericHuman:=True)
        Dim original = File.ReadAllBytes(Path.Combine(data, "Fixture.esm"))
        Dim profile As New NpcBatchProfile With {.DataRoot = data, .LoadOrder = New List(Of String) From {"Fallout4.esm", "Fixture.esm"}, .ProtectedRoots = New List(Of String) From {data}, .RequireFaceGen = False,
            .Targets = New List(Of NpcBatchTarget) From {New NpcBatchTarget With {.SourcePlugin = "Fixture.esm", .ObjectId = &H800UI, .Age = AgeEvidence.Adult, .AdultEvidence = "controlled adult"}}}
        File.WriteAllText(Path.Combine(root, "controlled-cli-profile.json"), JsonSerializer.Serialize(profile))
        Dim workspace = Path.Combine(root, "controlled-pipeline")
        Dim result = ProductionNpcBatch.RunControlled(profile, workspace, 1024 * 1024)
        Check(result.BundleStatus = "RECORDS-ONLY-BUNDLE-NOT-A-COMPLETE-CONVERSION", "original ESP serializer and aggregate run through controlled output interface")
        Using budget = ReservedWriteBudget.Recover(workspace)
            Dim files = Directory.EnumerateFiles(workspace, "*", SearchOption.AllDirectories).Where(Function(p) Path.GetFileName(p) <> "budget-ledger.jsonl").ToArray()
            Check(files.All(Function(p) budget.VerifiedNames.Contains(Path.GetRelativePath(workspace, p).Replace(Path.DirectorySeparatorChar, "/"c))), "every controlled ESP checkpoint copy and report has durable quota completion")
            Check(files.Any(Function(p) p.EndsWith(".esp", StringComparison.Ordinal)) AndAlso files.All(Function(p) Not p.EndsWith(".bak", StringComparison.OrdinalIgnoreCase)), "controlled writer bypasses legacy backup/temp path operations")
        End Using
        Dim resumed = ProductionNpcBatch.RunControlled(profile, workspace, 1024 * 1024, True)
        Check(resumed.Items.Single().Status = "RESUMED-VERIFIED-OUTPUTS", "quota-bound original batch journal resumes verified NPC")
        Check(Directory.EnumerateFiles(workspace, "*", SearchOption.AllDirectories).Sum(Function(p) New FileInfo(p).Length) <= 1024 * 1024, "all controlled batch file bytes and ledger under aggregate ceiling")
        Dim exhausted = Path.Combine(root, "controlled-exhaustion")
        Try
            Dim blocked = ProductionNpcBatch.RunControlled(profile, exhausted, 4096)
            Check(blocked.BundleStatus.StartsWith("BLOCKED", StringComparison.Ordinal), "small quota cannot declare a complete controlled NPC bundle")
        Catch ex As IOException
            Check(True, "quota exhaustion stops pipeline before complete report")
        End Try
        Using budget = ReservedWriteBudget.Recover(exhausted)
            Check(Directory.EnumerateFiles(exhausted, "*", SearchOption.AllDirectories).Sum(Function(p) New FileInfo(p).Length) <= 4096, "exhausted pipeline never exceeds reserved aggregate file-byte quota")
        End Using
        profile.RequireFaceGen = True
        Reject(Sub() ProductionNpcBatch.RunControlled(profile, Path.Combine(root, "unadmitted-facegen"), 1024 * 1024), "unverified native FaceGen routes blocked before workspace creation")
        Check(Not Directory.Exists(Path.Combine(root, "unadmitted-facegen")), "no output created for unadmitted native trial")
        Check(File.ReadAllBytes(Path.Combine(data, "Fixture.esm")).SequenceEqual(original), "controlled exhaustion recovery and export preserve original plugin")
    End Sub
    Private Sub UnmodeledRecordChecks(root As String)
        Dim data = Directory.CreateDirectory(Path.Combine(root, "unmodeled-inputs")).FullName
        Fixture(data, genericHuman:=True)
        RewriteFixture(data, Function(record) If(record.Header.Signature = "NPC_", RecordBytes(record).Concat(Sr("ZZZZ", BitConverter.GetBytes(&H1000802UI))).ToArray(), RecordBytes(record)))
        Dim output = Path.Combine(root, "unmodeled-output")
        Reject(Sub() WriteRecordOnlyPatch(data, {"Fallout4.esm", "Fixture.esm"}, "Fixture.esm", &H800UI, output, AgeEvidence.Adult, "controlled adult"), "opaque unknown NPC field cannot masquerade as proven global reference preservation", "canonical parse findings")
        Check(Not Directory.Exists(output), "unreviewed canonical input produces no patch directory")
    End Sub

    Function Main(args As String()) As Integer
        Try
            If args.Length <> 1 OrElse Directory.Exists(args(0)) Then Throw New ArgumentException("Expected new staging root.")
            Dim root = Path.GetFullPath(args(0))
            Dim data = Directory.CreateDirectory(Path.Combine(root, "inputs")).FullName
            File.WriteAllText(Path.Combine(root, "EXPERIMENTAL-FIXTURES-DO-NOT-INSTALL.txt"), "Generated test plugins and placeholder Fallout4.esm only. No real game or FaceGen validation. Never install this artifact.")
            Config_App.Current.Game = Config_App.Game_Enum.Fallout4
            Fixture(data)
            Dim order = {"Fallout4.esm", "Fixture.esm"}
            Dim sourceHash = SHA256.HashData(File.ReadAllBytes(Path.Combine(data, "Fixture.esm")))
            Dim before As New PluginReader() : before.Load(Path.Combine(data, "Fixture.esm"))
            Check(before.Records.Values.Any(Function(r) r.Header.Signature = "NPC_"), "existing reader loads fixture NPC_")
            Dim patch = WriteRecordOnlyPatch(data, order, "Fixture.esm", &H800UI, Path.Combine(root, "patch"), AgeEvidence.Adult, "generated-adult-fixture")
            Dim after As New PluginReader() : after.Load(patch)
            Check(after.Records.Count = 1, "one isolated override")
            Check(after.Masters.SequenceEqual({"Fixture.esm"}, StringComparer.OrdinalIgnoreCase), "unused master removed by existing writer")
            Dim verifyPm As New PluginManager()
            verifyPm.LoadAllPlugins(data, order)
            Dim written = after.Records.Values.Single()
            Check(written.Header.FormID = &H800UI, "file-local master index remapped")
            Check(verifyPm.ResolveFormID(written.Header.FormID, after) = &H1000800UI, "global NPC identity preserved across remap")
            Check((BitConverter.ToUInt32(written.GetSubrecord("ACBS").Value.Data, 0) And 1UI) = 1UI, "female flag serialized")
            Check(BitConverter.ToUInt32(written.GetSubrecord("ACBS").Value.Data, 0) = &H80000003UI, "unrelated flags preserved")
            Check(written.GetSubrecord("VMAD").Value.Data.SequenceEqual(New Byte() {6, 0, 2, 0, 0, 0}), "script bytes preserved")
            Check(verifyPm.ResolveFormID(BitConverter.ToUInt32(written.GetSubrecord("VTCK").Value.Data, 0), after) = &H1000802UI, "voice identity preserved across remap")
            Check(verifyPm.ResolveFormID(BitConverter.ToUInt32(written.GetSubrecord("RNAM").Value.Data, 0), after) = &H1000801UI, "race identity preserved across remap")
            Check(sourceHash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(Path.Combine(data, "Fixture.esm")))), "source bytes untouched")
            Check(File.Exists(Path.Combine(root, "patch", "verification.json")), "verification manifest emitted")
            Reject(Sub() WriteRecordOnlyPatch(data, order, "Fixture.esm", &H800UI, Path.Combine(root, "patch"), AgeEvidence.Adult, "fixture"), "existing output refused")
            Reject(Sub() WriteRecordOnlyPatch(data, order, "Fixture.esm", &H800UI, Path.Combine(data, "nested"), AgeEvidence.Adult, "fixture"), "input tree protected")
            Reject(Sub() WriteRecordOnlyPatch(data, order, "Fixture.esm", &H800UI, Path.Combine(root, "unknown"), AgeEvidence.Unknown, "fixture"), "unknown age refused")
            Reject(Sub() WriteRecordOnlyPatch(data, order, "Fixture.esm", &H800UI, Path.Combine(root, "child"), AgeEvidence.Child, "fixture"), "child refused")
            Reject(Sub() WriteRecordOnlyPatch(data, order, "Fixture.esm", &H800UI, Path.Combine(root, "no-evidence"), AgeEvidence.Adult, ""), "missing adult evidence refused")
            Reject(Sub() WriteRecordOnlyPatch(data, order, "Fixture.esm", &H999UI, Path.Combine(root, "absent"), AgeEvidence.Adult, "fixture"), "missing NPC refused")
            Reject(Sub() WriteRecordOnlyPatch(data, {"Fixture.esm"}, "Fixture.esm", &H800UI, Path.Combine(root, "masters"), AgeEvidence.Adult, "fixture"), "missing master refused")
            Reject(Sub() WriteRecordOnlyPatch(data, {"Fixture.esm", "Fallout4.esm"}, "Fixture.esm", &H800UI, Path.Combine(root, "order"), AgeEvidence.Adult, "fixture"), "incorrect master order refused")
            Dim inherited = Directory.CreateDirectory(Path.Combine(root, "inherited")).FullName
            Fixture(inherited, 1US)
            Reject(Sub() WriteRecordOnlyPatch(inherited, order, "Fixture.esm", &H800UI, Path.Combine(root, "inherited-patch"), AgeEvidence.Adult, "fixture"), "unmaterialized traits refused")
            Dim presetPath = Path.Combine(root, "female-preset.json")
            File.WriteAllText(presetPath, "{""Gender"":1,""Morphs"":{""Presets"":{""12345678"":0.25}}}")
            Dim appearancePatch = WriteAppearancePatch(data, order, "Fixture.esm", &H800UI, Path.Combine(root, "appearance"),
                AgeEvidence.Adult, "generated adult human fixture", presetPath, "Fixture.esm", &H801UI)
            Dim appearanceReader As New PluginReader() : appearanceReader.Load(appearancePatch)
            Dim appearanceRecord = appearanceReader.Records.Values.Single()
            Check(appearanceRecord.Subrecords.Any(Function(sr) sr.Signature = "MSDV"), "production preset face morph serialized")
            Check(appearanceRecord.GetSubrecord("VMAD").Value.Data.SequenceEqual(New Byte() {6, 0, 2, 0, 0, 0}), "appearance overlay preserves script bytes")
            Reject(Sub() WriteAppearancePatch(data, order, "Fixture.esm", &H800UI, Path.Combine(root, "wrong-race"),
                AgeEvidence.Adult, "fixture", presetPath, "Fixture.esm", &H802UI), "unapproved race excluded")
            File.WriteAllText(presetPath, "{""Gender"":0,""Morphs"":{""Presets"":{""12345678"":0.25}}}")
            Reject(Sub() WriteAppearancePatch(data, order, "Fixture.esm", &H800UI, Path.Combine(root, "male-preset"),
                AgeEvidence.Adult, "fixture", presetPath, "Fixture.esm", &H801UI), "male appearance excluded")
            File.WriteAllText(presetPath, "{""Gender"":1,""Morphs"":{""Presets"":{""12345678"":0.25}}}")
            Dim profile As New NpcBatchProfile With {.DataRoot = data, .LoadOrder = order.ToList(), .ProtectedRoots = New List(Of String) From {data}, .RequireFaceGen = False,
                .Targets = New List(Of NpcBatchTarget) From {
                    New NpcBatchTarget With {.SourcePlugin = "Fixture.esm", .ObjectId = &H999UI, .Age = AgeEvidence.Unknown},
                    New NpcBatchTarget With {.SourcePlugin = "Fixture.esm", .ObjectId = &H800UI, .Age = AgeEvidence.Adult, .AdultEvidence = "generated fixture",
                        .AppearancePreset = presetPath, .HumanRacePlugin = "Fixture.esm", .HumanRaceObjectId = &H801UI}}}
            Dim batch = ProductionNpcBatch.Run(profile, root)
            Check(String.IsNullOrEmpty(batch.BundleDirectory) AndAlso batch.Items(0).Status = "BLOCKED-PARTIAL-NOT-INSTALLED", "batch records failure without marking success")
            Check(batch.Items(1).Status = "APPEARANCE-RECORD-VERIFIED-NO-FACEGEN", "batch continues through real appearance writer")
            Dim resumed = ProductionNpcBatch.Run(profile, root, batch.StagingDirectory)
            Check(resumed.Items(1).Status = "RESUMED-VERIFIED-OUTPUTS", "batch resumes verified result without rewriting")
            File.AppendAllText(presetPath, " ")
            Reject(Sub() ProductionNpcBatch.Run(profile, root, batch.StagingDirectory), "resume seals preset content")
            Check(sourceHash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(Path.Combine(data, "Fixture.esm")))), "batch preserves original plugin")
            profile.Targets.RemoveAt(0)
            profile.RequireFaceGen = True
            Dim missingResources = ProductionNpcBatch.Run(profile, root)
            Check(missingResources.Items.Single().Status = "BLOCKED-PARTIAL-NOT-INSTALLED", "missing real FaceGen resources cannot authorize conversion")
            Dim snapshots = Directory.GetFiles(Path.Combine(missingResources.StagingDirectory, ".batch-journal"), "*.json")
            Check(snapshots.Length = 1, "failed FaceGen attempt never checkpointed")
            Dim multiData = Directory.CreateDirectory(Path.Combine(root, "multi-inputs")).FullName
            Fixture(multiData, twoNpcs:=True)
            Dim multiProfile As New NpcBatchProfile With {.DataRoot = multiData, .LoadOrder = order.ToList(), .ProtectedRoots = New List(Of String) From {multiData}, .RequireFaceGen = False,
                .Targets = New List(Of NpcBatchTarget) From {
                    New NpcBatchTarget With {.SourcePlugin = "Fixture.esm", .ObjectId = &H800UI, .Age = AgeEvidence.Adult, .AdultEvidence = "generated fixture", .AppearancePreset = presetPath, .HumanRacePlugin = "Fixture.esm", .HumanRaceObjectId = &H801UI},
                    New NpcBatchTarget With {.SourcePlugin = "Fixture.esm", .ObjectId = &H804UI, .Age = AgeEvidence.Adult, .AdultEvidence = "generated fixture", .AppearancePreset = presetPath, .HumanRacePlugin = "Fixture.esm", .HumanRaceObjectId = &H801UI}}}
            Dim aggregate = ProductionNpcBatch.Run(multiProfile, root)
            Check(aggregate.BundleStatus = "RECORDS-ONLY-BUNDLE-NOT-A-COMPLETE-CONVERSION", "two target batch exports one verified aggregate: " & aggregate.Status)
            Dim aggregateReader As New PluginReader() : aggregateReader.Load(Path.Combine(aggregate.BundleDirectory, "EXPERIMENTAL-NPC-Batch.esp"))
            Check(aggregateReader.Records.Count = 2, "aggregate contains both NPC overrides")
            Check(aggregateReader.Records.Values.All(Function(r) (BitConverter.ToUInt32(r.GetSubrecord("ACBS").Value.Data, 0) And 1UI) = 1UI AndAlso r.Subrecords.Any(Function(sr) sr.Signature = "MSDV")), "aggregate retains female flags and appearance for both NPCs")
            Dim aggregateResume = ProductionNpcBatch.Run(multiProfile, root, aggregate.StagingDirectory)
            Check(aggregateResume.BundleDirectory = aggregate.BundleDirectory, "resume reuses hash-verified aggregate without rewriting")
            File.AppendAllText(Path.Combine(aggregate.BundleDirectory, "EXPERIMENTAL-NPC-Batch.esp"), "tamper")
            Reject(Sub() ProductionNpcBatch.Run(multiProfile, root, aggregate.StagingDirectory), "tampered aggregate blocks resume")
            File.WriteAllText(Path.Combine(root, "cli-profile.json"), JsonSerializer.Serialize(multiProfile))
            Dim lightData = Directory.CreateDirectory(Path.Combine(root, "light-inputs")).FullName
            Fixture(lightData, light:=True)
            Dim lightPatch = WriteAppearancePatch(lightData, order, "Fixture.esm", &H800UI, Path.Combine(root, "light-appearance"),
                AgeEvidence.Adult, "generated light adult fixture", presetPath, "Fixture.esm", &H801UI)
            Dim lightReader As New PluginReader() : lightReader.Load(lightPatch)
            Dim lightPm As New PluginManager() : lightPm.LoadAllPlugins(lightData, order)
            Check(lightPm.ResolveFormID(lightReader.Records.Values.Single().Header.FormID, lightReader) = &HFE000800UI, "ESL target identity survives appearance export")
            Reject(Sub() WriteRecordOnlyPatch(lightData, order, "Fixture.esm", &H100800UI, Path.Combine(root, "light-alias"), AgeEvidence.Adult, "fixture"), "oversized ESL object ID cannot alias another NPC")
            Reject(Sub() WriteAppearancePatch(lightData, order, "Fixture.esm", &H800UI, Path.Combine(root, "light-race-alias"), AgeEvidence.Adult,
                "fixture", presetPath, "Fixture.esm", &H100801UI), "oversized ESL race identity cannot alias approval")
            Dim verifiedShards = aggregate.Items.Select(Function(item) Directory.GetFiles(item.AttemptDirectory, "*.esp").Single()).ToArray()
            Dim copyJournal = CreateCopyJournal(multiProfile, root, verifiedShards, False)
            Dim copiedBundle = AggregateNpcPatch.Export(multiProfile, copyJournal)
            Dim copiedResource = Path.Combine(copiedBundle, "Data", "Textures", "copy-fixture.bin")
            Check(File.ReadAllText(copiedResource) = "identical fixture bytes", "aggregate copies checkpointed resource bytes without reading original assets")
            Check(Directory.GetFiles(Path.Combine(copiedBundle, "Data"), "*", SearchOption.AllDirectories).Length = 1, "identical shared resources deduplicated")
            Dim conflictingJournal = CreateCopyJournal(multiProfile, root, verifiedShards, True)
            Reject(Sub() AggregateNpcPatch.Export(multiProfile, conflictingJournal), "conflicting staged resource paths block aggregate")
            Check(Not conflictingJournal.ContainsVerifiedCheckpoint(AggregateNpcPatch.CheckpointKey), "resource conflict never checkpoints partial bundle")
            File.AppendAllText(copiedResource, "tampered")
            Reject(Sub() copyJournal.ContainsVerifiedCheckpoint(AggregateNpcPatch.CheckpointKey), "copied resource tampering invalidates bundle checkpoint")
            ' Real loader/winning-override inventory, including an inactive file not named in load order.
            Dim campaignData = Directory.CreateDirectory(Path.Combine(root, "campaign-inputs")).FullName
            Fixture(campaignData, twoNpcs:=True, genericHuman:=True)
            Dim baseReader As New PluginReader() : baseReader.Load(Path.Combine(campaignData, "Fixture.esm"))
            Dim baseNpc = baseReader.Records.Values.Single(Function(r) r.Header.Signature = "NPC_" AndAlso r.Header.FormID = &H1000800UI)
            Dim overrideBody = baseNpc.Subrecords.SelectMany(Function(part) Sr(part.Signature, part.Data)).ToArray()
            Dim lateHeader = GenericPluginWriter.BuildTes4Header(Config_App.Game_Enum.Fallout4, False, False, New List(Of String) From {"Fixture.esm"}, 4, &H801UI, "fixture override", False)
            Dim lateRecord = GenericPluginWriter.WrapRecord("NPC_", overrideBody, &H20UI, &H800UI, 0UI, 0US, Config_App.Game_Enum.Fallout4)
            Dim unrelated = GenericPluginWriter.WrapRecord("ACTI", Sr("EDID", Text("UnrelatedActivator")), 0UI, &H1000800UI, 0UI, 0US, Config_App.Game_Enum.Fallout4)
            File.WriteAllBytes(Path.Combine(campaignData, "Late.esp"), lateHeader.Concat(GenericPluginWriter.BuildGrup("NPC_", New List(Of Byte()) From {lateRecord})).Concat(GenericPluginWriter.BuildGrup("ACTI", New List(Of Byte()) From {unrelated})).ToArray())
            Dim campaign As New CampaignInput With {.DataRoot = campaignData, .LoadOrder = order.Concat({"Late.esp"}).ToList(), .ProtectedRoots = New List(Of String) From {campaignData}}
            Dim discovered = GlobalNpcDiscovery.Scan(campaign)
            Check(discovered.LoadedRecordCount = 5, "original signature filter omits unrelated groups from discovery memory")
            Check(discovered.Decisions.Count = 2, "global discovery emits one winning row per NPC")
            Dim lateWinner = discovered.Decisions.Single(Function(d) d.Npc.ObjectId = &H800UI)
            Check(lateWinner.Npc.Origin = "Fixture.esm" AndAlso lateWinner.Npc.Winner = "Late.esp", "origin and winning override distinguished")
            Check(lateWinner.Npc.OverrideChain.SequenceEqual({"Fixture.esm", "Late.esp"}), "override chain follows actual load order")
            Check(lateWinner.Npc.Deleted AndAlso lateWinner.Reason = "deleted-winning-record", "deleted winning NPC never scheduled")
            Check(discovered.Pages.Count = 0 AndAlso discovered.Status = "EXPERIMENTAL-PLAN-NOT-CONVERTED", "unknown adulthood never scheduled")
            Check(GlobalNpcDiscovery.Scan(campaign).PlanSha256 = discovered.PlanSha256, "unchanged real input produces deterministic discovery plan")
            Dim planPath = Path.Combine(root, "global-inventory.json")
            GlobalNpcDiscovery.WriteReport(campaign, discovered, planPath)
            Check(File.Exists(planPath), "bounded global report exported outside originals")
            Reject(Sub() GlobalNpcDiscovery.WriteReport(campaign, discovered, planPath), "existing global report never overwritten")
            Reject(Sub() GlobalNpcDiscovery.WriteReport(campaign, discovered, Path.Combine(campaignData, "report.json")), "global report cannot write to input root")
            campaign.Policy.MaxReportBytes = 1
            Reject(Sub() GlobalNpcDiscovery.WriteReport(campaign, discovered, Path.Combine(root, "over-budget.json")), "report byte budget blocks output")
            Check(Not File.Exists(Path.Combine(root, "over-budget.json")), "budget rejection leaves no report")
            campaign.Policy.MaxReportBytes = 16L * 1024 * 1024
            campaign.Policy.MaxPluginBytes = 1
            Reject(Sub() GlobalNpcDiscovery.Scan(campaign), "plugin input budget checked before parsing")
            campaign.Policy.MaxPluginBytes = 4L * 1024 * 1024 * 1024
            campaign.Policy.BatchSize = 11
            Reject(Sub() GlobalNpcDiscovery.PreparePage(campaign, discovered, 0, Path.Combine(root, "stale-page.json")), "changed policy cannot prepare stale campaign page")
            campaign.Policy.BatchSize = 10
            Dim lightCampaign As New CampaignInput With {.DataRoot = lightData, .LoadOrder = order.ToList(), .ProtectedRoots = New List(Of String) From {lightData}}
            Check(GlobalNpcDiscovery.Scan(lightCampaign).Decisions.Single().Npc.Key = "FIXTURE.ESM|000800", "global ESL identity remains originating plugin plus local ID")
            campaign.Policy.HumanRaces.Add("FIXTURE.ESM|000801")
            campaign.Policy.AdultEvidence("FIXTURE.ESM|000804") = "generated adult generic evidence"
            campaign.Policy.FemalePresets.Add(presetPath)
            Dim baseCampaign As New CampaignInput With {.DataRoot = campaignData, .LoadOrder = order.ToList(), .ProtectedRoots = campaign.ProtectedRoots.ToList(), .Policy = campaign.Policy}
            Dim commonRacePlan = GlobalNpcDiscovery.Scan(baseCampaign)
            Check(commonRacePlan.Decisions.Count = 2 AndAlso commonRacePlan.Decisions.All(Function(d) d.Npc.RaceFlagsKnown), "shared race metadata resolves consistently for multiple winning NPCs")
            Dim approvedPlan = GlobalNpcDiscovery.Scan(campaign)
            Check(approvedPlan.Pages.Count = 1 AndAlso approvedPlan.Pages(0).Single() = "FIXTURE.ESM|000804", "discovery automatically plans eligible adult generic while excluding deleted NPC")
            Dim preparedPath = Path.Combine(root, "prepared-page.json")
            GlobalNpcDiscovery.PreparePage(campaign, approvedPlan, 0, preparedPath)
            Dim prepared = JsonSerializer.Deserialize(Of NpcBatchProfile)(File.ReadAllBytes(preparedPath))
            Check(prepared.RequireFaceGen AndAlso prepared.Targets.Single().ObjectId = &H804UI, "automatic page reuses production batch contract and requires FaceGen")
            Dim lateBytes = File.ReadAllBytes(Path.Combine(campaignData, "Late.esp"))
            Dim localizedBytes = CType(lateBytes.Clone(), Byte())
            localizedBytes(8) = localizedBytes(8) Or CByte(&H80)
            File.WriteAllBytes(Path.Combine(campaignData, "Late.esp"), localizedBytes)
            Dim localizedPlan = GlobalNpcDiscovery.Scan(campaign)
            Check(localizedPlan.ExecutionBlockers.Contains("localized-load-order-export-not-integrated"), "localized masters explicitly block current export admission")
            Reject(Sub() GlobalNpcDiscovery.PreparePage(campaign, localizedPlan, 0, Path.Combine(root, "localized-page.json")), "localized input cannot prepare unusable production page")
            File.WriteAllBytes(Path.Combine(campaignData, "Late.esp"), lateBytes)
            Dim raceHeader = GenericPluginWriter.BuildTes4Header(Config_App.Game_Enum.Fallout4, False, False, New List(Of String) From {"Fixture.esm"}, 4, &H800UI, "race admission fixtures", False)
            Dim deadRace = GenericPluginWriter.WrapRecord("RACE", Sr("EDID", Text("DeletedRace")), &H20UI, &H801UI, 0UI, 0US, Config_App.Game_Enum.Fallout4)
            File.WriteAllBytes(Path.Combine(campaignData, "Late.esp"), raceHeader.Concat(GenericPluginWriter.BuildGrup("NPC_", New List(Of Byte()) From {lateRecord})).Concat(GenericPluginWriter.BuildGrup("RACE", New List(Of Byte()) From {deadRace})).ToArray())
            Dim deadRacePlan = GlobalNpcDiscovery.Scan(campaign)
            Check(deadRacePlan.Pages.Count = 0 AndAlso deadRacePlan.Decisions.Single(Function(d) d.Npc.ObjectId = &H804UI).Npc.ErrorDetail.Contains("deleted race"), "deleted winning race blocks global admission")
            Reject(Sub() WriteAppearancePatch(campaignData, campaign.LoadOrder, "Fixture.esm", &H804UI, Path.Combine(root, "deleted-race-output"), AgeEvidence.Adult,
                "explicit adult evidence", presetPath, "Fixture.esm", &H801UI), "direct production appearance export rejects deleted race")
            Dim childView = DirectCast(FO4_Base_Library.Canon.CanonRecords.RaceNuevo(FO4_Base_Library.Canon.WbGame.Fallout4), FO4_Base_Library.Canon.RaceFO4)
            childView.EditorID = "ChildRaceAdmissionFixture"
            childView.DataFlagsChild = True
            Dim childBody = FO4_Base_Library.Canon.WbWriter.EmitBody(childView.Node, childView.Context)
            Dim childRaceRecord = GenericPluginWriter.WrapRecord("RACE", childBody, 0UI, &H801UI, 0UI, 0US, Config_App.Game_Enum.Fallout4)
            File.WriteAllBytes(Path.Combine(campaignData, "Late.esp"), raceHeader.Concat(GenericPluginWriter.BuildGrup("NPC_", New List(Of Byte()) From {lateRecord})).Concat(GenericPluginWriter.BuildGrup("RACE", New List(Of Byte()) From {childRaceRecord})).ToArray())
            Dim childPlan = GlobalNpcDiscovery.Scan(campaign)
            Check(childPlan.Pages.Count = 0 AndAlso childPlan.Decisions.Single(Function(d) d.Npc.ObjectId = &H804UI).Category = "child", "actual child-race flag overrides contradictory adult policy")
            Reject(Sub() WriteAppearancePatch(campaignData, campaign.LoadOrder, "Fixture.esm", &H804UI, Path.Combine(root, "child-race-output"), AgeEvidence.Adult,
                "contradictory policy cannot authorize a child", presetPath, "Fixture.esm", &H801UI), "direct production appearance export cannot bypass child gate")
            Check(Not Directory.Exists(Path.Combine(root, "child-race-output")), "child rejection writes no patch directory")
            File.WriteAllBytes(Path.Combine(campaignData, "Late.esp"), lateBytes)
            Dim unsupportedPreset = Path.Combine(root, "runtime-body-preset.json")
            File.WriteAllText(unsupportedPreset, "{""Gender"":1,""Morphs"":{""Presets"":{""12345678"":0.25}},""BodyMorphs"":{""FixtureSlider"":0.5}}")
            campaign.Policy.FemalePresets.Clear()
            campaign.Policy.FemalePresets.Add(unsupportedPreset)
            Reject(Sub() GlobalNpcDiscovery.Scan(campaign), "global planning reuses production unsupported-preset guard")
            campaign.Policy.FemalePresets.Clear()
            campaign.Policy.FemalePresets.Add(presetPath)
            campaign.Policy.MaxRecords = 1
            Reject(Sub() GlobalNpcDiscovery.Scan(campaign), "winning NPC record budget blocks before inventory allocation")
            campaign.Policy.MaxRecords = 100000
            File.WriteAllText(Path.Combine(root, "campaign-profile.json"), JsonSerializer.Serialize(campaign))
            Dim preparationStage = Path.Combine(root, "controlled-preparation")
            Reject(Sub() CampaignPreparation.SetPaused(campaign, campaignData, True), "pause cannot open preparation state inside original data root")
            Reject(Sub() CampaignPreparation.PrepareNext(campaign, approvedPlan, campaignData), "resume cannot open preparation state inside original data root")
            CampaignPreparation.Start(campaign, approvedPlan, preparationStage, 32768)
            CampaignPreparation.SetPaused(campaign, preparationStage, True)
            Check(CampaignPreparation.PrepareNext(campaign, approvedPlan, preparationStage).Status = "PAUSED-PREPARATION-ONLY", "campaign preparation pause survives process-style recovery")
            CampaignPreparation.SetPaused(campaign, preparationStage, False)
            Dim firstPage = CampaignPreparation.PrepareNext(campaign, approvedPlan, preparationStage)
            Check(firstPage.PageIndex = 0 AndAlso File.Exists(firstPage.OutputPath), "sequential preparation persists quota-reserved production profile")
            Check(CampaignPreparation.PrepareNext(campaign, approvedPlan, preparationStage).Status = "ALL-PAGES-PREPARED-NOT-CONVERTED", "resumed preparation skips verified completed pages")
            File.WriteAllText(firstPage.OutputPath, "interrupted or corrupt staged page")
            Dim retryPage = CampaignPreparation.PrepareNext(campaign, approvedPlan, preparationStage)
            Check(retryPage.PageIndex = 0 AndAlso retryPage.OutputPath <> firstPage.OutputPath, "partial staged page retries in a new charged attempt without overwriting")
            campaign.Policy.BatchSize = 2
            Reject(Sub() CampaignPreparation.PrepareNext(campaign, approvedPlan, preparationStage), "stale campaign policy blocks resumed page preparation")
            campaign.Policy.BatchSize = 10
            LocalizationChecks(root)
            TraitsChecks(root)
            NarrativeChecks(root)
            UnmodeledRecordChecks(root)
            AssetValidationChecks(root)
            ControlledPipelineChecks(root)
            File.WriteAllText(Path.Combine(root, "gate.json"), JsonSerializer.Serialize(New With {.checksPassed = checks, .evidence = "binary-fixture-production-integration", .inGameValidated = False, .realFaceGenValidated = False}))
            Console.WriteLine($"{checks} checks passed; binary fixture only, NOT an in-game conversion.")
            Return 0
        Catch ex As Exception
            Console.Error.WriteLine("LAST-CHECK: " & lastCheck)
            Console.Error.WriteLine(ex.ToString()) : Return 1
        End Try
    End Function
End Module
