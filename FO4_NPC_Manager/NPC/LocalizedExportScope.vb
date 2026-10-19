Option Strict On
Imports System.IO
Imports System.Linq
Imports System.Collections.Concurrent
Imports System.Security.Cryptography
Imports System.Text
Imports System.Threading
Imports FO4_Base_Library

Namespace ExperimentalTransformation
    ' Dedicated headless operations only. The original resource dictionary is process-global.
    ' Restrict visible resources rather than changing the pinned resolver's fallback policy.
    Public Class LocalizedStringAudit
        Public Property SourcePlugin As String = ""
        Public Property Field As String = ""
        Public Property Id As UInteger
        Public Property Kind As String = ""
        Public Property TextSha256 As String = ""
    End Class
    Public NotInheritable Class LocalizedExportScope
        Implements IDisposable
        Private Shared ReadOnly contextGate As New Object
        Private priorDictionary As ConcurrentDictionary(Of String, FilesDictionary_class.File_Location)
        Private outputEncoding As IDisposable
        Private priorRoot As String
        Private ReadOnly tableHandles As New List(Of FileStream)
        Private ReadOnly expected As New List(Of String)
        Private disposed As Boolean

        Private Sub New()
            Monitor.Enter(contextGate)
        End Sub
        Public Shared Function Open(profile As LocalizationProfile, plugins As IEnumerable(Of String)) As LocalizedExportScope
            Dim scope As New LocalizedExportScope()
            Try
                scope.priorDictionary = FilesDictionary_class.Dictionary
                scope.priorRoot = FilesDictionary_class.FO4Path
                ' Even a nonlocalized operation must not observe stale resources from another profile.
                Dim selected As New ConcurrentDictionary(Of String, FilesDictionary_class.File_Location)(StringComparer.OrdinalIgnoreCase)
                If profile IsNot Nothing Then
                    If PluginEncodingSettings.TryGetLocalizationPrimaryOverride() IsNot Nothing Then Throw New InvalidDataException("Unsealed global localization encoding override is unsupported.")
                    If PluginEncodingSettings.General.CodePage <> 1252 Then Throw New InvalidDataException("Unsealed general plugin encoding override is unsupported.")
                    scope.outputEncoding = PluginEncodingSettings.PushTranslatableOverride("utf-8")
                    If String.IsNullOrWhiteSpace(profile.Language) OrElse Not System.Text.RegularExpressions.Regex.IsMatch(profile.Language, "\A[a-z]{2,24}\z") Then Throw New ArgumentException("Explicit literal lower-case language token required.")
                    If profile.Tables Is Nothing OrElse profile.Tables.Count = 0 OrElse profile.Tables.Count > 4096 Then Throw New ArgumentException("Expected 1..4096 explicitly selected loose language tables.")
                    Dim loaded As New HashSet(Of String)(plugins, StringComparer.OrdinalIgnoreCase)
                    Dim total As Long = 0
                    For Each table In profile.Tables
                        If table Is Nothing OrElse Not loaded.Contains(table.Plugin) OrElse Path.GetFileName(table.Plugin) <> table.Plugin Then Throw New ArgumentException("Language table plugin must belong to the explicit load order.")
                        If table.Kind <> "strings" AndAlso table.Kind <> "dlstrings" AndAlso table.Kind <> "ilstrings" Then Throw New ArgumentException("Unknown original string table kind.")
                        Dim filePath = Path.GetFullPath(table.FilePath)
                        Dim filename = Path.GetFileNameWithoutExtension(table.Plugin) & "_" & profile.Language & "." & table.Kind
                        If Not Path.GetFileName(filePath).Equals(filename, StringComparison.OrdinalIgnoreCase) Then Throw New ArgumentException("Table basename must match plugin, selected language and kind.")
                        CheckPath(filePath)
                        If File.Exists(Path.ChangeExtension(filePath, ".cpoverride")) Then Throw New InvalidDataException("Unsealed encoding sidecar is not supported by this export route.")
                        Dim handle As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)
                        scope.tableHandles.Add(handle)
                        If handle.Length > 32L * 1024 * 1024 Then Throw New IOException("Language table exceeds 32 MiB.")
                        total += handle.Length
                        If total > 128L * 1024 * 1024 Then Throw New IOException("Language tables exceed 128 MiB.")
                        Dim digest = Convert.ToHexString(SHA256.HashData(handle)) : handle.Position = 0
                        If Not digest.Equals(table.Sha256, StringComparison.OrdinalIgnoreCase) Then Throw New InvalidDataException("Language table SHA256 mismatch.")
                        scope.expected.Add(digest)
                        Dim aliasKey = "Strings\" & filename
                        Dim location As New FilesDictionary_class.File_Location With {.FullPath = filePath}
                        If Not selected.TryAdd(aliasKey, location) Then Throw New ArgumentException("Ambiguous duplicate language table.")
                        If Not selected.TryAdd(filePath, location) Then Throw New ArgumentException("Duplicate table source.")
                    Next
                End If
                FilesDictionary_class.ClearBytesCache()
                FilesDictionary_class.Dictionary = selected
                FilesDictionary_class.FO4Path = ""
                Return scope
            Catch
                scope.Dispose()
                Throw
            End Try
        End Function
        Public Shared Function Audit(npc As NPC_Data) As List(Of LocalizedStringAudit)
            Dim result As New List(Of LocalizedStringAudit)
            Dim view = TryCast(npc.Record, Canon.CanonView)
            If view Is Nothing OrElse view.Resolver Is Nothing OrElse view.Context Is Nothing Then Throw New InvalidDataException("Missing canonical localization context.")
            For Each node In view.Node.Walk()
                If Not TypeOf node.Def Is Canon.WbLStringDef OrElse Not Canon.WbLStringDef.EsIdDeTabla(node, view.Context) Then Continue For
                Dim value As String = ""
                If Not view.Resolver.TryText(node, value) Then Throw New InvalidDataException("Unresolved localized field: " & node.Path)
                Dim owner = node
                While owner IsNot Nothing AndAlso String.IsNullOrEmpty(owner.Signature)
                    owner = owner.Parent
                End While
                Dim signature = If(owner Is Nothing, "", owner.Signature)
                result.Add(New LocalizedStringAudit With {.SourcePlugin = view.Resolver.Record.SourcePluginName, .Field = node.Path,
                    .Id = CUInt(Convert.ToInt64(node.Value) And &HFFFFFFFFL),
                    .Kind = Canon.CanonResolver.TablaDeCampo(view.Resolver.Record.Header.Signature, signature).ToString(),
                    .TextSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))})
            Next
            Return result
        End Function
        Public Sub VerifyUnchanged()
            For i = 0 To tableHandles.Count - 1
                CheckPath(tableHandles(i).Name)
                If File.Exists(Path.ChangeExtension(tableHandles(i).Name, ".cpoverride")) Then Throw New IOException("Encoding sidecar appeared during export.")
                tableHandles(i).Position = 0
                If Convert.ToHexString(SHA256.HashData(tableHandles(i))) <> expected(i) Then Throw New IOException("Language table changed during export.")
                tableHandles(i).Position = 0
            Next
        End Sub
        Private Shared Sub CheckPath(filePath As String)
            Dim current = filePath
            While Not String.IsNullOrEmpty(current)
                If (Directory.Exists(current) OrElse File.Exists(current)) AndAlso (File.GetAttributes(current) And FileAttributes.ReparsePoint) <> 0 Then Throw New IOException("Reparse language table path is not allowed.")
                current = Path.GetDirectoryName(current)
            End While
        End Sub
        Public Sub Dispose() Implements IDisposable.Dispose
            If disposed Then Return
            disposed = True
            Try
                FilesDictionary_class.ClearBytesCache()
                FilesDictionary_class.Dictionary = priorDictionary
                FilesDictionary_class.FO4Path = priorRoot
                For Each handle In tableHandles
                    handle.Dispose()
                Next
                If outputEncoding IsNot Nothing Then outputEncoding.Dispose()
            Finally
                Monitor.Exit(contextGate)
            End Try
        End Sub
    End Class
End Namespace
