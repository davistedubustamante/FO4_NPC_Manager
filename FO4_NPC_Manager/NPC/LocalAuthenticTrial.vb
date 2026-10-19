Option Strict On
Imports System.IO
Imports System.Linq
Imports System.Security.Cryptography
Imports System.Text.Json
Imports FO4_Base_Library
Imports BSA_BA2_Library_DLL.BethesdaArchive.Core

Namespace ExperimentalTransformation
    Public Class TrialResource
        Public Property Role As String = ""
        Public Property FilePath As String = ""
        Public Property Sha256 As String = ""
        Public Property ResourcePath As String = ""
    End Class
    Public Class AuthenticTrialInput
        Public Property CollectionIdentity As String = ""
        Public Property IsolatedProfileEvidence As String = ""
        Public Property Profile As New NpcBatchProfile
        Public Property Resources As New List(Of TrialResource)
    End Class
    Public Class ValidatedAsset
        Public Property Role As String = ""
        Public Property FilePath As String = ""
        Public Property Sha256 As String = ""
        Public Property SourceSha256 As String = ""
        Public Property ResourcePath As String = ""
        Public Property Bytes As Long
        Public Property Shapes As Integer
        Public Property Width As Integer
        Public Property Height As Integer
    End Class
    Public Class AuthenticTrialReport
        Public Property Status As String = "PREFLIGHT-ONLY-NOT-A-CONVERSION"
        Public Property CollectionIdentity As String = ""
        Public Property InputSha256 As String = ""
        Public Property NpcKey As String = ""
        Public Property NarrativeState As String = ""
        Public Property Assets As New List(Of ValidatedAsset)
        Public Property Blockers As String() = {"all direct-path ESP/FaceGen/cache/temp writes need verified quota admission", "preset/FaceGen identity and body/equipment compatibility need real staged integration", "ESP export/reopen and generated assets need a complete controlled trial", "quests/dialogue/audio and game runtime unvalidated"}
    End Class
    Public Module LocalAuthenticTrial
        ' Read-only local resource validation. A positive report NEVER authorizes conversion/installation.
        Public Function Inspect(input As AuthenticTrialInput) As AuthenticTrialReport
            If input Is Nothing OrElse String.IsNullOrWhiteSpace(input.CollectionIdentity) OrElse String.IsNullOrWhiteSpace(input.IsolatedProfileEvidence) OrElse input.Profile Is Nothing OrElse input.Resources Is Nothing OrElse input.Resources.Count > 64 Then Throw New ArgumentException("Explicit isolated local profile, collection identity and bounded resource manifest required.")
            Dim profile = input.Profile
            CheckPath(profile.DataRoot)
            For Each root In profile.ProtectedRoots
                CheckPath(root)
            Next
            If profile.Targets.Count <> 1 OrElse Not profile.RequireFaceGen Then Throw New ArgumentException("Authentic trial requires exactly one FaceGen target.")
            Dim target = profile.Targets.Single()
            CheckPath(target.AppearancePreset)
            If target.Age <> AgeEvidence.Adult OrElse String.IsNullOrWhiteSpace(target.AdultEvidence) OrElse String.IsNullOrWhiteSpace(target.AppearancePreset) Then Throw New InvalidDataException("Reviewed adulthood and female preset required.")
            Dim required = {"facegen-nif", "facegen-dds", "body-nif", "equipment-nif"}
            If input.Resources.Any(Function(r) r Is Nothing OrElse Not required.Contains(r.Role)) OrElse required.Any(Function(role) Not input.Resources.Any(Function(r) r.Role = role)) Then Throw New ArgumentException("FaceGen NIF/DDS, body NIF and equipment NIF resources required.")
            Dim campaign As New CampaignInput With {.DataRoot = profile.DataRoot, .LoadOrder = profile.LoadOrder, .ProtectedRoots = profile.ProtectedRoots, .Localization = profile.Localization}
            campaign.Policy.HumanRaces.Add(target.HumanRacePlugin.ToUpperInvariant() & "|" & target.HumanRaceObjectId.ToString("X6"))
            campaign.Policy.AdultEvidence.Add(target.Key, target.AdultEvidence)
            campaign.Policy.FemalePresets.Add(target.AppearancePreset)
            Dim plan = NarrativeDependencies.DiscoverReviewed(campaign)
            Dim decision = plan.Decisions.SingleOrDefault(Function(d) d.Npc.Key.Equals(target.Key, StringComparison.OrdinalIgnoreCase))
            If decision Is Nothing OrElse decision.Npc.ChildRace OrElse Not decision.Npc.RaceFlagsKnown OrElse decision.Npc.Female OrElse decision.Npc.Deleted OrElse decision.Npc.TemplateFlags <> 0US OrElse decision.Npc.ErrorDetail <> "" OrElse decision.Npc.RaceKey <> campaign.Policy.HumanRaces.Single() Then Throw New InvalidDataException("Target is not a fully readable reviewed adult human male in this profile.")
            Dim report As New AuthenticTrialReport With {.CollectionIdentity = input.CollectionIdentity, .InputSha256 = plan.InputSha256, .NpcKey = target.Key, .NarrativeState = decision.Npc.NarrativeState}
            Dim total As Long = 0
            For Each resource In input.Resources
                Dim asset = ValidateSelectedResource(resource, 128L * 1024 * 1024 - total)
                total += asset.Bytes
                report.Assets.Add(asset)
            Next
            Return report
        End Function
        Public Function ValidateSelectedResource(resource As TrialResource, Optional remainingBytes As Long = 128L * 1024 * 1024) As ValidatedAsset
            ArgumentNullException.ThrowIfNull(resource)
            If Not {"facegen-nif", "facegen-dds", "body-nif", "equipment-nif"}.Contains(resource.Role) Then Throw New ArgumentException("Explicit trial resource role required.")
            Dim full = Path.GetFullPath(resource.FilePath)
            CheckPath(full)
            Dim extension = If(resource.Role.EndsWith("dds", StringComparison.Ordinal), ".dds", ".nif")
            Dim bytes As Byte()
            Dim sourceHash As String
            Using stream As New FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read)
                Dim packed = Not String.IsNullOrEmpty(resource.ResourcePath)
                If stream.Length < 1 OrElse stream.Length > If(packed, 16L * 1024 * 1024 * 1024, 32L * 1024 * 1024) Then Throw New IOException("Trial source exceeds bounded file/archive limit.")
                sourceHash = Convert.ToHexString(SHA256.HashData(stream)) : stream.Position = 0
                If Not sourceHash.Equals(resource.Sha256, StringComparison.OrdinalIgnoreCase) Then Throw New InvalidDataException("Trial source hash mismatch.")
                If Not packed Then
                    If stream.Length > remainingBytes OrElse Not Path.GetExtension(full).Equals(extension, StringComparison.OrdinalIgnoreCase) Then Throw New IOException("Trial asset exceeds pool budget or role extension.")
                    ReDim bytes(CInt(stream.Length) - 1) : stream.ReadExactly(bytes)
                Else
                    If Not Path.GetExtension(full).Equals(".ba2", StringComparison.OrdinalIgnoreCase) OrElse Not Path.GetExtension(resource.ResourcePath).Equals(extension, StringComparison.OrdinalIgnoreCase) Then Throw New InvalidDataException("Exact BA2 resource matching asset role required.")
                    Using reader As New BethesdaReader(stream)
                        If Not reader.Ba2HeaderVersion.HasValue OrElse reader.EntriesFiles.Count > 1000000 Then Throw New InvalidDataException("Unsupported or oversized archive index.")
                        Dim key = resource.ResourcePath.Replace("/", "\")
                        Dim entries = reader.EntriesFiles.Where(Function(e) e.FullPath.Replace("/", "\").Equals(key, StringComparison.OrdinalIgnoreCase)).ToArray()
                        If entries.Length <> 1 Then Throw New InvalidDataException("Missing or ambiguous authentic archive member.")
                        ' Original API exposes exact payload metadata for single-chunk GNRL/DX10.
                        ' Unsupported multi-chunk layouts remain blocked rather than extracted without a bound.
                        Dim payload = reader.GetPayloadSource(entries(0).Index)
                        Dim maximum = Math.Min(32L * 1024 * 1024, remainingBytes)
                        Dim headerBytes As Long = If(payload.HasDx10Metadata, 148L, 0L)
                        If payload.DecompSize < 1 OrElse payload.DecompSize > maximum - headerBytes OrElse payload.Length < 1 OrElse payload.Length > 32L * 1024 * 1024 OrElse payload.Offset < 0 OrElse payload.Offset > stream.Length - payload.Length Then Throw New IOException("Authentic member exceeds pre-extraction byte limits.")
                        If payload.HasDx10Metadata AndAlso (extension <> ".dds" OrElse payload.Width < 1 OrElse payload.Height < 1 OrElse payload.Width > 4096 OrElse payload.Height > 4096 OrElse payload.Faces <> 1 OrElse payload.IsCubemap OrElse payload.MipCount > 13) Then Throw New InvalidDataException("Bounded single-face DX10 resource required.")
                        bytes = reader.ExtractToMemory(entries(0).Index)
                    End Using
                End If
            End Using
            Dim result = ValidateAsset(bytes, extension)
            If result.Bytes > remainingBytes Then Throw New IOException("Trial asset pool exhausted.")
            result.Role = resource.Role : result.FilePath = full
            result.SourceSha256 = sourceHash : result.ResourcePath = resource.ResourcePath
            Return result
        End Function
        Public Function ValidateAsset(bytes As Byte(), extension As String) As ValidatedAsset
            If bytes Is Nothing OrElse bytes.Length < 1 OrElse bytes.Length > 32 * 1024 * 1024 Then Throw New InvalidDataException("Bounded nonempty asset required.")
            Dim result As New ValidatedAsset With {.Bytes = bytes.LongLength, .Sha256 = Convert.ToHexString(SHA256.HashData(bytes))}
            If extension = ".nif" Then
                Dim nif As New Nifcontent_Class_Manolo()
                Try
                    nif.Load_Manolo(bytes)
                    If nif.Header.Version.UserVersion <> 12 OrElse nif.Header.Version.StreamVersion <> 130 OrElse nif.GetRootNode() Is Nothing Then Throw New InvalidDataException("Fallout 4 NIF version/root required.")
                    Dim shapes = nif.GetShapes().ToList()
                    If shapes.Count = 0 OrElse shapes.Any(Function(s) s.VertexCount <= 0 OrElse s.TriangleCount <= 0) Then Throw New InvalidDataException("NIF must contain nonempty geometry.")
                    result.Shapes = shapes.Count
                Catch ex As Exception
                    Throw New InvalidDataException("Original NIF reader rejected trial resource: " & ex.Message, ex)
                End Try
            ElseIf extension = ".dds" Then
                ' Bound allocation before native decoding. Native DirectXTex remains the DDS decoder.
                If bytes.Length < 128 OrElse System.Text.Encoding.ASCII.GetString(bytes, 0, 4) <> "DDS " OrElse BitConverter.ToUInt32(bytes, 4) <> 124UI Then Throw New InvalidDataException("DDS header required.")
                Dim width = BitConverter.ToUInt32(bytes, 16), height = BitConverter.ToUInt32(bytes, 12)
                If width < 1 OrElse height < 1 OrElse width > 4096 OrElse height > 4096 OrElse BitConverter.ToUInt32(bytes, 24) > 1UI OrElse BitConverter.ToUInt32(bytes, 28) > 13UI OrElse BitConverter.ToUInt32(bytes, 112) <> 0UI Then Throw New InvalidDataException("Bounded 2D FaceGen DDS required.")
                If System.Text.Encoding.ASCII.GetString(bytes, 84, 4) = "DX10" AndAlso (bytes.Length < 148 OrElse BitConverter.ToUInt32(bytes, 132) <> 3UI OrElse (BitConverter.ToUInt32(bytes, 136) And 4UI) <> 0UI OrElse BitConverter.ToUInt32(bytes, 140) <> 1UI) Then Throw New InvalidDataException("DDS arrays/volumes cannot enter a FaceGen trial.")
                Dim texture = DirectXTexWrapperCLI.Loader.ConvertForBitmap(bytes)
                If texture Is Nothing OrElse Not texture.Loaded OrElse texture.Levels Is Nothing OrElse texture.Levels.Count = 0 OrElse texture.Levels(0).Data Is Nothing OrElse texture.Levels(0).Data.Length = 0 OrElse texture.Levels(0).Width <> width OrElse texture.Levels(0).Height <> height Then Throw New InvalidDataException("Native DDS decoder rejected trial resource.")
                result.Width = CInt(width) : result.Height = CInt(height)
            Else
                Throw New ArgumentException("Only NIF/DDS validation is supported.")
            End If
            Return result
        End Function
        Public Sub WriteReport(input As AuthenticTrialInput, report As AuthenticTrialReport, newDirectory As String)
            Dim roots = input.Profile.ProtectedRoots.Concat({input.Profile.DataRoot, input.Profile.Targets.Single().AppearancePreset}).Concat(input.Resources.Select(Function(r) r.FilePath)).Concat(If(input.Profile.Localization Is Nothing, Array.Empty(Of String)(), input.Profile.Localization.Tables.Select(Function(t) t.FilePath)))
            Using budget = ReservedWriteBudget.Create(newDirectory, 1024 * 1024, roots)
                budget.WriteNew("authentic-preflight.json", JsonSerializer.SerializeToUtf8Bytes(report))
            End Using
        End Sub
        Private Sub CheckPath(value As String)
            Dim current = Path.GetFullPath(value)
            If current.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(Function(part) part.Equals("CRU-Female-Lite", StringComparison.OrdinalIgnoreCase)) Then Throw New IOException("Keep authentic trial separate from CRU-Female-Lite.")
            While Not String.IsNullOrEmpty(current)
                If (File.Exists(current) OrElse Directory.Exists(current)) AndAlso (File.GetAttributes(current) And FileAttributes.ReparsePoint) <> 0 Then Throw New IOException("Reparse trial asset rejected.")
                current = Path.GetDirectoryName(current)
            End While
        End Sub
    End Module
End Namespace
