[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][ValidateSet('Discover','AnalyzeNarrative','StartPreparation','NextPreparation','PausePreparation','ResumePreparation')][string]$Task,
    [Parameter(Mandatory=$true)][string]$CliPath,
    [Parameter(Mandatory=$true)][ValidatePattern('^[A-Fa-f0-9]{64}$')][string]$CliSha256,
    [Parameter(Mandatory=$true)][string]$ProfilePath,
    [string]$PlanPath,
    [string]$OutputPath,
    [string]$StagingDirectory,
    [long]$BudgetBytes = 1048576
)
$ErrorActionPreference = 'Stop'
function Assert-SeparatePath([string]$Path) {
    if (!$Path) { return }
    $full = [IO.Path]::GetFullPath($Path)
    if ($full -match '(?i)(^|[\\/])CRU-Female-Lite([\\/]|$)') { throw 'Keep this project separate from CRU-Female-Lite' }
    for ($cursor = $full; $cursor; $cursor = [IO.Path]::GetDirectoryName($cursor)) {
        if ((Test-Path -LiteralPath $cursor) -and ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Reparse path refused' }
    }
}
foreach ($path in @($CliPath,$ProfilePath,$PlanPath,$OutputPath,$StagingDirectory)) { Assert-SeparatePath $path }
if ((Get-FileHash -LiteralPath $CliPath -Algorithm SHA256).Hash -ne $CliSha256) { throw 'CLI fingerprint mismatch' }
if ((Get-Item -LiteralPath $ProfilePath).Length -gt 16MB) { throw 'Profile exceeds 16 MiB' }
$profile = Get-Content -Raw -LiteralPath $ProfilePath | ConvertFrom-Json
Assert-SeparatePath $profile.DataRoot
foreach ($root in $profile.ProtectedRoots) { Assert-SeparatePath $root }
if ($profile.Localization) {
    foreach ($table in $profile.Localization.Tables) { Assert-SeparatePath $table.FilePath }
}
$invokeArgs = @()
switch ($Task) {
    'Discover' { if (!$OutputPath) { throw 'OutputPath required' }; $invokeArgs = @('--discover',$ProfilePath,$OutputPath) }
    'AnalyzeNarrative' { if (!$OutputPath) { throw 'OutputPath required' }; $invokeArgs = @('--analyze-narrative',$ProfilePath,$OutputPath) }
    'StartPreparation' { if (!$PlanPath -or !$StagingDirectory) { throw 'PlanPath and new StagingDirectory required' }; $invokeArgs = @('--start-preparation',$ProfilePath,$PlanPath,$StagingDirectory,$BudgetBytes.ToString([Globalization.CultureInfo]::InvariantCulture)) }
    'NextPreparation' { if (!$PlanPath -or !$StagingDirectory) { throw 'PlanPath and StagingDirectory required' }; $invokeArgs = @('--next-preparation',$ProfilePath,$PlanPath,$StagingDirectory) }
    'PausePreparation' { if (!$StagingDirectory) { throw 'StagingDirectory required' }; $invokeArgs = @('--pause-preparation',$ProfilePath,$StagingDirectory) }
    'ResumePreparation' { if (!$StagingDirectory) { throw 'StagingDirectory required' }; $invokeArgs = @('--resume-preparation',$ProfilePath,$StagingDirectory) }
}
# Invoke only the fingerprinted existing experimental CLI. No downloads/install/VFS configuration.
& $CliPath @invokeArgs
exit $LASTEXITCODE
