Imports System.Linq
Imports System.IO
Imports System.Text.Json
Imports FO4_NPC_Manager.ExperimentalTransformation
Module Program
    Function Main(args As String()) As Integer
        Try
            If args.Length = 5 AndAlso args(0) = "--prepare-page" Then
                Dim campaign = JsonSerializer.Deserialize(Of CampaignInput)(File.ReadAllBytes(args(1)), New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True})
                Dim saved = JsonSerializer.Deserialize(Of CampaignPlan)(File.ReadAllBytes(args(2)))
                GlobalNpcDiscovery.PreparePage(campaign, saved, Integer.Parse(args(3), Globalization.CultureInfo.InvariantCulture), args(4))
                Console.WriteLine("Prepared verified input page; no patch generated or installed.")
                Return 0
            End If
            If args.Length = 3 AndAlso args(0) = "--discover" Then
                Dim campaign = JsonSerializer.Deserialize(Of CampaignInput)(File.ReadAllBytes(args(1)), New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True})
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
End Module
