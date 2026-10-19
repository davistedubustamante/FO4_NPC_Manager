Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Linq

Namespace ExperimentalTransformation
    Public Enum AgeEvidence
        Unknown
        Adult
        Child
    End Enum

    ' Synthetic DTO only. Never pass this object directly to an ESP writer.
    Public Class SyntheticNpc
        Public Property Key As String = ""
        Public Property RaceKey As String = ""
        Public Property Age As AgeEvidence
        Public Property TraitsResolved As Boolean
        Public Property Flags As UInteger
        Public Property Appearance As String = ""
        Public Property Voice As String = ""
        Public Property Equipment As New List(Of String)
        ' Opaque stand-ins for quests, scripts, packages, factions and other untouched data.
        Public Property Preserved As New Dictionary(Of String, String)(StringComparer.Ordinal)
    End Class

    Public Class SyntheticProfile
        Public Property Id As String = ""
        Public Property RaceKey As String = ""
        Public Property Age As AgeEvidence
        Public Property Female As Boolean
        Public Property Appearance As String = ""
        Public Property Body As String = ""
    End Class

    Public Class SyntheticChange
        Public Property Key As String = ""
        Public Property Reason As String = ""
        Public Property Transformed As SyntheticNpc
        Public Property Body As String = ""
        Public ReadOnly Property Applied As Boolean
            Get
                Return Transformed IsNot Nothing
            End Get
        End Property
    End Class

    Public Class SyntheticBatch
        Public ReadOnly Property Status As String
            Get
                Return "EXPERIMENTAL-SYNTHETIC-NOT-A-GAME-PATCH"
            End Get
        End Property
        Public ReadOnly Property CanWriteGamePatch As Boolean
            Get
                Return False
            End Get
        End Property
        Public ReadOnly Property PendingValidation As String()
            Get
                Return {"real-plugin-round-trip", "adult-classification", "template-materialization",
                        "facegen", "body-assets", "female-equipment", "voice-and-dialogue", "quests", "collection-compatibility"}
            End Get
        End Property
        Public Property Changes As New List(Of SyntheticChange)
    End Class

    Public Module AdultTransformation
        Public Const FemaleFlag As UInteger = 1UI
        Public Const MaximumBatch As Integer = 10

        ' Materialize input before producing any output. Ambiguous identity rejects the whole batch.
        Public Function TransformSynthetic(npcs As IEnumerable(Of SyntheticNpc), profile As SyntheticProfile) As SyntheticBatch
            If npcs Is Nothing Then Throw New ArgumentNullException(NameOf(npcs))
            If profile Is Nothing Then Throw New ArgumentNullException(NameOf(profile))
            If profile.Age <> AgeEvidence.Adult OrElse Not profile.Female OrElse
               String.IsNullOrWhiteSpace(profile.Id) OrElse String.IsNullOrWhiteSpace(profile.RaceKey) OrElse
               String.IsNullOrWhiteSpace(profile.Appearance) OrElse String.IsNullOrWhiteSpace(profile.Body) Then
                Throw New ArgumentException("An explicit adult female profile with race, appearance and body is required.")
            End If
            Dim input = npcs.Take(MaximumBatch + 1).ToList()
            If input.Count = 0 OrElse input.Count > MaximumBatch Then Throw New ArgumentException("Synthetic batch must contain 1 to 10 NPCs.")
            Dim keys As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each npc In input
                If npc Is Nothing OrElse String.IsNullOrWhiteSpace(npc.Key) OrElse Not keys.Add(npc.Key) Then
                    Throw New ArgumentException("NPC identities must be nonempty and unique.")
                End If
                If npc.Equipment Is Nothing OrElse npc.Preserved Is Nothing Then Throw New ArgumentException("Incomplete NPC snapshot.")
            Next
            Dim result As New SyntheticBatch
            For Each npc In input
                Dim change As New SyntheticChange With {.Key = npc.Key}
                If npc.Age <> AgeEvidence.Adult Then
                    change.Reason = "adult-evidence-required"
                ElseIf Not npc.TraitsResolved Then
                    change.Reason = "unresolved-template-traits"
                ElseIf Not String.Equals(npc.RaceKey, profile.RaceKey, StringComparison.OrdinalIgnoreCase) Then
                    change.Reason = "race-profile-mismatch"
                ElseIf (npc.Flags And FemaleFlag) <> 0UI Then
                    change.Reason = "already-female"
                Else
                    change.Transformed = New SyntheticNpc With {
                        .Key = npc.Key, .RaceKey = npc.RaceKey, .Age = npc.Age,
                        .TraitsResolved = npc.TraitsResolved, .Flags = npc.Flags Or FemaleFlag,
                        .Appearance = profile.Appearance, .Voice = npc.Voice,
                        .Equipment = New List(Of String)(npc.Equipment),
                        .Preserved = New Dictionary(Of String, String)(npc.Preserved, StringComparer.Ordinal)}
                    change.Body = profile.Body
                    change.Reason = "synthetic-copy-transformed;voice-and-equipment-preserved-unvalidated;facegen-not-generated"
                End If
                result.Changes.Add(change)
            Next
            Return result
        End Function
    End Module
End Namespace
