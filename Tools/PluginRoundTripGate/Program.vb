Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Security.Cryptography
Imports System.Text.Json
Imports FO4_Base_Library
Imports FO4_NPC_Manager.ExperimentalTransformation

Module Program
    Private checks As Integer
    Private Sub Check(ok As Boolean, name As String)
        If Not ok Then Throw New InvalidDataException("FAIL: " & name)
        checks += 1
        Console.WriteLine("PASS: " & name)
    End Sub
    Private Sub Reject(action As Action, name As String)
        Dim rejected = False
        Try
            action()
        Catch ex As ArgumentException
            rejected = True
        Catch ex As InvalidOperationException
            rejected = True
        Catch ex As IOException
            rejected = True
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
    Private Sub Fixture(data As String, Optional templates As UShort = 0US)
        Dim game = Config_App.Game_Enum.Fallout4
        File.WriteAllBytes(Path.Combine(data, "Fallout4.esm"), GenericPluginWriter.BuildTes4Header(game, True, False, New List(Of String), 0, &H800UI, "fixture", False))
        Dim acbs(19) As Byte
        Array.Copy(BitConverter.GetBytes(&H80000002UI), acbs, 4)
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
        Dim race = GenericPluginWriter.WrapRecord("RACE", Sr("EDID", Text("AdultRaceFixture")), 0UI, &H1000801UI, 0UI, 0US, game)
        Dim voice = GenericPluginWriter.WrapRecord("VTYP", Sr("EDID", Text("QuestVoiceFixture")), 0UI, &H1000802UI, 0UI, 0US, game)
        Dim npcClass = GenericPluginWriter.WrapRecord("CLAS", Sr("EDID", Text("ClassFixture")), 0UI, &H1000803UI, 0UI, 0US, game)
        Dim header = GenericPluginWriter.BuildTes4Header(game, True, False, New List(Of String) From {"Fallout4.esm"}, 8, &H804UI, "fixture", False)
        File.WriteAllBytes(Path.Combine(data, "Fixture.esm"), header.Concat(GenericPluginWriter.BuildGrup("CLAS", New List(Of Byte()) From {npcClass})) _
            .Concat(GenericPluginWriter.BuildGrup("RACE", New List(Of Byte()) From {race})) _
            .Concat(GenericPluginWriter.BuildGrup("NPC_", New List(Of Byte()) From {npc})) _
            .Concat(GenericPluginWriter.BuildGrup("VTYP", New List(Of Byte()) From {voice})).ToArray())
    End Sub
    Function Main(args As String()) As Integer
        Try
            If args.Length <> 1 OrElse Directory.Exists(args(0)) Then Throw New ArgumentException("Expected new staging root.")
            Dim root = Path.GetFullPath(args(0))
            Dim data = Directory.CreateDirectory(Path.Combine(root, "inputs")).FullName
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
            Check(batch.Items(0).Status = "BLOCKED-PARTIAL-NOT-INSTALLED", "batch records failure without marking success")
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
            profile.RequireFaceGen = False
            File.WriteAllText(Path.Combine(root, "cli-profile.json"), JsonSerializer.Serialize(profile))
            File.WriteAllText(Path.Combine(root, "gate.json"), JsonSerializer.Serialize(New With {.checksPassed = checks, .evidence = "binary-fixture-round-trip", .inGameValidated = False}))
            Console.WriteLine($"{checks} checks passed; binary fixture only, NOT an in-game conversion.")
            Return 0
        Catch ex As Exception
            Console.Error.WriteLine(ex.ToString()) : Return 1
        End Try
    End Function
End Module
