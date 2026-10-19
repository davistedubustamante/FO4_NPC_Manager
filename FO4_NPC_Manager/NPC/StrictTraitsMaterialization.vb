Option Strict On
Imports System.IO
Imports System.Linq
Imports FO4_Base_Library

Namespace ExperimentalTransformation
    Public Module StrictTraitsMaterialization
        ' Reuse original category buckets and frozen chain law. No alternate template resolver.
        ' The editor tolerates dead terminal pointers; autonomous export must fail closed instead.
        Public Function CategoryNames(flags As UShort) As String()
            Return [Enum].GetValues(Of NPC_TemplateCategory)().Where(Function(c) NpcTemplateHelpers.HasTemplateFlag(flags, c)).Select(Function(c) c.ToString()).ToArray()
        End Function
        Public Function Materialize(npc As NPC_Data, pm As PluginManager) As NPC_Data
            If npc Is Nothing OrElse npc.Record Is Nothing Then Throw New InvalidDataException("Missing template NPC.")
            If npc.Record.ConfigurationTemplateFlags = 0US Then Return npc
            Dim admitted = {NPC_TemplateCategory.Traits, NPC_TemplateCategory.Factions, NPC_TemplateCategory.Inventory, NPC_TemplateCategory.AIData}
            Dim allowed As UShort = 0US
            For Each category In admitted
                allowed = allowed Or NpcTemplateHelpers.MascaraDePlantilla(category)
            Next
            If (npc.Record.ConfigurationTemplateFlags And Not allowed) <> 0US Then Throw New InvalidOperationException("Unverified inherited category; only Traits, Factions, Inventory and AIData are admitted.")
            ' Resolve every bucket against the unchanged source before applying any mutation.
            Dim shadow = npc.Copia()
            For Each category In admitted
                If Not NpcTemplateHelpers.HasTemplateFlag(npc.Record.ConfigurationTemplateFlags, category) Then Continue For
                MaterializeCategory(npc, shadow, pm, category)
            Next
            If shadow.Record.ConfigurationTemplateFlags <> 0US Then Throw New InvalidDataException("Unexpected inherited category after materialization.")
            Return shadow
        End Function
        Private Sub MaterializeCategory(npc As NPC_Data, shadow As NPC_Data, pm As PluginManager, category As NPC_TemplateCategory)
            Dim count As Integer = 0
            Dim validate As Action(Of NPC_Data) =
                Sub(candidate)
                    count += 1
                    If count > 4096 Then Throw New InvalidDataException("Traits resolution record budget exceeded.")
                    If candidate Is Nothing OrElse candidate.Record Is Nothing OrElse Not candidate.Record.ConfigurationFlagsPresente Then Throw New InvalidDataException("Incomplete Traits source.")
                    Dim raw = pm.GetRecord(candidate.FormID)
                    If raw Is Nothing OrElse raw.Header.IsDeleted OrElse raw.Header.Signature <> "NPC_" Then Throw New InvalidDataException("Missing or deleted Traits source.")
                    If raw.Subrecords.Where(Function(part) part.Signature = "TPLT").Count() > 1 OrElse raw.Subrecords.Where(Function(part) part.Signature = "TPTA").Count() > 1 Then Throw New InvalidDataException("Ambiguous duplicate template pointer blocks.")
                    IsolatedNpcPatch.ValidateParsedNpc(candidate)
                    If NpcTemplateHelpers.HasTemplateFlag(candidate.Record.ConfigurationTemplateFlags, category) Then
                        Dim pointer = NpcTemplateHelpers.ResolveTemplateSourceFormID(candidate, category)
                        Dim source = pm.GetRecord(pointer)
                        If pointer = 0UI OrElse source Is Nothing OrElse source.Header.IsDeleted Then Throw New InvalidDataException("Incomplete Traits chain; dead pointers cannot fall back to an intermediate NPC.")
                        If source.Header.Signature <> "NPC_" Then Throw New InvalidDataException("Non-NPC Traits source requires a separately verified deterministic route; no leveled-list pick.")
                    End If
                End Sub
            validate(npc)
            Dim parse As Func(Of UInteger, NPC_Data) =
                Function(fid)
                    Dim raw = pm.GetRecord(fid)
                    If raw Is Nothing OrElse raw.Header.IsDeleted OrElse raw.Header.Signature <> "NPC_" Then Throw New InvalidDataException("Unreadable Traits source.")
                    Dim candidate = RecordParsers.ParseNPC(raw, pm)
                    validate(candidate)
                    Return candidate
                End Function
            Dim resolved = NpcTemplateMaterializer.ProbeCategoryOwn(npc, category, parse, Nothing, NpcTemplateHelpers.FirmaDeRecord(pm))
            If resolved.Outcome <> NpcTemplateMaterializer.MaterializeOutcome.Materialized OrElse resolved.Source Is Nothing Then Throw New InvalidDataException(category.ToString() & " chain is cyclic, ambiguous or incomplete: " & resolved.LogReason)
            Dim frozen = NpcTemplateMaterializer.CongelarResolucion(resolved)
            If category = NPC_TemplateCategory.Traits AndAlso (Not frozen.Source.Record.RacePresente OrElse Not frozen.Source.Record.VoicePresente) Then Throw New InvalidDataException("Terminal Traits source must define race and voice.")
            If NpcTemplateMaterializer.MakeCategoryOwn(shadow, category, frozen) <> NpcTemplateMaterializer.MaterializeOutcome.Materialized Then Throw New InvalidDataException("Traits materializer did not complete.")
        End Sub
    End Module
End Namespace
