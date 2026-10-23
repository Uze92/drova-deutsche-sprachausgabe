using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
internal static class Launcher {
    [STAThread] static void Main(string[] args) {
        string root=AppDomain.CurrentDomain.BaseDirectory;
        string setup=Path.Combine(root,"Setup.ps1");
        if(!File.Exists(setup)) { MessageBox.Show("Bitte das ZIP vollständig entpacken und den Installer aus dem entpackten Ordner starten.","Deutsche Stimmen");return; }
        try {
            bool check=args.Length==1 && args[0]=="--check";
            var info=new ProcessStartInfo("powershell.exe","-NoProfile -ExecutionPolicy Bypass -STA -File \""+setup+"\""+(check?" -NoUI":""));
            info.UseShellExecute=false;info.CreateNoWindow=true;info.WorkingDirectory=root;
            using(var process=Process.Start(info)) { process.WaitForExit();Environment.ExitCode=process.ExitCode; if(process.ExitCode!=0 && !check) MessageBox.Show("Der Installer konnte nicht vollständig ausgeführt werden. Bitte das Paket erneut entpacken. Zur Fehleranzeige kann Installieren.cmd gestartet werden.","Deutsche Stimmen"); }
        } catch(Exception ex) { MessageBox.Show("Der Installer konnte nicht gestartet werden: "+ex.Message,"Deutsche Stimmen"); }
    }
}
