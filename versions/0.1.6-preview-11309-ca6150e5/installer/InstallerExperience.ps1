# Shared presentation and navigation; transactions remain in the game adapter.
$script:lastCheck='Noch nicht geprüft';$script:lastOperation='Keine';$script:lastError='Kein Fehler'
$script:checkWorker=$null;$script:checkSelection='';$script:checkLog='';$script:checkError='';$script:progressFile=''
$creatorFile=Join-Path $script:PackageRoot 'creator-profile.json'
$script:creator=if(Test-Path -LiteralPath $creatorFile){Get-Content -LiteralPath $creatorFile -Raw -Encoding UTF8|ConvertFrom-Json}else{$null}
$eyebrow.SetBounds(30,84,620,22)
foreach($label in @($content.Controls|Where-Object {$_ -is [Windows.Forms.Label]})){
    if($label.Text -eq $profile.title){$label.SetBounds(28,109,624,47);$label.Font=New-Object Drawing.Font('Georgia',24)}
    if($label.Text -eq $description){$label.SetBounds(30,158,620,38)}
}
$line.SetBounds(30,204,622,1);$line.BackColor=$colors.accent
$tutorial.Text='Hilfe zur Einrichtung';$tutorial.SetBounds(482,30,170,34)
$about=Add-Button 'Über & Unterstützen' 292 30 178 $false 34
$brand.Visible=$false
foreach($label in @($hero.Controls|Where-Object {$_ -is [Windows.Forms.Label]})){
    if($label.Text -like 'Eine Welt.*'){$label.Text="DEUTSCHE`r`nSTIMMEN";$label.SetBounds(28,282,282,110);$label.Font=New-Object Drawing.Font('Georgia',25,[Drawing.FontStyle]::Bold)}
    if($label.Text -eq $profile.tagline){$label.SetBounds(30,433,278,80)}
}
$gameLogo=Add-Label $hero $(if($profile.PSObject.Properties['hero_title']){$profile.hero_title}else{$profile.title}) 24 88 290 120 38 $true
$gameLogo.Font=New-Object Drawing.Font('Georgia',38,[Drawing.FontStyle]::Bold);$gameLogo.TextAlign='MiddleCenter';$gameLogo.ForeColor=$colors.text
# The game's own typography is embedded privately; no system font installation.
$script:displayFonts=$null
if($profile.PSObject.Properties['display_font_files']){
    $script:displayFonts=New-Object Drawing.Text.PrivateFontCollection
    foreach($fontFile in $profile.display_font_files){$fontPath=Join-Path $script:PackageRoot $fontFile;if(Test-Path -LiteralPath $fontPath){$script:displayFonts.AddFontFile($fontPath)}}
    if($script:displayFonts.Families.Count){
        $displayFamily=$script:displayFonts.Families[0]
        foreach($label in @($content.Controls|Where-Object {$_ -is [Windows.Forms.Label] -and $_.Text -eq $profile.title})){$label.UseCompatibleTextRendering=$true;$label.Font=New-Object Drawing.Font($displayFamily,26,[Drawing.FontStyle]::Regular)}
        foreach($label in @($hero.Controls|Where-Object {$_ -is [Windows.Forms.Label]})){
            $label.UseCompatibleTextRendering=$true
            if($label.Text -like 'DEUTSCHE*STIMMEN'){$label.Font=New-Object Drawing.Font($displayFamily,26,[Drawing.FontStyle]::Bold);$label.SetBounds(28,296,282,100)}
            if($label.Text -eq $profile.tagline){$label.Font=New-Object Drawing.Font($displayFamily,12);$label.SetBounds(30,440,278,74)}
        }
    }
}
if($profile.PSObject.Properties['hero_artwork']){
    # Official portrait artwork already contains the original Drova wordmark.
    $gameLogo.Visible=$false;$hero.BackgroundImage=$null
    if($script:art){$script:art.Dispose()}
    $script:art=[Drawing.Image]::FromFile((Join-Path $script:PackageRoot $profile.hero_artwork))
    $hero.BackColor=[Drawing.Color]::FromArgb(15,20,21)
    $hero.Add_Paint({param($sender,$eventArgs)
        $g=$eventArgs.Graphics;$height=[int]($script:art.Height*$sender.Width/$script:art.Width)
        $g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.DrawImage($script:art,(New-Object Drawing.Rectangle(0,0,$sender.Width,$height)))
        $fade=New-Object Drawing.Drawing2D.LinearGradientBrush((New-Object Drawing.Point(0,225)),(New-Object Drawing.Point(0,$height)),[Drawing.Color]::FromArgb(0,15,20,21),[Drawing.Color]::FromArgb(255,15,20,21))
        $g.FillRectangle($fade,0,225,$sender.Width,($height-225));$fade.Dispose()
    })
}
$count=Add-Label $hero $(if($package){[string]$package.routes+' SPRACHZEILEN'}else{'DEUTSCHE SPRACHAUSGABE'}) 30 614 278 28 9 $true
$count.ForeColor=$colors.accent;$count.TextAlign='MiddleCenter'
$edition.Text='Inoffizielle deutsche KI-Fanvertonung';$edition.TextAlign='MiddleCenter'

