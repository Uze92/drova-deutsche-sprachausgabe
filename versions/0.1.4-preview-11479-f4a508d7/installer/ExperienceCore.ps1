function Report-InstallerProgress([string]$Phase,[int]$Percent) {
    if($env:DEUTSCHE_STIMMEN_PROGRESS){
        try{[IO.File]::WriteAllText($env:DEUTSCHE_STIMMEN_PROGRESS,(@{phase=$Phase;percent=$Percent}|ConvertTo-Json -Compress),[Text.Encoding]::UTF8)}catch{}
    }
}
function Get-InstallerState([string]$Target) {
    $checks=New-Object 'System.Collections.Generic.List[string]'
    $installed='Noch nicht installiert';$valid=$true
    try{Assert-GameFolder $Target;$checks.Add('✓ Spiel gefunden')}catch{$checks.Add('○ Spielordner auswählen');$valid=$false}
    $gameFound=$valid
    if($valid -and $package){
        $compatible=$true
        foreach($entry in $package.game_content){$path=Get-SafePath $Target $entry.path;if(-not (Test-Path -LiteralPath $path) -or (Get-FileHashText $path) -ne $entry.sha256){$compatible=$false}}
        if($compatible){$checks.Add('✓ Datenversion unterstützt')}else{$checks.Add('✕ Datenversion passt nicht');$valid=$false}
    }
    if($Target){
        try{
            $receiptPath=Get-SafePath $Target ($profile.state_directory+'/installed.json')
            if(Test-Path -LiteralPath $receiptPath){
                $receipt=Get-Content -LiteralPath $receiptPath -Raw -Encoding UTF8|ConvertFrom-Json
                $installed='Version '+$receipt.version+' installiert'
                foreach($entry in $receipt.files){$path=Get-SafePath $Target $entry.path;if(-not(Test-Path -LiteralPath $path) -or (Get-FileHashText $path) -ne $entry.sha256){$installed='Dateien verändert oder unvollständig (Version '+$receipt.version+')';break}}
            }elseif($gameFound -and $profile.state_directory -eq '.DrovaVoices-Setup' -and (Test-Path -LiteralPath (Get-SafePath $Target 'Mods/DrovaVoices.dll'))){
                $installed='Vorhandene Sprachmod ohne Setup-Beleg – zum Verwalten aktualisieren'
            }
        }catch{$installed='Installationsbeleg nicht lesbar';$valid=$false}
    }
    if($package){foreach($dependency in $package.dependencies){
        $present=$false;$provided=$false
        if($gameFound -and $dependency.PSObject.Properties['files']){
            $present=$true
            foreach($entry in $dependency.files){$path=Get-SafePath $Target $entry.path;if(-not(Test-Path -LiteralPath $path) -or (Get-FileHashText $path) -ne $entry.sha256){$present=$false;break}}
        }
        if($script:DependencyArchives.ContainsKey($dependency.sha256)){$zip=$script:DependencyArchives[$dependency.sha256];$provided=(Test-Path -LiteralPath $zip) -and (Get-FileHashText $zip) -eq $dependency.sha256}
        if($present){$checks.Add('✓ '+$dependency.name+' vorhanden')}
        elseif($provided){$checks.Add('✓ '+$dependency.name+' · ZIP geprüft')}
        else{$checks.Add('○ '+$dependency.name+' · ZIP auswählen')}
    }}
    return [pscustomobject]@{installed=$installed;checks=@($checks.ToArray());compatible=$valid}
}
function Hide-PrivatePaths([string]$Text) {
    return [regex]::Replace($Text,'(?i)[a-z]:[\\/][^\r\n]*|\\\\[^\r\n]+','[privater Pfad]')
}
