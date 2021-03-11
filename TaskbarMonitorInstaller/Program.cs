using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TaskbarMonitorInstaller.BLL;

namespace TaskbarMonitorInstaller
{
    class Program
    {

        static Guid UninstallGuid = new Guid(@"c7f3d760-a8d1-4fdc-9c74-41bf9112e835");
        class InstallInfo
        {
            public Dictionary<string, byte[]> FilesToCopy { get; set; }
            public List<string> FilesToRegister { get; set; }
            public string TargetPath { get; set; }
        }

        static void Main(string[] args)
        {
            Console.Title = "Taskbar Monitor Installer";

            InstallInfo info = new InstallInfo
            {
                FilesToCopy = new Dictionary<string, byte[]> { 
                    { "TaskbarMonitor.dll", Properties.Resources.TaskbarMonitor }, 
                    { "Newtonsoft.Json.dll", Properties.Resources.Newtonsoft_Json } 
                },
                FilesToRegister = new List<string> { "TaskbarMonitor.dll" },
                //TargetPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "TaskbarMonitor")
                TargetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "TaskbarMonitor")
            };

            if (args.Length > 0 && args[0].ToLower() == "/uninstall")
                RollBack(info);
            else
                Install(info);

            // pause
            Console.WriteLine("Press any key to close this window...");
            Console.ReadKey();
        }

        static void Install(InstallInfo info)
        {
            RestartExplorer restartExplorer = new RestartExplorer();
            //restartExplorer.ReportProgress += Console.WriteLine;
            //restartExplorer.ReportPercentage += (percentage) =>
            //Console.WriteLine($"Percentage: {percentage}");

            // Create directory
            if (!Directory.Exists(info.TargetPath))
            {
                Console.Write("Creating target directory... ");
                Directory.CreateDirectory(info.TargetPath);
                Console.WriteLine("OK.");

                // First copy files to program files folder          
                foreach (var file in info.FilesToCopy)
                {
                    var item = file.Key;

                    var targetFilePath = Path.Combine(info.TargetPath, item);
                    Console.Write(string.Format("Copying {0}... ", item));
                    File.WriteAllBytes(targetFilePath, file.Value);
                    //File.Copy(item, targetFilePath, true);
                    Console.WriteLine("OK.");
                }
                // copy the uninstaller too
                File.Copy("TaskbarMonitorInstaller.exe", Path.Combine(info.TargetPath, "TaskbarMonitorInstaller.exe"));
            }
            else
            {
                
                restartExplorer.Execute(() =>
                {
                    // First copy files to program files folder          
                    foreach (var file in info.FilesToCopy)
                    {
                        var item = file.Key;

                        var targetFilePath = Path.Combine(info.TargetPath, item);
                        Console.Write($"Copying {item}... ");
                        File.WriteAllBytes(targetFilePath, file.Value);
                        //File.Copy(item, targetFilePath, true);
                        Console.WriteLine("OK.");
                    }
                    // copy the uninstaller too
                    File.Copy("TaskbarMonitorInstaller.exe", Path.Combine(info.TargetPath, "TaskbarMonitorInstaller.exe"), true);
                });
            }

            // Register assemblies
            //RegistrationServices regAsm = new RegistrationServices();
            foreach (var item in info.FilesToRegister)
            {
                var targetFilePath = Path.Combine(info.TargetPath, item);
                Console.Write($"Registering {item}... ");
                RegisterDLL(targetFilePath, true, false);
                Console.WriteLine("OK.");
            }

            CreateUninstaller(Path.Combine(info.TargetPath, "TaskbarMonitorInstaller.exe"));
        }

        static bool RegisterDLL(string target, bool bit64 = false, bool unregister = false)
        {
            string args = unregister ? "/unregister" : "/nologo /codebase";
            var regAsmPath = bit64 ?
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Microsoft.NET\Framework64\v4.0.30319\regasm.exe") :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Microsoft.NET\Framework\v4.0.30319\regasm.exe");
            RunProgram(regAsmPath, $@"{args} ""{target}""");