$pages=New-Object Windows.Forms.TabControl;$pages.SetBounds(30,220,622,442);$pages.DrawMode='OwnerDrawFixed';$pages.ItemSize=New-Object Drawing.Size(202,28);$pages.SizeMode='Fixed'
$pages.Add_DrawItem({param($sender,$eventArgs)
    $selected=$eventArgs.Index -eq $sender.SelectedIndex
    $brush=New-Object Drawing.SolidBrush($(if($selected){$colors.panel}else{$colors.background}));$eventArgs.Graphics.FillRectangle($brush,$eventArgs.Bounds);$brush.Dispose()
    [Windows.Forms.TextRenderer]::DrawText($eventArgs.Graphics,$sender.TabPages[$eventArgs.Index].Text,$form.Font,$eventArgs.Bounds,$(if($selected){$colors.accent}else{$colors.muted}),([Windows.Forms.TextFormatFlags]::HorizontalCenter -bor [Windows.Forms.TextFormatFlags]::VerticalCenter -bor [Windows.Forms.TextFormatFlags]::NoPrefix))
})
$setupPage=New-Object Windows.Forms.TabPage 'Installation';$helpPage=New-Object Windows.Forms.TabPage 'Hilfe & Inhalt';$supportPage=New-Object Windows.Forms.TabPage 'Support & Updates'
foreach($page in @($setupPage,$helpPage,$supportPage)){$page.BackColor=$colors.background;$page.ForeColor=$colors.text;[void]$pages.TabPages.Add($page)}
$content.Controls.Add($pages)
foreach($control in @($content.Controls)){
    if($control -ne $pages -and $control.Top -ge 180 -and $control.Top -lt 660 -and $control -ne $line){$content.Controls.Remove($control);$setupPage.Controls.Add($control)}
}
foreach($label in @($setupPage.Controls|Where-Object {$_ -is [Windows.Forms.Label]})){
    if($label.Text -eq '01   Spielordner'){$label.Text='1  Spielordner';$label.SetBounds(8,6,590,24)}
    elseif($label.Text -eq '02   Vorbereitung'){$label.Text='2  MelonLoader + Modding API';$label.SetBounds(8,148,590,24)}
    elseif($label.Text -eq $profile.folder_hint){$label.Visible=$false}
}
$folder.SetBounds(8,36,438,28);$choose.SetBounds(454,34,150,32)
$statusLabel.SetBounds(8,70,438,25);$statusLabel.Font=New-Object Drawing.Font($design.font_family,9)
$check=Add-Button 'Status prüfen' 0 0 150 $false 30;$content.Controls.Remove($check);$setupPage.Controls.Add($check);$check.SetBounds(454,70,150,30)
$requirements=Add-Label $setupPage "○ Spiel und Datenversion noch nicht geprüft`r`n○ Werkzeuge noch nicht geprüft" 8 102 596 45 8.5 $false $true
$dependencyPanel.SetBounds(8,176,596,43)
foreach($button in $script:dependencyButtons){$button.Width=282;$button.Font=New-Object Drawing.Font($design.font_family,9)}
$help.SetBounds(8,225,282,30)
$zipHintText=if($profile.PSObject.Properties['download_hint']){$profile.download_hint}else{'Original-ZIPs nicht entpacken. Setup richtet die Werkzeuge selbst ein.'}
$zipHint=Add-Label $setupPage $zipHintText 8 262 596 23 9 $false $true
$install.SetBounds(8,294,270,40);$uninstall.SetBounds(286,294,166,40)
$content.Controls.Remove($close);$setupPage.Controls.Add($close);$close.SetBounds(460,294,144,40)
$progressStep=Add-Label $setupPage 'Bereit · Voraussetzungen werden vor Änderungen erneut geprüft.' 8 334 596 22 9 $false $true
$progress.SetBounds(8,358,596,10);$status.SetBounds(8,374,596,30);$status.Font=New-Object Drawing.Font($design.font_family,9)
if(-not $package -or @($package.dependencies).Count -eq 0){$help.Visible=$false;$dependencyPanel.Visible=$false;$zipHint.Visible=$false;foreach($label in $setupPage.Controls){if($label.Text -eq '2  MelonLoader + Modding API'){$label.Visible=$false}}}

