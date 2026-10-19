Option Strict On
Imports System.Collections.Generic
Namespace ExperimentalTransformation
    Public Class LocalizationProfile
        Public Property Language As String = ""
        Public Property Tables As New List(Of LocalizationTable)
    End Class
    Public Class LocalizationTable
        Public Property Plugin As String = ""
        ' Literal original resolver suffix: strings, dlstrings or ilstrings.
        Public Property Kind As String = "strings"
        Public Property FilePath As String = ""
        Public Property Sha256 As String = ""
    End Class
    Public Class NpcBatchProfile
        Public Property DataRoot As String = ""
        Public Property LoadOrder As New List(Of String)
        ' Include every original game/mod/save root. No installation is performed.
        Public Property ProtectedRoots As New List(Of String)
        Public Property Localization As LocalizationProfile
        Public Property RequireFaceGen As Boolean = True
        Public Property Targets As New List(Of NpcBatchTarget)
    End Class
    Public Class NpcBatchTarget
        Public Property SourcePlugin As String = ""
        Public Property ObjectId As UInteger
        Public Property Age As AgeEvidence = AgeEvidence.Unknown
        Public Property AdultEvidence As String = ""
        Public Property AppearancePreset As String = ""
        Public Property HumanRacePlugin As String = ""
        Public Property HumanRaceObjectId As UInteger
        Public ReadOnly Property Key As String
            Get
                Return SourcePlugin.ToUpperInvariant() & "|" & ObjectId.ToString("X6")
            End Get
        End Property
    End Class
End Namespace