            return true;
        }

        static bool RollBack(InstallInfo info)
        {
            // Unregister assembly
            //RegistrationServices regAsm = new RegistrationServices();
            foreach (var item in info.FilesToRegister)
            {
                var targetFilePath = Path.Combine(info.TargetPath, item);
                RegisterDLL(targetFilePath, false, true);
                RegisterDLL(targetFilePath, true, true);
            }

            // Delete files
            RestartExplorer restartExplorer = new RestartExplorer();
            restartExplorer.Execute(() =>
            {
                // First copy files to program files folder          
                foreach (var file in info.FilesToCopy)
                {
                    var item = file.Key;
                    var targetFilePath = Path.Combine(info.TargetPath, item);
                    if (File.Exists(targetFilePath))
                    {
                        Console.Write($"Deleting {item}... ");
                        File.Delete(targetFilePath);
                        Console.WriteLine("OK.");
                    }
                }
                
            });

            {
                var item = "TaskbarMonitorInstaller.exe";
                Console.Write($"Deleting {item}... ");
                try
                {
                    Win32Api.DeleteFile(Path.Combine(info.TargetPath, item));
                    Console.WriteLine("OK.");
                }
                catch
                {
                    Win32Api.MoveFileEx(Path.Combine(info.TargetPath, item), null, MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT);
                    Console.WriteLine("Scheduled for deletion after next reboot.");
                }
                
            }
            
            if (Directory.Exists(info.TargetPath))
            {
                Console.Write("Deleting target directory... ");
                try
                {
                    Directory.Delete(info.TargetPath);
                    Console.WriteLine("OK.");
                }
                catch
                {
                    Win32Api.MoveFileEx(info.TargetPath, null, MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT);
                    Console.WriteLine("Scheduled for deletion after next reboot.");
                }
            }
            Console.Write("Removing uninstall info from registry... "); 
            DeleteUninstaller();
            Console.WriteLine("OK.");

            return true;
        }

        static string RunProgram(string path, string args)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = path;
                process.StartInfo.Arguments = args;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return output;
            }
        }
        static private void DeleteUninstaller()
        {
            var UninstallRegKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            using (Microsoft.Win32.RegistryKey parent = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                        UninstallRegKeyPath, true))
            {
                if (parent == null)
                {
                    throw new Exception("Uninstall registry key not found.");
                }
                string guidText = UninstallGuid.ToString("B");
                parent.DeleteSubKeyTree(guidText, false);
            }
        
        }
        static private void CreateUninstaller(string pathToUninstaller)
        {
            var UninstallRegKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            using (Microsoft.Win32.RegistryKey parent = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                        UninstallRegKeyPath, true))
            {
                if (parent == null)
                {
                    throw new Exception("Uninstall registry key not found.");
                }
                try
                {
                    Microsoft.Win32.RegistryKey key = null;

                    try
                    {
                        string guidText = UninstallGuid.ToString("B");
                        key = parent.OpenSubKey(guidText, true) ??
                              parent.CreateSubKey(guidText);

                        if (key == null)
                        {
                            throw new Exception(String.Format("Unable to create uninstaller '{0}\\{1}'", UninstallRegKeyPath, guidText));
                        }

                        Version v = new Version(Properties.Resources.Version);
                         
                        string exe = pathToUninstaller;

                        key.SetValue("DisplayName", "taskbar-monitor");
                        key.SetValue("ApplicationVersion", v.ToString());
                        key.SetValue("Publisher", "Leandro Lugarinho");
                        key.SetValue("DisplayIcon", exe);
                        key.SetValue("DisplayVersion", v.ToString(3));
                        key.SetValue("URLInfoAbout", "https://lugarinho.tech/tools/taskbar-monitor");
                        key.SetValue("Contact", "leandrosa81@gmail.com");
                        key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
                        key.SetValue("UninstallString", exe + " /uninstall");
                    }
                    finally
                    {
                        if (key != null)
                        {
                            key.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        "An error occurred writing uninstall information to the registry.  The service is fully installed but can only be uninstalled manually through the command line.",
                        ex);
                }
            }
        }
    }
}
