Option Strict On
Imports System.IO
Imports System.Linq
Imports System.Collections.Concurrent
Imports System.Security.Cryptography
Imports System.Text
Imports System.Threading
Imports FO4_Base_Library
Imports BSA_BA2_Library_DLL.BethesdaArchive.Core

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
        Private priorCache As String
        Private ReadOnly mounted As New List(Of String)
        Private ReadOnly archiveNames As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Private ReadOnly tableHandles As New List(Of FileStream)
        Private ReadOnly expected As New List(Of String)
        Private ReadOnly archiveEntries As New Dictionary(Of String, Dictionary(Of String, FilesDictionary_class.File_Location))(StringComparer.OrdinalIgnoreCase)
        Private disposed As Boolean

        Private Sub New()
            Monitor.Enter(contextGate)
        End Sub
        Public Shared Function Open(profile As LocalizationProfile, plugins As IEnumerable(Of String)) As LocalizedExportScope
            Dim scope As New LocalizedExportScope()
            Try
                scope.priorDictionary = FilesDictionary_class.Dictionary
                scope.priorRoot = FilesDictionary_class.FO4Path
                scope.priorCache = FilesDictionary_class.CacheDirectory
                If profile IsNot Nothing AndAlso profile.Tables IsNot Nothing AndAlso profile.Tables.Any(Function(t) t IsNot Nothing AndAlso Not String.IsNullOrEmpty(t.ResourcePath)) AndAlso scope.priorDictionary.Values.Any(Function(e) Not e.IsLosseFile) Then Throw New InvalidOperationException("BA2 export requires a dedicated headless resource context.")
                FilesDictionary_class.CacheDirectory = ""
                FilesDictionary_class.Dictionary = New ConcurrentDictionary(Of String, FilesDictionary_class.File_Location)(StringComparer.OrdinalIgnoreCase)
                FilesDictionary_class.FO4Path = ""
                ' Even a nonlocalized operation must not observe stale resources from another profile.
                Dim selected As New ConcurrentDictionary(Of String, FilesDictionary_class.File_Location)(StringComparer.OrdinalIgnoreCase)
                If profile IsNot Nothing Then
                    If PluginEncodingSettings.TryGetLocalizationPrimaryOverride() IsNot Nothing Then Throw New InvalidDataException("Unsealed global localization encoding override is unsupported.")
                    If PluginEncodingSettings.General.CodePage <> 1252 Then Throw New InvalidDataException("Unsealed general plugin encoding override is unsupported.")
                    scope.outputEncoding = PluginEncodingSettings.PushTranslatableOverride("utf-8")
                    If String.IsNullOrWhiteSpace(profile.Language) OrElse Not System.Text.RegularExpressions.Regex.IsMatch(profile.Language, "\A[a-z]{2,24}\z") Then Throw New ArgumentException("Explicit literal lower-case language token required.")
                    If profile.Tables Is Nothing OrElse profile.Tables.Count = 0 OrElse profile.Tables.Count > 4096 Then Throw New ArgumentException("Expected 1..4096 explicitly selected language tables.")
                    Dim loaded As New HashSet(Of String)(plugins, StringComparer.OrdinalIgnoreCase)
                    Dim total As Long = 0
                    For Each table In profile.Tables
                        If table Is Nothing OrElse Not loaded.Contains(table.Plugin) OrElse Path.GetFileName(table.Plugin) <> table.Plugin Then Throw New ArgumentException("Language table plugin must belong to the explicit load order.")
                        If table.Kind <> "strings" AndAlso table.Kind <> "dlstrings" AndAlso table.Kind <> "ilstrings" Then Throw New ArgumentException("Unknown original string table kind.")
                        Dim filePath = Path.GetFullPath(table.FilePath)
                        Dim filename = Path.GetFileNameWithoutExtension(table.Plugin) & "_" & profile.Language & "." & table.Kind
                        If String.IsNullOrEmpty(table.ResourcePath) AndAlso Not Path.GetFileName(filePath).Equals(filename, StringComparison.OrdinalIgnoreCase) Then Throw New ArgumentException("Table basename must match plugin, selected language and kind.")
                        CheckPath(filePath)
                        If File.Exists(Path.ChangeExtension(filePath, ".cpoverride")) Then Throw New InvalidDataException("Unsealed encoding sidecar is not supported by this export route.")
                        Dim handle As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)
                        scope.tableHandles.Add(handle)
                        If String.IsNullOrEmpty(table.ResourcePath) AndAlso handle.Length > 32L * 1024 * 1024 Then Throw New IOException("Language table exceeds 32 MiB.")

                        Dim digest = Convert.ToHexString(SHA256.HashData(handle)) : handle.Position = 0
                        If Not digest.Equals(table.Sha256, StringComparison.OrdinalIgnoreCase) Then Throw New InvalidDataException("Language table SHA256 mismatch.")
                        scope.expected.Add(digest)
                        Dim aliasKey = "Strings\" & filename
                        Dim location As FilesDictionary_class.File_Location
                        If String.IsNullOrEmpty(table.ResourcePath) Then
                            total += handle.Length
                            location = New FilesDictionary_class.File_Location With {.FullPath = filePath}
                        Else
                            If Not Path.GetExtension(filePath).Equals(".ba2", StringComparison.OrdinalIgnoreCase) OrElse Not table.ResourcePath.Replace("/", "\").Equals(aliasKey, StringComparison.OrdinalIgnoreCase) Then Throw New ArgumentException("BA2 table must name the exact selected Strings resource.")
                            ' Inspect only metadata before allowing the original resource reader to extract this entry.
                            Using reader As New BethesdaReader(New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                                If Not reader.Ba2HeaderVersion.HasValue OrElse reader.EntriesFiles.Count > 1000000 Then Throw New InvalidDataException("BA2 index limit or format rejected.")
                                Dim entries = reader.EntriesFiles.Where(Function(e) e.FullPath.Replace("/", "\").Equals(aliasKey, StringComparison.OrdinalIgnoreCase)).ToList()
                                If entries.Count <> 1 Then Throw New InvalidDataException("Missing or ambiguous BA2 language resource.")
                                Dim entry = entries.Single()
                                Dim payload = reader.GetPayloadSource(entry.Index)
                                If payload.HasDx10Metadata OrElse payload.DecompSize < 8 OrElse payload.DecompSize > 32L * 1024 * 1024 OrElse payload.Length > 32L * 1024 * 1024 Then Throw New IOException("BA2 string entry exceeds bounded GNRL extraction limits.")
                                total += payload.DecompSize
                            End Using
                            If scope.archiveNames.Add(Path.GetFileName(filePath)) Then
                                Dim sourceOrder As Integer
                                If FilesDictionary_class.TryGetArchiveSourceOrder(Path.GetFileName(filePath), sourceOrder) Then Throw New InvalidOperationException("Archive already active in another resource context; use a dedicated headless process.")
                                FilesDictionary_class.Dictionary = New ConcurrentDictionary(Of String, FilesDictionary_class.File_Location)(StringComparer.OrdinalIgnoreCase)
                                FilesDictionary_class.RegisterArchive(filePath)
                                scope.mounted.Add(filePath)
                                scope.archiveEntries.Add(filePath, FilesDictionary_class.Dictionary.ToDictionary(Function(pair) pair.Key, Function(pair) pair.Value, StringComparer.OrdinalIgnoreCase))
                            ElseIf Not scope.mounted.Contains(filePath, StringComparer.OrdinalIgnoreCase) Then
                                Throw New InvalidDataException("Ambiguous archive basenames from different roots.")
                            End If
                            location = Nothing
                            scope.archiveEntries(filePath).TryGetValue(aliasKey, location)
                            If location Is Nothing OrElse Not Path.GetFileName(location.BA2File).Equals(Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase) Then Throw New InvalidDataException("Original BA2 index did not select the declared source.")
                            ' Keep the original entry's live generation token; make its archive identity absolute.
                            location.BA2File = filePath
                            Dim bytes = location.GetBytes()
                            If bytes Is Nothing OrElse Not Convert.ToHexString(SHA256.HashData(bytes)).Equals(table.ResourceSha256, StringComparison.OrdinalIgnoreCase) Then Throw New InvalidDataException("BA2 language resource SHA256 mismatch.")
                        End If
                        If total > 128L * 1024 * 1024 Then Throw New IOException("Language tables exceed 128 MiB.")
                        If Not selected.TryAdd(aliasKey, location) Then Throw New ArgumentException("Ambiguous duplicate language table.")
                        If String.IsNullOrEmpty(table.ResourcePath) AndAlso Not selected.TryAdd(filePath, location) Then Throw New ArgumentException("Duplicate table source.")
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
                For Each archive In mounted
                    FilesDictionary_class.UnregisterArchive(archive)
                Next
                FilesDictionary_class.Dictionary = priorDictionary
                FilesDictionary_class.FO4Path = priorRoot
                FilesDictionary_class.CacheDirectory = priorCache
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
