[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$GameRoot,
    [Parameter(Mandatory=$true)][string]$OutputRoot,
    [string]$ModsRoot,
    [string]$ProfileRoot,
    [switch]$ArchiveIndex
)
$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'inspect_wod.py'
$scanArgs = @($scriptPath, '--root', "game=$GameRoot", '--output', $OutputRoot)
if ($ModsRoot) { $scanArgs += @('--root', "mods=$ModsRoot") }
if ($ProfileRoot) { $scanArgs += @('--root', "profile=$ProfileRoot") }
if ($ArchiveIndex) { $scanArgs += '--archive-index' }
$launcher = Get-Command py -ErrorAction SilentlyContinue
if ($launcher) {
    & $launcher.Source -3 @scanArgs
} else {
    $python = Get-Command python -ErrorAction Stop
    & $python.Source @scanArgs
}
exit $LASTEXITCODE
