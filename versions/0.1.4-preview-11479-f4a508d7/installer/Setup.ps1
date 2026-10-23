param([ValidateSet('Gui','Install','Uninstall','Check')][string]$Action='Gui',[string]$GamePath='', [switch]$NoUI,[string]$LoaderZip='',[string]$ApiZip='',[string]$PreviewPath='',[ValidateSet('','Install','Uninstall')][string]$ExerciseAction='',[switch]$ExerciseTutorial,[switch]$ExerciseExperience,[ValidateSet('Installation','Help','Support')][string]$PreviewTab='Installation')
$ErrorActionPreference='Stop'
$script:EntryPath=$PSCommandPath
$script:PackageRoot=$PSScriptRoot
. (Join-Path $PSScriptRoot 'InstallerCore.ps1')
$global:LASTEXITCODE=0
try {
    . (Join-Path $PSScriptRoot 'InstallerUI.ps1')
    exit $LASTEXITCODE
} catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