$helpBody=New-Object Windows.Forms.RichTextBox;$helpBody.Dock='Fill';$helpBody.ReadOnly=$true;$helpBody.BorderStyle='None';$helpBody.BackColor=$colors.background;$helpBody.ForeColor=$colors.text;$helpBody.Font=$form.Font
$helpBody.Text=($profile.tutorial|ForEach-Object {$_.title.ToUpper()+"`r`n`r`n"+$_.text}) -join "`r`n`r`n────────────────────`r`n`r`n";$helpPage.Padding=New-Object Windows.Forms.Padding(14);$helpPage.Controls.Add($helpBody)
function Move-SupportButton($Button,$X,$Y,$Width){$Button.Parent=$supportPage;$Button.SetBounds($X,$Y,$Width,34)}
$diagnose=Add-Button 'Diagnose kopieren' 0 0 280;$news=Add-Button 'Was ist neu?' 0 0 280;$problem=Add-Button 'Problem melden' 0 0 280
Move-SupportButton $diagnose 12 18 282;Move-SupportButton $news 308 18 282;Move-SupportButton $update 12 132 282;Move-SupportButton $problem 308 132 282
$update.Text='Modseite & Updates'
[void](Add-Label $supportPage "Version, Prüfstatus und letzter Vorgang – persönliche Pfade ausgeblendet.`r`nEs wird nichts automatisch übertragen." 12 66 578 48 9 $false $true)
$script:modLinkValid=$profile.mod_page -match '^https://www\.nexusmods\.com/[a-z0-9_-]+/mods/[0-9]+$'
$problem.Enabled=$script:modLinkValid
$unpublished=Add-Label $supportPage 'Die Nexus-Modseite ist noch nicht veröffentlicht. Modseiten- und Fehlerlinks bleiben bis dahin deaktiviert.' 12 180 578 54 10 $false $true
$unpublished.Visible= -not $script:modLinkValid
[void](Add-Label $supportPage "$($profile.next_steps)`r`nWeitere Schritte findest du unter Hilfe & Inhalt." 12 264 578 74 10 $false $true)

function Show-InstallerText([string]$Title,[string]$Text,[bool]$Author=$false){
    $script:lastDialogTitle=$Title;$script:lastDialogText=$Text
    $dialog=New-Object Windows.Forms.Form;$dialog.Text=$Title;$dialog.ClientSize=New-Object Drawing.Size(680,550);$dialog.StartPosition='CenterParent';$dialog.BackColor=$colors.background;$dialog.ForeColor=$colors.text;$dialog.Font=$form.Font
    $body=New-Object Windows.Forms.RichTextBox;$body.SetBounds(24,24,632,390);$body.ReadOnly=$true;$body.BorderStyle='None';$body.BackColor=$colors.background;$body.ForeColor=$colors.text;$body.Font=$form.Font;$body.Text=$Text;$dialog.Controls.Add($body)
    if($Author -and $script:creator){
        $nexus=New-Object StimmenButton;$nexus.Text='Mein Nexus-Profil';$nexus.SetBounds(24,425,300,36)
        $kofi=New-Object StimmenButton;$kofi.Text='Auf Ko-fi unterstützen';$kofi.SetBounds(336,425,320,36)
        foreach($button in @($nexus,$kofi)){$button.BackColor=$colors.secondary;$button.ForeColor=$colors.text;$button.FlatAppearance.MouseOverBackColor=$colors.secondary_hover;$dialog.Controls.Add($button)}
        $nexus.Add_Click({Open-SafeWebPage $script:creator.nexus_profile_url});$kofi.Add_Click({Open-SafeWebPage $script:creator.kofi_url})
    }
    $done=New-Object StimmenButton;$done.Text='Schließen';$done.SetBounds(496,482,160,36);$done.BackColor=$colors.accent;$done.ForeColor=$colors.accent_text;$done.FlatAppearance.MouseOverBackColor=$colors.accent_hover;$done.DialogResult='OK';$dialog.Controls.Add($done);$dialog.CancelButton=$done
    if(-not $NoUI){[void]$dialog.ShowDialog($form)};$dialog.Dispose()
}
$about.Add_Click({Show-InstallerText 'Über & Unterstützen' ($script:creator.display_name+" – Deutsche Stimmen`r`nInoffizielle KI-Fanvertonungen für Spiele.`r`n`r`n"+$script:creator.support_message) $true})
$news.Add_Click({$changes=if($package -and $package.PSObject.Properties['changes']){$package.changes -join "`r`n`r`n"}else{'Keine Änderungsliste hinterlegt.'};Show-InstallerText 'Was ist neu?' $changes})
$diagnose.Add_Click({$text="Paket: $version`r`nStatus: $($statusLabel.Text)`r`nPrüfung: $script:lastCheck`r`nLetzter Vorgang: $script:lastOperation`r`nFehler: $script:lastError";[Windows.Forms.Clipboard]::SetText((Hide-PrivatePaths $text));$status.Text='Diagnose kopiert. Es wurde nichts versendet.'})
$problem.Add_Click({if($script:modLinkValid){Open-SafeWebPage ($profile.mod_page+'?tab=bugs')}})

