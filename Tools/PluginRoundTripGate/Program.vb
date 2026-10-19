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
        Catch ex As InvalidDataException
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
            File.WriteAllText(Path.Combine(root, "gate.json"), JsonSerializer.Serialize(New With {.checksPassed = checks, .evidence = "binary-fixture-production-integration", .inGameValidated = False, .realFaceGenValidated = False}))
            Console.WriteLine($"{checks} checks passed; binary fixture only, NOT an in-game conversion.")
            Return 0
        Catch ex As Exception
            Console.Error.WriteLine(ex.ToString()) : Return 1
        End Try
    End Function
End Module
