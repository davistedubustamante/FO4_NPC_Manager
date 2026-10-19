Imports System.Linq
Imports System.IO
Imports System.Text.Json
Imports FO4_NPC_Manager.ExperimentalTransformation
Module Program
    Function Main(args As String()) As Integer
        Try
            If args.Length = 3 AndAlso (args(0) = "--pause-preparation" OrElse args(0) = "--resume-preparation") Then
                CampaignPreparation.SetPaused(ReadCampaign(args(1)), args(2), args(0) = "--pause-preparation")
                Return 0
            End If
            If (args.Length = 5 AndAlso args(0) = "--start-preparation") OrElse (args.Length = 4 AndAlso args(0) = "--next-preparation") Then
                Dim campaign = ReadCampaign(args(1))
                If New FileInfo(args(2)).Length > 64L * 1024 * 1024 Then Throw New IOException("Saved plan exceeds 64 MiB.")
                Dim plan = JsonSerializer.Deserialize(Of CampaignPlan)(File.ReadAllBytes(args(2)))
                If args(0) = "--start-preparation" Then
                    CampaignPreparation.Start(campaign, plan, args(3), Long.Parse(args(4), Globalization.CultureInfo.InvariantCulture))
                Else
                    Console.WriteLine(JsonSerializer.Serialize(CampaignPreparation.PrepareNext(campaign, plan, args(3))))
                End If
                Return 0
            End If
            If args.Length = 3 AndAlso args(0) = "--analyze-narrative" Then
                Dim campaign = ReadCampaign(args(1))
                Dim report = NarrativeDependencies.Analyze(campaign)
                GlobalNpcDiscovery.WriteNarrativeReport(campaign, report, args(2))
                Console.WriteLine($"{report.Edges.Count} static reference edges; quest compatibility NOT validated.")
                Return 0
            End If
            If args.Length = 5 AndAlso args(0) = "--prepare-page" Then
                Dim campaign = ReadCampaign(args(1))
                If New FileInfo(args(2)).Length > 64L * 1024 * 1024 Then Throw New IOException("Saved plan exceeds 64 MiB.")
                Dim saved = JsonSerializer.Deserialize(Of CampaignPlan)(File.ReadAllBytes(args(2)))
                GlobalNpcDiscovery.PreparePage(campaign, saved, Integer.Parse(args(3), Globalization.CultureInfo.InvariantCulture), args(4))
                Console.WriteLine("Prepared verified input page; no patch generated or installed.")
                Return 0
            End If
            If args.Length = 3 AndAlso args(0) = "--discover" Then
                Dim campaign = ReadCampaign(args(1))
                Dim plan = GlobalNpcDiscovery.Scan(campaign)
                GlobalNpcDiscovery.WriteReport(campaign, plan, args(2))
                Console.WriteLine($"{plan.Decisions.Count} winning NPC records; {plan.Pages.Count} planned pages; NOT converted. {plan.PlanSha256}")
                Return 0
            End If
            If args.Length < 2 OrElse args.Length > 3 Then Throw New ArgumentException("NpcConversionCli <private-profile.json> <existing-staging-parent> [resume-staging-directory]")
            Dim profile = JsonSerializer.Deserialize(Of NpcBatchProfile)(File.ReadAllBytes(args(0)), New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True})
            Dim result = ProductionNpcBatch.Run(profile, args(1), If(args.Length = 3, args(2), Nothing))
            Console.WriteLine(JsonSerializer.Serialize(result, New JsonSerializerOptions With {.WriteIndented = True}))
            If result.Items.Any(Function(i) i.Status = "BLOCKED-PARTIAL-NOT-INSTALLED") OrElse result.BundleStatus.StartsWith("BLOCKED", StringComparison.Ordinal) Then Return 2
            Return 0
        Catch ex As Exception
            Console.Error.WriteLine(ex.ToString())
            Return 1
        End Try
    End Function
    Private Function ReadCampaign(filePath As String) As CampaignInput
        If New FileInfo(filePath).Length > 16L * 1024 * 1024 Then Throw New IOException("Campaign policy exceeds 16 MiB.")
        Return JsonSerializer.Deserialize(Of CampaignInput)(File.ReadAllBytes(filePath), New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True})
    End Function
End Module
