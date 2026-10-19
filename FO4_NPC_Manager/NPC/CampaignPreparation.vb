Option Strict On
Imports System.IO
Imports System.Linq
Imports System.Text.Json
Imports System.Threading

Namespace ExperimentalTransformation
    Public Class PreparationResult
        Public Property Status As String = "PREPARATION-ONLY-NO-ESP-OR-FACEGEN-WORKER"
        Public Property PageIndex As Integer = -1
        Public Property OutputPath As String = ""
        Public Property RemainingBytes As Long
    End Class
    Public Module CampaignPreparation
        Public Sub Start(input As CampaignInput, saved As CampaignPlan, directory As String, budgetBytes As Long)
            Dim current = CurrentPlan(input, saved)
            ValidateStage(input, directory)
            Using workspace = ReservedWriteBudget.Create(directory, budgetBytes, input.ProtectedRoots.Concat({input.DataRoot}).Concat(If(input.Localization Is Nothing, Array.Empty(Of String)(), input.Localization.Tables.Select(Function(t) t.FilePath))))
                Dim identity = JsonSerializer.SerializeToUtf8Bytes(New String() {current.InputSha256, current.PlanSha256})
                workspace.WriteNew("campaign-identity.json", identity)
            End Using
        End Sub
        Public Function PrepareNext(input As CampaignInput, saved As CampaignPlan, directory As String, Optional cancellation As CancellationToken = Nothing) As PreparationResult
            cancellation.ThrowIfCancellationRequested()
            ValidateStage(input, directory)
            Dim current = CurrentPlan(input, saved)
            Using workspace = ReservedWriteBudget.Recover(directory)
                If Not workspace.VerifiedNames.Contains("campaign-identity.json") Then Throw New InvalidDataException("Missing or interrupted campaign identity.")
                Dim identity = JsonSerializer.Deserialize(Of String())(File.ReadAllBytes(Path.Combine(workspace.DirectoryPath, "campaign-identity.json")))
                If identity.Length <> 2 OrElse identity(0) <> current.InputSha256 OrElse identity(1) <> current.PlanSha256 Then Throw New InvalidDataException("Preparation checkpoint belongs to another profile or plan.")
                Dim result As New PreparationResult With {.RemainingBytes = workspace.RemainingBytes}
                If workspace.Paused Then
                    result.Status = "PAUSED-PREPARATION-ONLY" : Return result
                End If
                Dim verified = workspace.VerifiedNames
                For page = 0 To current.Pages.Count - 1
                    Dim prefix = "page-" & page.ToString("D6") & "-attempt-"
                    If verified.Any(Function(name) name.StartsWith(prefix, StringComparison.Ordinal)) Then Continue For
                    cancellation.ThrowIfCancellationRequested()
                    Dim profile = GlobalCampaign.CreateBatch(input, current, page)
                    Dim bytes = JsonSerializer.SerializeToUtf8Bytes(profile)
                    If bytes.LongLength > input.Policy.MaxReportBytes Then Throw New IOException("Page profile exceeds report byte budget before writing.")
                    Dim attempt = workspace.ReservedNames.Where(Function(candidate) candidate.StartsWith(prefix, StringComparison.Ordinal)).Count()
                    Dim outputName = prefix & attempt.ToString("D4") & ".json"
                    workspace.WriteNew(outputName, bytes)
                    result.PageIndex = page : result.OutputPath = Path.Combine(workspace.DirectoryPath, outputName)
                    result.RemainingBytes = workspace.RemainingBytes
                    Return result
                Next
                result.Status = "ALL-PAGES-PREPARED-NOT-CONVERTED"
                Return result
            End Using
        End Function
        Public Sub SetPaused(input As CampaignInput, directory As String, value As Boolean)
            ValidateStage(input, directory)
            Using workspace = ReservedWriteBudget.Recover(directory)
                If Not workspace.VerifiedNames.Contains("campaign-identity.json") Then Throw New InvalidDataException("Not a verified campaign preparation workspace.")
                workspace.SetPaused(value)
            End Using
        End Sub
        Private Sub ValidateStage(input As CampaignInput, directory As String)
            If input Is Nothing OrElse input.ProtectedRoots Is Nothing Then Throw New ArgumentException("Explicit protected profile roots required.")
            Dim destination = Path.GetFullPath(directory)
            For Each original In input.ProtectedRoots.Concat({input.DataRoot}).Concat(input.Policy.FemalePresets).Concat(If(input.Localization Is Nothing, Array.Empty(Of String)(), input.Localization.Tables.Select(Function(t) t.FilePath)))
                Dim full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(original))
                If destination.Equals(full, StringComparison.OrdinalIgnoreCase) OrElse destination.StartsWith(If(Path.EndsInDirectorySeparator(full), full, full & Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) Then Throw New IOException("Preparation state must remain outside every original root/input.")
            Next
        End Sub
        Private Function CurrentPlan(input As CampaignInput, saved As CampaignPlan) As CampaignPlan
            Dim current = GlobalNpcDiscovery.Scan(input)
            If saved Is Nothing OrElse current.InputSha256 <> saved.InputSha256 OrElse current.PlanSha256 <> saved.PlanSha256 Then Throw New InvalidDataException("Campaign plan is stale; no page writes allowed.")
            If current.ExecutionBlockers.Count <> 0 Then Throw New InvalidOperationException("Campaign has unresolved execution admission blockers.")
            Return current
        End Function
    End Module
End Namespace
