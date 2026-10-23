[Console]::OutputEncoding=New-Object System.Text.UTF8Encoding($false)
# Shared Windows interface. Game-specific validation and transactions live in InstallerCore.ps1.
$profile=Get-Content -LiteralPath (Join-Path $script:PackageRoot 'profile.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$packageFile=Join-Path $script:PackageRoot 'Paket.json';$package=$null
if(Test-Path -LiteralPath $packageFile){$package=Get-Content -LiteralPath $packageFile -Raw -Encoding UTF8 | ConvertFrom-Json}
if($package){foreach($dep in $package.dependencies){
    if($dep.url -match 'LavaGang' -and $LoaderZip){$script:DependencyArchives[$dep.sha256]=$LoaderZip}
    if($dep.url -match 'Drova-Modding' -and $ApiZip){$script:DependencyArchives[$dep.sha256]=$ApiZip}
}}
if($env:DEUTSCHE_STIMMEN_ZIP_SELECTION -and (Test-Path -LiteralPath $env:DEUTSCHE_STIMMEN_ZIP_SELECTION)){
    $selection=Get-Content -LiteralPath $env:DEUTSCHE_STIMMEN_ZIP_SELECTION -Raw -Encoding UTF8 | ConvertFrom-Json
    if($selection){foreach($p in $selection.PSObject.Properties){$script:DependencyArchives[$p.Name]=[string]$p.Value}}
}
. (Join-Path $script:PackageRoot 'ExperienceCore.ps1')
if($Action -eq 'Check' -and $NoUI){Get-InstallerState $GamePath|ConvertTo-Json -Depth 5;exit 0}
if($Action -ne 'Gui' -and $NoUI){
    try{if($Action -eq 'Install'){Install-Voices $script:PackageRoot $GamePath}else{Uninstall-Voices $GamePath};exit 0}
    catch{[Console]::Error.WriteLine($_.Exception.Message);exit 1}
}
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -WarningAction SilentlyContinue -ReferencedAssemblies System.Windows.Forms,System.Drawing -TypeDefinition @'
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
public class StimmenButton : Button {
    protected override void OnPaint(PaintEventArgs e) {
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        Color fill=Enabled?BackColor:Color.FromArgb(34,39,43);
        if(Enabled && ClientRectangle.Contains(PointToClient(Cursor.Position))) fill=FlatAppearance.MouseOverBackColor;
        using(var brush=new SolidBrush(fill)) using(var path=new GraphicsPath()) {
            float r=8,w=Width-1,h=Height-1;
            path.AddArc(0,0,r,r,180,90);path.AddArc(w-r,0,r,r,270,90);
            path.AddArc(w-r,h-r,r,r,0,90);path.AddArc(0,h-r,r,r,90,90);path.CloseFigure();g.FillPath(brush,path);
            if(Focused && ShowFocusCues) using(var pen=new Pen(Color.FromArgb(209,173,105),2))g.DrawPath(pen,path);
        }
        TextRenderer.DrawText(g,Text,Font,ClientRectangle,Enabled?ForeColor:Color.FromArgb(137,145,149),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
    }
}
'@
[Windows.Forms.Application]::EnableVisualStyles()
$design=Get-Content -LiteralPath (Join-Path $script:PackageRoot 'design.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$colors=@{};foreach($entry in $design.colors.PSObject.Properties){$colors[$entry.Name]=[Drawing.ColorTranslator]::FromHtml($entry.Value)}
$form=New-Object Windows.Forms.Form
$form.Text=if($profile.title -eq 'Deutsche Stimmen'){'Deutsche Stimmen – Installer'}else{$profile.title+' – Deutsche Stimmen'};$form.ClientSize=New-Object Drawing.Size(1020,720)
$form.AutoScaleDimensions=New-Object Drawing.SizeF(96,96);$form.AutoScaleMode='Dpi'
$form.StartPosition='CenterScreen';$form.FormBorderStyle='FixedSingle';$form.MaximizeBox=$false
$form.Font=New-Object Drawing.Font($design.font_family,10);$form.BackColor=$colors.background;$form.ForeColor=$colors.text
$script:art=$null;$script:windowIcon=$null
$artPath=Join-Path $script:PackageRoot $profile.background
if(Test-Path -LiteralPath $artPath){$script:art=[Drawing.Image]::FromFile($artPath)}
$iconPath=Join-Path $script:PackageRoot $profile.icon
if(Test-Path -LiteralPath $iconPath){$script:windowIcon=New-Object Drawing.Icon($iconPath);$form.Icon=$script:windowIcon}
$hero=New-Object Windows.Forms.Panel;$hero.SetBounds(0,0,338,720);$hero.BackColor=$colors.panel
if($script:art){$hero.BackgroundImage=$script:art;$hero.BackgroundImageLayout='Stretch'};$form.Controls.Add($hero)
$content=New-Object Windows.Forms.Panel;$content.SetBounds(338,0,682,720);$content.BackColor=$colors.background;$form.Controls.Add($content)
function Add-Label($parent,$text,$x,$y,$width,$height,$size=10,$bold=$false,$muted=$false){
    $c=New-Object Windows.Forms.Label;$c.Text=$text;$c.SetBounds($x,$y,$width,$height);$c.BackColor=[Drawing.Color]::Transparent
    $weight=if($bold){[Drawing.FontStyle]::Bold}else{[Drawing.FontStyle]::Regular}
    $c.Font=New-Object Drawing.Font($design.font_family,$size,$weight);$c.ForeColor=if($muted){$colors.muted}else{$colors.text}
    $parent.Controls.Add($c);return $c
}
function Add-Button($text,$x,$y,$width,$primary=$false,$height=40){
    $c=New-Object StimmenButton;$c.Text=$text;$c.SetBounds($x,$y,$width,$height);$c.FlatStyle='Flat';$c.FlatAppearance.BorderSize=0;$c.Cursor='Hand'
    $c.Font=New-Object Drawing.Font($design.font_family,10,[Drawing.FontStyle]::Bold)
    if($primary){$c.BackColor=$colors.accent;$c.ForeColor=$colors.accent_text;$c.FlatAppearance.MouseOverBackColor=$colors.accent_hover}
    else{$c.BackColor=$colors.secondary;$c.ForeColor=$colors.text;$c.FlatAppearance.MouseOverBackColor=$colors.secondary_hover}
    $content.Controls.Add($c);return $c
}
$brand=Add-Label $hero $design.family_label 28 30 285 24 10 $true;$brand.ForeColor=$colors.accent
[void](Add-Label $hero 'Eine Welt.
Ihre Stimmen.' 28 462 290 104 29 $true)
[void](Add-Label $hero $profile.tagline 30 580 278 64 11 $false $true)
$edition=Add-Label $hero 'INOFFIZIELLE SPRACHMOD · WINDOWS' 30 671 286 28 8 $true;$edition.ForeColor=$colors.accent
$eyebrow=Add-Label $content 'DEUTSCHE SPRACHAUSGABE' 30 26 620 22 9 $true;$eyebrow.ForeColor=$colors.accent
[void](Add-Label $content $profile.title 28 53 624 47 25 $true)
$version=if($package){$package.version}else{'Installierte Ausgabe'}
$description=if($package){$profile.description.Replace('{routes}',[string]$package.routes)}else{'Installierte Sprachausgabe verwalten oder entfernen.'}
[void](Add-Label $content $description 30 105 620 40 10 $false $true)
$line=New-Object Windows.Forms.Panel;$line.SetBounds(30,158,622,1);$line.BackColor=$colors.border;$content.Controls.Add($line)
[void](Add-Label $content '01   Spielordner' 30 180 620 27 12 $true)
$folder=New-Object Windows.Forms.TextBox;$folder.SetBounds(30,216,466,30);$folder.BackColor=$colors.panel;$folder.ForeColor=$colors.text;$folder.BorderStyle='FixedSingle';$folder.Text=$GamePath;$content.Controls.Add($folder)
$choose=Add-Button 'Auswählen …' 508 212 144 $false 36
[void](Add-Label $content $profile.folder_hint 30 256 620 32 9 $false $true)
[void](Add-Label $content '02   Vorbereitung' 30 302 620 27 12 $true)
$script:dependencyButtons=@();$script:selectedZips=@{};foreach($key in $script:DependencyArchives.Keys){$script:selectedZips[$key]=$script:DependencyArchives[$key]};$script:worker=$null;$script:workerExit=$null;$script:outLog='';$script:errLog=''
$dependencyPanel=New-Object Windows.Forms.FlowLayoutPanel;$dependencyPanel.SetBounds(30,341,622,44);$dependencyPanel.WrapContents=$false;$dependencyPanel.AutoScroll=$true;$content.Controls.Add($dependencyPanel)
if($package){foreach($dep in $package.dependencies){
    $b=New-Object StimmenButton;$b.Text=$dep.name+' · ZIP wählen';$b.Width=if($package.dependencies.Count -gt 1){300}else{622};$b.Height=38;$b.Margin=New-Object Windows.Forms.Padding(0,0,10,0)
    $b.FlatStyle='Flat';$b.FlatAppearance.BorderSize=0;$b.BackColor=$colors.secondary;$b.ForeColor=$colors.text;$b.FlatAppearance.MouseOverBackColor=$colors.secondary_hover;$b.Cursor='Hand';$b.Tag=$dep
    $b.Add_Click({param($sender,$eventArgs)
        $dependency=$sender.Tag;$dialog=New-Object Windows.Forms.OpenFileDialog;$dialog.Title=$dependency.name+' – offizielle ZIP auswählen';$dialog.Filter='ZIP-Archiv (*.zip)|*.zip'
        if($dialog.ShowDialog() -eq 'OK'){
            if((Get-FileHashText $dialog.FileName) -ne $dependency.sha256){$status.ForeColor=$colors.error;$status.Text='Diese ZIP passt nicht zur benötigten Version. Bitte die verlinkte Originalversion verwenden.'}
            else{$script:selectedZips[$dependency.sha256]=$dialog.FileName;$sender.Text=$dependency.name+' · bereit';$status.ForeColor=$colors.success;$status.Text='Die ausgewählte ZIP wurde erfolgreich geprüft.'}
        };$dialog.Dispose()
    });$dependencyPanel.Controls.Add($b);$script:dependencyButtons+= $b
}}
if(-not $package -or @($package.dependencies).Count -eq 0){[void](Add-Label $dependencyPanel 'Keine zusätzlichen Werkzeug-ZIPs erforderlich.' 0 4 615 32 10 $false $true)}
$help=Add-Button 'Werkzeug-Downloads öffnen ↗' 30 394 304 $false 34
$update=Add-Button 'Modseite / Updates öffnen ↗' 346 394 306 $false 34
$install=Add-Button 'Installieren / Aktualisieren' 30 454 352 $true 48
$uninstall=Add-Button 'Deinstallieren' 394 454 258 $false 48
$statusLabel=Add-Label $content 'BEREIT' 30 525 620 24 9 $true;$statusLabel.ForeColor=$colors.accent
$progress=New-Object Windows.Forms.ProgressBar;$progress.SetBounds(30,554,622,4);$progress.Style='Continuous';$content.Controls.Add($progress)
$status=New-Object Windows.Forms.TextBox;$status.SetBounds(30,572,622,68);$status.Multiline=$true;$status.ReadOnly=$true;$status.ScrollBars='Vertical';$status.BackColor=$colors.background;$status.ForeColor=$colors.muted;$status.BorderStyle='None';$status.Text=$profile.ready_hint;$content.Controls.Add($status)
$footer=Add-Label $content ('Version '+$version) 30 672 483 32 8 $false $true
$close=Add-Button 'Schließen' 526 663 126 $false 34
$tutorial=Add-Button 'Tutorial / Hilfe' 512 22 140 $false 30
$script:tutorialResult=$null
$script:actionControls=@($install,$uninstall,$choose,$help,$update,$close,$tutorial)+$script:dependencyButtons
function Get-InstalledVersion {
    try{
        if(-not $folder.Text){return 'Noch kein Spielordner ausgewählt.'}
        $state=Get-SafePath $folder.Text ($profile.state_directory+'/installed.json')
        if(Test-Path -LiteralPath $state){$receipt=Get-Content -LiteralPath $state -Raw -Encoding UTF8 | ConvertFrom-Json;return 'Installiert: '+$receipt.version}
        return 'Sprachmod noch nicht mit diesem Installer eingerichtet.'
    }catch{return 'Spielordner bitte überprüfen.'}
}
function Update-Availability {
    $install.Enabled=($null -ne $package);$uninstall.Enabled=$false
    if($folder.Text){try{$state=Get-SafePath $folder.Text ($profile.state_directory+'/installed.json');$uninstall.Enabled=Test-Path -LiteralPath $state}catch{}}
    foreach($b in $script:dependencyButtons){$b.Enabled=($null -ne $package)}
    $help.Enabled=($profile.download_pages.Count -gt 0);$update.Enabled= -not [string]::IsNullOrWhiteSpace($profile.mod_page)
    $statusLabel.Text=Get-InstalledVersion
}
function Open-SafeWebPage([string]$url){
    $uri=$null;if(-not [Uri]::TryCreate($url,[UriKind]::Absolute,[ref]$uri) -or $uri.Scheme -ne 'https'){throw 'Nur sichere HTTPS-Downloadseiten sind erlaubt.'}
    Start-Process $uri.AbsoluteUri
}
$close.Add_Click({$form.Close()})
$showTutorial={
    $guide=New-Object Windows.Forms.Form;$guide.Text=$profile.title+' – Tutorial';$guide.ClientSize=New-Object Drawing.Size(760,610)
    $guide.StartPosition='CenterParent';$guide.MinimizeBox=$false;$guide.MaximizeBox=$false;$guide.FormBorderStyle='FixedDialog'
    $guide.Font=$form.Font;$guide.BackColor=$colors.background;$guide.ForeColor=$colors.text
    $tabs=New-Object Windows.Forms.TabControl;$tabs.SetBounds(20,20,720,520);$guide.Controls.Add($tabs)
    $chapters=if($profile.PSObject.Properties['tutorial']){@($profile.tutorial)}else{@([pscustomobject]@{title='Anleitung';text='Spiel schließen, passenden Spielordner auswählen und Installieren / Aktualisieren anklicken. Zusätzliche Anforderungen stehen in der beiliegenden LIESMICH.txt.'})}
    foreach($chapter in $chapters){
        $page=New-Object Windows.Forms.TabPage;$page.Text=$chapter.title;$page.BackColor=$colors.panel
        $body=New-Object Windows.Forms.RichTextBox;$body.Dock='Fill';$body.ReadOnly=$true;$body.BorderStyle='None';$body.BackColor=$colors.panel;$body.ForeColor=$colors.text
        $body.Font=New-Object Drawing.Font($design.font_family,11);$body.Text=$chapter.text;$body.DetectUrls=$true;$body.ScrollBars='Vertical';$body.WordWrap=$true
        $body.Add_LinkClicked({param($sender,$eventArgs) try{Open-SafeWebPage $eventArgs.LinkText}catch{[void][Windows.Forms.MessageBox]::Show($_.Exception.Message,'Link öffnen')}})
        $page.Padding=New-Object Windows.Forms.Padding(16);$page.Controls.Add($body);$tabs.TabPages.Add($page)
    }
    $done=New-Object StimmenButton;$done.Text='Verstanden';$done.SetBounds(580,555,160,36);$done.FlatStyle='Flat';$done.BackColor=$colors.accent;$done.ForeColor=$colors.accent_text;$done.FlatAppearance.MouseOverBackColor=$colors.accent_hover
    $done.DialogResult='OK';$guide.Controls.Add($done);$guide.AcceptButton=$done;$guide.CancelButton=$done
    if($NoUI){$script:tutorialResult=[pscustomobject]@{pages=$tabs.TabPages.Count;titles=@($chapters | ForEach-Object {$_.title});text=($chapters.text -join "`n")}}
    else{[void]$guide.ShowDialog($form)}
    $guide.Dispose()
}
$tutorial.Add_Click($showTutorial)
$choose.Add_Click({$dialog=New-Object Windows.Forms.FolderBrowserDialog;$dialog.Description=$profile.folder_hint;$dialog.ShowNewFolderButton=$false;if($dialog.ShowDialog() -eq 'OK'){$folder.Text=$dialog.SelectedPath};$dialog.Dispose()})
$folder.Add_TextChanged({if(-not $script:worker){Update-Availability}})
$help.Add_Click({try{if($profile.PSObject.Properties['download_guide']){Show-InstallerText 'Werkzeug-Downloads: Welche Dateien?' ($profile.download_guide+"`r`n`r`nNach dem Schließen dieser Anleitung öffnen sich die beiden offiziellen Downloadseiten.")}
foreach($url in $profile.download_pages){Open-SafeWebPage $url};$status.Text=if($profile.PSObject.Properties['download_hint']){$profile.download_hint}else{'Original-ZIPs auf den geöffneten Seiten herunterladen und anschließend hier auswählen.'}}catch{$status.Text=$_.Exception.Message}})
$update.Add_Click({try{Open-SafeWebPage $profile.mod_page;$status.Text='Die Modseite wurde im Browser geöffnet. Dort aktuelle Version laden und mit diesem Installer aktualisieren.'}catch{$status.Text=$_.Exception.Message}})
if(-not $folder.Text){$games=@(Get-SteamGames);if($games.Count -eq 1){$folder.Text=$games[0]}elseif($games.Count -gt 1){$status.Text='Mehrere Installationen gefunden. Bitte den gewünschten Spielordner auswählen.'}}
Update-Availability
$timer=New-Object Windows.Forms.Timer;$timer.Interval=200
$execute={param($operation)
    try{
        if($script:checkWorker){throw 'Bitte die laufende Statusprüfung abwarten.'}
        Assert-GameFolder $folder.Text;Assert-GameClosed
        $script:lastOperation=$operation;$script:lastError='Kein Fehler'
        $script:progressFile=[IO.Path]::GetTempFileName()
        $argsList=@('-NoProfile','-ExecutionPolicy','Bypass','-STA','-File',('"'+$script:EntryPath+'"'),'-NoUI','-Action',$operation,'-GamePath',('"'+$folder.Text+'"'))
        # The receipt contains paths only; it is local, unique and removed after the worker exits.
        $script:selectionFile=[IO.Path]::GetTempFileName()
        $script:selectedZips | ConvertTo-Json | Set-Content -LiteralPath $script:selectionFile -Encoding UTF8
        $script:outLog=[IO.Path]::GetTempFileName();$script:errLog=[IO.Path]::GetTempFileName()
        # Redirect to files through the existing reliable PowerShell child path; no shell interpolation of paths.
        $oldProgress=$env:DEUTSCHE_STIMMEN_PROGRESS;$env:DEUTSCHE_STIMMEN_PROGRESS=$script:progressFile
        $oldSelection=$env:DEUTSCHE_STIMMEN_ZIP_SELECTION;$env:DEUTSCHE_STIMMEN_ZIP_SELECTION=$script:selectionFile
        try{$script:worker=Start-Process powershell.exe -WindowStyle Hidden -PassThru -ArgumentList $argsList -RedirectStandardOutput $script:outLog -RedirectStandardError $script:errLog}finally{$env:DEUTSCHE_STIMMEN_ZIP_SELECTION=$oldSelection;$env:DEUTSCHE_STIMMEN_PROGRESS=$oldProgress}
        [void]$script:worker.Handle
        foreach($c in $script:actionControls){$c.Enabled=$false};$folder.ReadOnly=$true
        $status.ForeColor=$colors.text;$status.Text='Dateien werden geprüft und eingerichtet. Bitte warten.';$statusLabel.Text='WIRD AKTUALISIERT';$progress.Style='Continuous';$progress.Value=0;$timer.Start()
    }catch{$status.ForeColor=$colors.error;$status.Text=$_.Exception.Message;$statusLabel.Text='BITTE PRÜFEN'}
}
$timer.Add_Tick({
    if($script:worker -and $script:worker.HasExited){
        $timer.Stop();$script:worker.WaitForExit();$code=$script:worker.ExitCode;$script:workerExit=$code
        $message=Get-Content -LiteralPath $script:outLog -Raw -ErrorAction SilentlyContinue;$errorText=Get-Content -LiteralPath $script:errLog -Raw -ErrorAction SilentlyContinue
        $progress.Style='Continuous';$progress.Value=if($code -eq 0){100}else{0}
        if($code -eq 0){$status.ForeColor=$colors.success;$status.Text=$message}else{
            $script:lastError=$errorText;$status.ForeColor=$colors.error;$status.Text=$errorText
            if($errorText -match 'UnauthorizedAccessException|Zugriff.*verweigert'){
                if([Windows.Forms.MessageBox]::Show('Setup für diesen Spielordner als Administrator neu öffnen?','Ordnerzugriff','YesNo','Question') -eq 'Yes'){
                    $elevated=@('-NoProfile','-ExecutionPolicy','Bypass','-STA','-File',('"'+$script:EntryPath+'"'),'-GamePath',('"'+$folder.Text+'"'))
                    Start-Process powershell.exe -Verb RunAs -ArgumentList $elevated
                }
            }
        }
        $script:worker.Dispose();$script:worker=$null
        foreach($path in @($script:outLog,$script:errLog,$script:selectionFile)){if($path -and (Test-Path -LiteralPath $path)){Remove-Item -LiteralPath $path}}
        foreach($c in $script:actionControls){$c.Enabled=$true};$folder.ReadOnly=$false;Update-Availability
        if($code -ne 0){$statusLabel.Text='AKTION NICHT ABGESCHLOSSEN'}
    }
})
$install.Add_Click({& $execute 'Install'})
$uninstall.Add_Click({if([Windows.Forms.MessageBox]::Show('Sprachausgabe entfernen? Spielstände und andere Mods bleiben erhalten.','Deinstallieren','YesNo','Question') -eq 'Yes'){& $execute 'Uninstall'}})
$form.Add_FormClosing({param($sender,$eventArgs) if($script:worker -and -not $script:worker.HasExited){$eventArgs.Cancel=$true}})
$form.AcceptButton=$install;$form.CancelButton=$close
$form.Add_Shown({
    # Fit on small working areas (including taskbar), with scrolling instead of clipped actions.
    $area=[Windows.Forms.Screen]::FromControl($form).WorkingArea
    if($form.Height -gt $area.Height -or $form.Width -gt $area.Width){
        $form.AutoScroll=$true;$form.AutoScrollMinSize=New-Object Drawing.Size($form.ClientSize.Width,$form.ClientSize.Height)
        $form.Size=New-Object Drawing.Size([Math]::Min($form.Width,$area.Width),[Math]::Min($form.Height,$area.Height))
        $form.Location=New-Object Drawing.Point($area.Left,$area.Top)
    }
})
if($Action -eq 'Uninstall'){$status.Text='Zum Entfernen bitte auf Deinstallieren klicken.'}
. (Join-Path $script:PackageRoot 'InstallerExperience.ps1')
if($NoUI){
    if($ExerciseTutorial){& $showTutorial}
    $experienceResult=$null
    if($ExerciseExperience){
        $form.Show();[Windows.Forms.Application]::DoEvents()
        $aboutPoint=New-Object Drawing.Point(($about.Left+10),($about.Top+10))
        $helpPoint=New-Object Drawing.Point(($tutorial.Left+10),($tutorial.Top+10))
        $aboutHit=$content.GetChildAtPoint($aboutPoint) -eq $about
        $helpHit=$content.GetChildAtPoint($helpPoint) -eq $tutorial
        $about.PerformClick();$aboutTitle=$script:lastDialogTitle;$aboutText=$script:lastDialogText
        $pages.SelectedTab=$supportPage;[Windows.Forms.Application]::DoEvents();$news.PerformClick();$newsTitle=$script:lastDialogTitle
        $pages.SelectedTab=$helpPage;[Windows.Forms.Application]::DoEvents();$helpVisible=$helpBody.Visible
        $pages.SelectedTab=$setupPage;[Windows.Forms.Application]::DoEvents()
        $experienceResult=[pscustomobject]@{about_hit=$aboutHit;help_hit=$helpHit;about_title=$aboutTitle;author_text=$aboutText;news_title=$newsTitle;help_visible=$helpVisible;support_unpublished_visible=(-not $script:modLinkValid);setup_actions_fit=($status.Bottom -le $setupPage.ClientSize.Height);status_bottom=$status.Bottom;page_height=$setupPage.ClientSize.Height;diagnostic=(Hide-PrivatePaths 'C:\Users\Privat\Spiel\audio.wav')}
        $form.Hide()
    }
    if($ExerciseAction){& $execute $ExerciseAction;while($script:worker){[Windows.Forms.Application]::DoEvents();Start-Sleep -Milliseconds 100};if($script:workerExit -ne 0){throw $status.Text}}
    if($PreviewPath){
        $pages.SelectedTab=if($PreviewTab -eq 'Support'){$supportPage}elseif($PreviewTab -eq 'Help'){$helpPage}else{$setupPage}
        $form.Show();[Windows.Forms.Application]::DoEvents();$form.Refresh()
        $bitmap=New-Object Drawing.Bitmap($form.Width,$form.Height)
        $form.DrawToBitmap($bitmap,(New-Object Drawing.Rectangle(0,0,$form.Width,$form.Height)))
        $bitmap.Save($PreviewPath,[Drawing.Imaging.ImageFormat]::Png);$bitmap.Dispose();$form.Hide()
    }
    [pscustomobject]@{title=$form.Text;buttons=$content.Controls.Count;install_enabled=$install.Enabled;path=$folder.Text;design_id=$design.design_id;worker_exit=$script:workerExit;update_enabled=$update.Enabled;installed_status=$statusLabel.Text;tutorial=$script:tutorialResult;display_font=$(if($script:displayFonts -and $script:displayFonts.Families.Count){$script:displayFonts.Families[0].Name}else{'System'});experience=$experienceResult;pages=@($pages.TabPages|ForEach-Object {$_.Text});requirements=$requirements.Text;about_enabled=$about.Enabled;problem_enabled=$problem.Enabled;progress=$progress.Value;diagnostic_sample=(Hide-PrivatePaths 'Fehler in C:\Users\Privat\Spiel\audio.wav')}|ConvertTo-Json -Depth 5
}else{[void]$form.ShowDialog()}
$timer.Dispose();$form.Dispose();if($script:displayFonts){$script:displayFonts.Dispose()};if($script:art){$script:art.Dispose()};if($script:windowIcon){$script:windowIcon.Dispose()}
