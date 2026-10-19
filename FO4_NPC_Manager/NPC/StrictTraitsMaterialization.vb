Option Strict On
Imports System.IO
Imports System.Linq
Imports FO4_Base_Library

Namespace ExperimentalTransformation
    Public Module StrictTraitsMaterialization
        ' Reuse the editor's chain law and frozen complete Traits bucket. No alternate template resolver.
        ' The editor tolerates dead terminal pointers; autonomous export must fail closed instead.
        Public Function Materialize(npc As NPC_Data, pm As PluginManager) As NPC_Data
            If npc Is Nothing OrElse npc.Record Is Nothing Then Throw New InvalidDataException("Missing template NPC.")
            If npc.Record.ConfigurationTemplateFlags = 0US Then Return npc
            If npc.Record.ConfigurationTemplateFlags <> NpcTemplateHelpers.MascaraDePlantilla(NPC_TemplateCategory.Traits) Then Throw New InvalidOperationException("Only Traits-only inheritance is verified for autonomous materialization; other categories remain blocked.")
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
                    If NpcTemplateHelpers.HasTemplateFlag(candidate.Record.ConfigurationTemplateFlags, NPC_TemplateCategory.Traits) Then
                        Dim pointer = NpcTemplateHelpers.ResolveTemplateSourceFormID(candidate, NPC_TemplateCategory.Traits)
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
            Dim resolved = NpcTemplateMaterializer.ProbeCategoryOwn(npc, NPC_TemplateCategory.Traits, parse, Nothing, NpcTemplateHelpers.FirmaDeRecord(pm))
            If resolved.Outcome <> NpcTemplateMaterializer.MaterializeOutcome.Materialized OrElse resolved.Source Is Nothing Then Throw New InvalidDataException("Traits chain is cyclic, ambiguous or incomplete: " & resolved.LogReason)
            Dim frozen = NpcTemplateMaterializer.CongelarResolucion(resolved)
            If Not frozen.Source.Record.RacePresente OrElse Not frozen.Source.Record.VoicePresente Then Throw New InvalidDataException("Terminal Traits source must define race and voice.")
            Dim shadow = npc.Copia()
            If NpcTemplateMaterializer.MakeCategoryOwn(shadow, NPC_TemplateCategory.Traits, frozen) <> NpcTemplateMaterializer.MaterializeOutcome.Materialized Then Throw New InvalidDataException("Traits materializer did not complete.")
            If shadow.Record.ConfigurationTemplateFlags <> 0US Then Throw New InvalidDataException("Unexpected inherited category after Traits materialization.")
            Return shadow
        End Function
    End Module
End Namespace
