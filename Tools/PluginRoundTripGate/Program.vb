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
        Dim header = GenericPluginWriter.BuildTes4Header(game, True, False, New List(Of String) From {"Fallout4.esm"}, 4, &H804UI, "fixture", False)
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
            Check(after.Masters.SequenceEqual(order, StringComparer.OrdinalIgnoreCase), "masters preserve source identity")
            Dim written = after.Records.Values.Single()
            Check(written.Header.FormID = &H1000800UI, "file-local FormID preserved")
            Check((BitConverter.ToUInt32(written.GetSubrecord("ACBS").Value.Data, 0) And 1UI) = 1UI, "female flag serialized")
            Check(BitConverter.ToUInt32(written.GetSubrecord("ACBS").Value.Data, 0) = &H80000003UI, "unrelated flags preserved")
            Check(written.GetSubrecord("VMAD").Value.Data.SequenceEqual(New Byte() {6, 0, 2, 0, 0, 0}), "script bytes preserved")
            Check(BitConverter.ToUInt32(written.GetSubrecord("VTCK").Value.Data, 0) = &H1000802UI, "voice reference preserved")
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
            File.WriteAllText(Path.Combine(root, "gate.json"), JsonSerializer.Serialize(New With {.checksPassed = checks, .evidence = "binary-fixture-round-trip", .inGameValidated = False}))
            Console.WriteLine($"{checks} checks passed; binary fixture only, NOT an in-game conversion.")
            Return 0
        Catch ex As Exception
            Console.Error.WriteLine(ex.ToString()) : Return 1
        End Try
    End Function
End Module
