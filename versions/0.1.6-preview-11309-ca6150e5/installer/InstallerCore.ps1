Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:DependencyArchives=@{}

function Get-SafePath([string]$Root,[string]$Relative) {
    if ([IO.Path]::IsPathRooted($Relative) -or $Relative -match ':' -or @($Relative -split '[\\/]' | Where-Object {$_ -in @('','..','.')}).Count) { throw "Ungueltiger Dateipfad: $Relative" }
    $base=[IO.Path]::GetFullPath($Root).TrimEnd('\')
    $path=[IO.Path]::GetFullPath((Join-Path $base $Relative))
    if (-not $path.StartsWith($base+'\',[StringComparison]::OrdinalIgnoreCase)) {throw 'Datei ausserhalb des Zielordners.'}
    $walk=$path
    while ($walk) {
        $attributes=0
        try {$attributes=[IO.File]::GetAttributes($walk)}
        catch [IO.FileNotFoundException] {}
        catch [IO.DirectoryNotFoundException] {}
        if ($attributes -band [IO.FileAttributes]::ReparsePoint) {throw "Verknuepfte Ordner werden nicht veraendert: $walk"}
        $walk=[IO.Path]::GetDirectoryName($walk)
    }
    return $path
}
function Get-FileHashText([string]$Path) {
    $stream=[IO.File]::OpenRead($Path);$hash=[Security.Cryptography.SHA256]::Create()
    try {return [BitConverter]::ToString($hash.ComputeHash($stream)).Replace('-','').ToLowerInvariant()}
    finally {$hash.Dispose();$stream.Dispose()}
}
function Write-JsonFile($Path,$Value) {
    $parent=Split-Path -Parent $Path
    [IO.Directory]::CreateDirectory($parent) | Out-Null
    $temp=$Path+'.tmp'
    $Value | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $temp -Encoding UTF8
    Move-Item -LiteralPath $temp -Destination $Path -Force
}
function Assert-GameClosed {
    if (Get-Process -Name Drova -ErrorAction SilentlyContinue) {throw 'Bitte Drova zuerst vollstaendig schliessen.'}
}
function Assert-GameFolder([string]$GamePath) {
    $info=Get-SafePath $GamePath 'Drova_Data/app.info'
    if (-not (Test-Path -LiteralPath (Get-SafePath $GamePath 'Drova.exe')) -or -not (Test-Path -LiteralPath $info)) {throw 'Bitte den Spielordner auswaehlen, in dem Drova.exe liegt.'}
    if ((Get-Content -LiteralPath $info -Raw) -notmatch '(?m)^Drova\s*$') {throw 'Dieser Ordner wurde nicht als Drova erkannt.'}
}
function Get-SteamGames {
    $roots=@()
    $found=New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($key in @('HKCU:\Software\Valve\Steam','HKLM:\SOFTWARE\WOW6432Node\Valve\Steam')) {
        $item=Get-ItemProperty -LiteralPath $key -ErrorAction SilentlyContinue
        if ($item) {foreach ($property in @('SteamPath','InstallPath')) {if ($item.PSObject.Properties[$property]) {$roots+= $item.$property}}}
    }
    if (${env:ProgramFiles(x86)}) {$roots+=Join-Path ${env:ProgramFiles(x86)} 'Steam'}
    $libraries=@($roots)
    foreach ($root in $roots) {
        $vdf=Join-Path $root 'steamapps/libraryfolders.vdf'
        if (Test-Path -LiteralPath $vdf) {
            foreach ($match in [regex]::Matches((Get-Content -LiteralPath $vdf -Raw),'"path"\s+"([^"]+)"')) {$libraries+=$match.Groups[1].Value.Replace('\\','\')}
        }
    }
    foreach ($library in @($libraries | Select-Object -Unique)) {
        $manifest=Join-Path $library 'steamapps/appmanifest_1585180.acf'
        if (Test-Path -LiteralPath $manifest) {
            $match=[regex]::Match((Get-Content -LiteralPath $manifest -Raw),'"installdir"\s+"([^"]+)"')
            if ($match.Success) {
                $game=Get-SafePath (Join-Path $library 'steamapps/common') $match.Groups[1].Value
                $game=[IO.Path]::GetFullPath($game.Replace('/','\'))
                if ((Test-Path -LiteralPath (Join-Path $game 'Drova.exe')) -and $found.Add($game)) {$game}
            }
        }
    }
}
function Get-DependencyArchive($Dependency,[string]$Cache) {
    [IO.Directory]::CreateDirectory($Cache) | Out-Null
    $path=Get-SafePath $Cache ($Dependency.sha256+'.zip')
    if (-not (Test-Path -LiteralPath $path)) {
        if (-not $script:DependencyArchives.ContainsKey($Dependency.sha256)) {throw "Bitte zuerst die offizielle ZIP fuer $($Dependency.name) im Installer auswaehlen."}
        $selected=$script:DependencyArchives[$Dependency.sha256]
        if ((Get-FileHashText $selected) -ne $Dependency.sha256) {throw "Die ausgewaehlte ZIP passt nicht zu $($Dependency.name). Bitte die verlinkte Originalversion verwenden."}
        Copy-Item -LiteralPath $selected -Destination $path
    }
    if ((Get-FileHashText $path) -ne $Dependency.sha256) {throw 'Komponenten-Download unvollstaendig oder veraendert. Bitte Cachedatei entfernen und erneut versuchen.'}
    return $path
}
function Get-FileKind([string]$Name) {
    $n=$Name.Replace('\','/')
    if ($n -in @('Mods/DrovaVoices.dll','Mods/DrovaAutoDialogue.dll',
        'Mods/Modding_API/Localization/de/BenchlerAutoDialogue_de.loc',
        'Mods/Modding_API/Localization/en/BenchlerAutoDialogue_en.loc') -or $n.StartsWith('Mods/DrovaVoices/')) {return 'own'}
    if ($n.StartsWith('MelonLoader/') -or $n -in @('version.dll','Mods/Drova_Modding_API.dll','UserLibs/LiteNetLib.dll') -or $n.StartsWith('Mods/Modding_API/Localization/')) {return 'shared'}
    throw "Dieses Paket darf die Datei nicht installieren: $Name"
}
function Install-Voices([string]$PackageRoot,[string]$GamePath) {
    Report-InstallerProgress 'Spieldaten prüfen' 3
    Assert-GameClosed; Assert-GameFolder $GamePath
    $GamePath=[IO.Path]::GetFullPath($GamePath).TrimEnd('\')
    $manifest=Get-Content -LiteralPath (Join-Path $PackageRoot 'Paket.json') -Raw | ConvertFrom-Json
    if ($manifest.schema -ne 1) {throw 'Unbekanntes Paketformat.'}
    foreach ($check in $manifest.game_content) {
        $file=Get-SafePath $GamePath $check.path
        if (-not (Test-Path -LiteralPath $file) -or (Get-FileHashText $file) -ne $check.sha256) {throw 'Die Spieldaten passen nicht zu dieser Mod-Ausgabe. Bitte Spielstand/Mod-Version pruefen und ein passendes Mod-Update verwenden.'}
    }
    $stateRoot=Get-SafePath $GamePath '.DrovaVoices-Setup'
    $statePath=Get-SafePath $stateRoot 'installed.json'
    $previous=$null
    if (Test-Path -LiteralPath $statePath) {
        $previous=Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
        foreach ($row in $previous.files) {
            if ($row.kind -ne 'own') {continue}
            if ((Get-FileKind $row.path) -ne 'own') {throw 'Ungueltige Installationsdaten: fremde Dateien werden nicht veraendert.'}
            $target=Get-SafePath $GamePath $row.path
            if ((Test-Path -LiteralPath $target) -and (Get-FileHashText $target) -ne $row.sha256) {throw "Eine installierte Mod-Datei wurde manuell veraendert: $($row.path). Sie bleibt erhalten."}
        }
    }
    Report-InstallerProgress 'Modpaket und Audio prüfen' 10
    $sources=@();$seen=New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($row in $manifest.files) {
        $kind=Get-FileKind $row.path
        if (-not $seen.Add($row.path)) {throw 'Doppelter Dateipfad im Paket.'}
        $source=Get-SafePath (Join-Path $PackageRoot 'Mod-Dateien') $row.path
        if ((Get-FileHashText $source) -ne $row.sha256) {throw "Mod-Download beschaedigt: $($row.path)"}
        $sources+= [pscustomobject]@{path=$row.path;sha256=$row.sha256;kind=$kind;source=$source}
    }
    Report-InstallerProgress 'Werkzeuge prüfen und vorbereiten' 30
    $history=Get-SafePath $stateRoot ('history/'+[guid]::NewGuid().ToString('N'))
    [IO.Directory]::CreateDirectory($history) | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    # Short external staging paths avoid the legacy Windows/.NET 260-char limit.
    $tempRoot=[IO.Path]::GetTempPath()
    $stageName='DrovaV-'+[guid]::NewGuid().ToString('N').Substring(0,8)
    $stageRoot=Get-SafePath $tempRoot $stageName
    [IO.Directory]::CreateDirectory($stageRoot) | Out-Null
    try {
    foreach ($dependency in $manifest.dependencies) {
        $present=$false
        if ($dependency.PSObject.Properties['files']) {
            $present=$true
            foreach ($entry in $dependency.files) {
                $target=Get-SafePath $GamePath $entry.path
                if (-not (Test-Path -LiteralPath $target) -or (Get-FileHashText $target) -ne $entry.sha256) {$present=$false;break}
            }
        }
        if ($present) {
            foreach ($entry in $dependency.files) {
                if ((Get-FileKind $entry.path) -ne 'shared' -or -not $seen.Add($entry.path)) {throw 'Ungueltige Komponentenliste.'}
                $sources+=[pscustomobject]@{path=$entry.path;sha256=$entry.sha256;kind='shared';source=(Get-SafePath $GamePath $entry.path)}
            }
            continue
        }
        $archive=Get-DependencyArchive $dependency (Join-Path $stateRoot 'downloads')
        $zip=[IO.Compression.ZipFile]::OpenRead($archive)
        try {
            foreach ($entry in $zip.Entries) {
                if (-not $entry.Name) {continue}
                $name=$entry.FullName.Replace('\','/');$kind=Get-FileKind $name
                if ($kind -ne 'shared' -or -not $seen.Add($name)) {throw 'Unerwarteter Komponenteninhalt.'}
                $source=Get-SafePath $stageRoot $name
                [IO.Directory]::CreateDirectory((Split-Path -Parent $source)) | Out-Null
                [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,$source,$false)
                $sources+=[pscustomobject]@{path=$name;sha256=(Get-FileHashText $source);kind='shared';source=$source}
            }
        } finally {$zip.Dispose()}
    }
    # Finish every conflict/path check before changing any active game/mod file.
    foreach ($row in $sources) {
        $target=Get-SafePath $GamePath $row.path
        if ((Test-Path -LiteralPath $target) -and $row.kind -eq 'shared' -and (Get-FileHashText $target) -ne $row.sha256) {throw "Andere vorhandene Mod-Komponente: $($row.path). Sie wird nicht ueberschrieben. Bitte die passende Loader/API-Version verwenden."}
    }
    Report-InstallerProgress 'Konflikte und Sicherungen vorbereiten' 45
    $operations=@()
    foreach ($row in $sources) {
        $target=Get-SafePath $GamePath $row.path
        $before=if(Test-Path -LiteralPath $target){Get-FileHashText $target}else{$null}
        $operations+=[pscustomobject]@{path=$row.path;before=$before;after=$row.sha256;source=$row.source}
    }
    if ($previous) {foreach ($row in $previous.files) {
        if ($row.kind -eq 'own' -and -not $seen.Contains($row.path)) {
            $target=Get-SafePath $GamePath $row.path
            if (Test-Path -LiteralPath $target) {$operations+=[pscustomobject]@{path=$row.path;before=(Get-FileHashText $target);after=$null;source=$null}}
        }
    }}
    foreach ($operation in $operations) {
        if ($operation.before -and $operation.before -ne $operation.after) {
            $backup=Get-SafePath (Join-Path $history 'before') $operation.path
            [IO.Directory]::CreateDirectory((Split-Path -Parent $backup)) | Out-Null
            Copy-Item -LiteralPath (Get-SafePath $GamePath $operation.path) -Destination $backup
            if ((Get-FileHashText $backup) -ne $operation.before) {throw 'Sicherung fehlgeschlagen.'}
        }
    }
    Write-JsonFile (Join-Path $history 'operations.json') $operations
    $completed=@()
    try {
        $doneCount=0
        foreach ($operation in $operations) {
            if(($doneCount % 100) -eq 0){Report-InstallerProgress 'Sprachdateien einrichten und prüfen' (55+[int](35*$doneCount/[Math]::Max(1,$operations.Count)))}
            $doneCount++
            $target=Get-SafePath $GamePath $operation.path
            $completed+= $operation
            if ($operation.after) {
                if ($operation.before -ne $operation.after) {
                    [IO.Directory]::CreateDirectory((Split-Path -Parent $target)) | Out-Null
                    Copy-Item -LiteralPath $operation.source -Destination $target -Force
                }
                if ((Get-FileHashText $target) -ne $operation.after) {throw 'Dateipruefung nach Installation fehlgeschlagen.'}
            } elseif (Test-Path -LiteralPath $target) {Remove-Item -LiteralPath $target}
        }
        Copy-Item -LiteralPath (Join-Path $PackageRoot 'InstallerCore.ps1') -Destination (Join-Path $stateRoot 'InstallerCore.ps1') -Force
        Copy-Item -LiteralPath (Join-Path $PackageRoot 'Setup.ps1') -Destination (Join-Path $stateRoot 'Setup.ps1') -Force
        if(Test-Path -LiteralPath (Join-Path $PackageRoot 'design.json')) {Copy-Item -LiteralPath (Join-Path $PackageRoot 'design.json') -Destination (Join-Path $stateRoot 'design.json') -Force}
        foreach($support in @('InstallerUI.ps1','InstallerExperience.ps1','ExperienceCore.ps1','creator-profile.json','profile.json','Deutsche-Stimmen-Setup.exe')) {
            $supportPath=Join-Path $PackageRoot $support
            if(Test-Path -LiteralPath $supportPath){Copy-Item -LiteralPath $supportPath -Destination (Join-Path $stateRoot $support) -Force}
        }
        if(Test-Path -LiteralPath (Join-Path $PackageRoot 'assets')) {
            [IO.Directory]::CreateDirectory((Join-Path $stateRoot 'assets')) | Out-Null
            foreach($asset in @('background.png','icon.ico','icon.png','keyart.jpg','logo.png','Philosopher-Regular.ttf','Philosopher-Bold.ttf','Philosopher-OFL.txt','CREDITS.txt')){
                $assetPath=Join-Path $PackageRoot ('assets/'+$asset)
                if(Test-Path -LiteralPath $assetPath){Copy-Item -LiteralPath $assetPath -Destination (Join-Path $stateRoot ('assets/'+$asset)) -Force}
            }
        }
        $remove='@echo off'+"`r`n"+'powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File "%~dp0.DrovaVoices-Setup\Setup.ps1" -Action Uninstall -GamePath "%~dp0."'+"`r`n"
        [IO.File]::WriteAllText((Get-SafePath $GamePath 'Drova-Sprachmod-entfernen.cmd'),$remove,[Text.Encoding]::ASCII)
        Write-JsonFile $statePath ([ordered]@{schema=1;version=$manifest.version;routes=$manifest.routes;game=$GamePath;files=@($sources | Select-Object path,sha256,kind);installed_utc=[DateTime]::UtcNow.ToString('o')})
    } catch {
        foreach ($operation in @($completed | Select-Object -Last $completed.Count)) {
            $target=Get-SafePath $GamePath $operation.path
            if ($operation.before -and $operation.before -ne $operation.after) {Copy-Item -LiteralPath (Get-SafePath (Join-Path $history 'before') $operation.path) -Destination $target -Force}
            elseif (-not $operation.before -and (Test-Path -LiteralPath $target)) {Remove-Item -LiteralPath $target}
        }
        throw
    }
    Report-InstallerProgress 'Installation vollständig geprüft' 100
    return "Fertig: $($manifest.routes) Sprachzeilen installiert. Im Spiel: Optionen > Modding > Dialogaudio aktivieren > An."
    } finally {
        $checked=Get-SafePath $tempRoot $stageName
        if ($checked -ne $stageRoot -or $stageName -notmatch '^DrovaV-[a-f0-9]{8}$') {throw 'Unsicherer temporaerer Ordner.'}
        # Verify every resolved descendant before removing this exact private tree.
        foreach ($entry in Get-ChildItem -LiteralPath $stageRoot -Force -Recurse) {
            $relative=$entry.FullName.Substring($stageRoot.Length+1)
            [void](Get-SafePath $stageRoot $relative)
        }
        Remove-Item -LiteralPath $checked -Recurse -Force
    }
}
function Uninstall-Voices([string]$GamePath) {
    Report-InstallerProgress 'Installationsbeleg prüfen' 5
    Assert-GameClosed;Assert-GameFolder $GamePath
    $state=Get-SafePath $GamePath '.DrovaVoices-Setup/installed.json'
    if (-not (Test-Path -LiteralPath $state)) {throw 'Keine mit diesem Setup installierte Sprachmod gefunden.'}
    $receipt=Get-Content -LiteralPath $state -Raw | ConvertFrom-Json
    $own=@($receipt.files | Where-Object {$_.kind -eq 'own'})
    foreach ($row in $own) {
        if ((Get-FileKind $row.path) -ne 'own') {throw 'Ungueltiger Deinstallationseintrag.'}
        $target=Get-SafePath $GamePath $row.path
        if ((Test-Path -LiteralPath $target) -and (Get-FileHashText $target) -ne $row.sha256) {throw "Manuell geaenderte Datei bleibt erhalten: $($row.path). Deinstallation wurde nicht begonnen."}
    }
    Report-InstallerProgress 'Eigene Sprachdateien entfernen' 50
    foreach ($row in $own) {$target=Get-SafePath $GamePath $row.path;if(Test-Path -LiteralPath $target){Remove-Item -LiteralPath $target}}
    Move-Item -LiteralPath $state -Destination ($state+'.removed-'+[guid]::NewGuid().ToString('N'))
    Report-InstallerProgress 'Sprachmod entfernt; gemeinsame Werkzeuge erhalten' 100
    return 'Sprachausgabe entfernt. Spielstaende und andere Mods bleiben erhalten. MelonLoader und Modding API bleiben als gemeinsam genutzte Komponenten installiert.'
}

if(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'ExperienceCore.ps1')){. (Join-Path $PSScriptRoot 'ExperienceCore.ps1')}else{function Report-InstallerProgress([string]$Phase,[int]$Percent){}}