$checkTimer=New-Object Windows.Forms.Timer;$checkTimer.Interval=250
function Format-InstallerChecks($Checks){
    if($Checks.Count -ge 2){return ($Checks[0]+'   '+$Checks[1]+"`r`n"+(@($Checks|Select-Object -Skip 2) -join "`r`n"))}
    return $Checks -join "`r`n"
}
function Start-InstallerCheck {
    if($script:checkWorker -or $script:worker){return}
    if(-not $folder.Text){$statusLabel.Text='Spielordner auswählen';return}
    $script:DependencyArchives=$script:selectedZips
    if($NoUI){$state=Get-InstallerState $folder.Text;$statusLabel.Text=$state.installed;$requirements.Text=Format-InstallerChecks $state.checks;$script:lastCheck=$requirements.Text;return}
    $script:checkLog=[IO.Path]::GetTempFileName();$script:checkError=[IO.Path]::GetTempFileName()
    $script:checkSelection=[IO.Path]::GetTempFileName();$script:selectedZips|ConvertTo-Json|Set-Content -LiteralPath $script:checkSelection -Encoding UTF8
    $oldCheckSelection=$env:DEUTSCHE_STIMMEN_ZIP_SELECTION;$env:DEUTSCHE_STIMMEN_ZIP_SELECTION=$script:checkSelection
    try{$script:checkWorker=Start-Process powershell.exe -WindowStyle Hidden -PassThru -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-STA','-File',('"'+$script:EntryPath+'"'),'-NoUI','-Action','Check','-GamePath',('"'+$folder.Text+'"')) -RedirectStandardOutput $script:checkLog -RedirectStandardError $script:checkError}finally{$env:DEUTSCHE_STIMMEN_ZIP_SELECTION=$oldCheckSelection}
    [void]$script:checkWorker.Handle;$check.Enabled=$false;$install.Enabled=$false;$uninstall.Enabled=$false;$folder.ReadOnly=$true;$choose.Enabled=$false;$statusLabel.Text='Installationsstatus wird geprüft …';$checkTimer.Start()
}
$check.Add_Click({Start-InstallerCheck})
$folder.Add_TextChanged({if(-not $script:worker -and -not $script:checkWorker){$requirements.Text='○ Neuer Ordner – bitte Status prüfen';$script:lastCheck='Neuer Ordner, noch nicht geprüft'}})
$checkTimer.Add_Tick({if($script:checkWorker -and $script:checkWorker.HasExited){
    $checkTimer.Stop();$script:checkWorker.WaitForExit();$code=$script:checkWorker.ExitCode
    try{if($code -ne 0){throw (Get-Content -LiteralPath $script:checkError -Raw)};$state=Get-Content -LiteralPath $script:checkLog -Raw -Encoding UTF8|ConvertFrom-Json;Update-Availability;$statusLabel.Text=$state.installed;$requirements.Text=Format-InstallerChecks $state.checks;$script:lastCheck=$requirements.Text}catch{$statusLabel.Text='Prüfung nicht abgeschlossen';$script:lastError=$_.Exception.Message}
    $script:checkWorker.Dispose();$script:checkWorker=$null;$check.Enabled=$true;$folder.ReadOnly=$false;$choose.Enabled=$true
    foreach($path in @($script:checkLog,$script:checkError,$script:checkSelection)){Remove-Item -LiteralPath $path -ErrorAction SilentlyContinue}
}})
$timer.Add_Tick({
    $problem.Enabled=$script:modLinkValid -and -not $script:worker
    if($script:progressFile -and (Test-Path -LiteralPath $script:progressFile)){try{$phase=Get-Content -LiteralPath $script:progressFile -Raw -Encoding UTF8|ConvertFrom-Json;$progress.Style='Continuous';$progress.Value=[Math]::Max(0,[Math]::Min(100,[int]$phase.percent));$progressStep.Text=$phase.phase}catch{}}
    if(-not $script:worker -and $script:workerExit -eq 0){
        if($script:lastOperation -eq 'Install'){$status.Text=("So geht's weiter: "+$profile.next_steps)}
        Start-InstallerCheck
        if($script:progressFile){Remove-Item -LiteralPath $script:progressFile -ErrorAction SilentlyContinue;$script:progressFile=''}
    }
})
$form.Add_Shown({Start-InstallerCheck})
$form.Add_FormClosed({if($script:checkWorker){$script:checkWorker.WaitForExit();$script:checkWorker.Dispose()};$checkTimer.Dispose()})
$script:actionControls+=@($check,$about,$diagnose,$news,$problem)
if($NoUI){Start-InstallerCheck}
