using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using TaskbarMonitorInstaller.BLL;

namespace TaskbarMonitorInstaller
{
    class Program
    {

        static Guid UninstallGuid = new Guid(@"c7f3d760-a8d1-4fdc-9c74-41bf9112e835");
        class InstallInfo
        {
            public List<string> FilesToCopy { get; set; }
            public List<string> FilesToRegister { get; set; }
            public string TargetPath { get; set; }
        }

        static void Main(string[] args)
        {
            Console.Title = "Taskbar Monitor Installer";

            InstallInfo info = new InstallInfo
            {
                FilesToCopy = new List<string> { "TaskbarMonitor.dll" },
                FilesToRegister = new List<string> { "TaskbarMonitor.dll" },
                //TargetPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "TaskbarMonitor")
                TargetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "TaskbarMonitor")
            };

            if (args.Length > 0 && args[0].ToLower() == "/uninstall")
            {
                RollBack(info);
            }
            else
            {
                Install(info);
            }

            // pause
            Console.WriteLine("Press any key to close this window..");
            Console.ReadKey();
        }

        public static void WriteEmbeddedResourceToFile(string resourceName, string fileName)
        {
            string fullResourceName = $"{Assembly.GetExecutingAssembly().GetName().Name}.Resources.{resourceName}";

            using (Stream manifestResourceSTream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fullResourceName))
            {
                using (FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    manifestResourceSTream.CopyTo(fileStream);
                }
            }
        }

        static void Install(InstallInfo info)
        {
            Console.WriteLine("Installing taskbar-monitor on your computer, please wait.");
            RestartExplorer restartExplorer = new RestartExplorer();                        

            // Create directory
            if (!Directory.Exists(info.TargetPath))
            {
                Console.Write("Creating target directory... ");
                Directory.CreateDirectory(info.TargetPath);
                Console.WriteLine("OK.");

                // First copy files to program files folder          
                foreach (var item in info.FilesToCopy)
                {
                    var targetFilePath = Path.Combine(info.TargetPath, item);
                    Console.Write(string.Format("Copying {0}... ", item));
                    WriteEmbeddedResourceToFile(item, targetFilePath);
                    //File.Copy(item, targetFilePath, true);
                    Console.WriteLine("OK.");
                }

                // Copy the uninstaller too
                File.Copy(Assembly.GetExecutingAssembly().Location, Path.Combine(info.TargetPath, "TaskbarMonitorInstaller.exe"), true);
            }
            else
            {
                
                restartExplorer.Execute(() =>
                {
                    // First copy files to program files folder          
                    foreach (var item in info.FilesToCopy)
                    {
                        var targetFilePath = Path.Combine(info.TargetPath, item);
                        Console.Write($"Copying {item}... ");
                        WriteEmbeddedResourceToFile(item, targetFilePath);
                        //File.Copy(item, targetFilePath, true);
                        Console.WriteLine("OK.");
                    }

                    // Copy the uninstaller too
                    File.Copy(Assembly.GetExecutingAssembly().Location, Path.Combine(info.TargetPath, "TaskbarMonitorInstaller.exe"), true);
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

            Console.Write("Registering uninstaller... ");
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(info.TargetPath, "TaskbarMonitorInstaller.dll"));
            CreateUninstaller(Path.Combine(info.TargetPath, "TaskbarMonitorInstaller.exe"), Version.Parse(fileVersionInfo.FileVersion));
            Console.WriteLine("OK.");

            // remove pending delete operations
            {
                Console.Write("Cleaning up previous pending uninstalls... ");
                if (CleanUpPendingDeleteOperations(info.TargetPath, out string errorMessage))
                    Console.WriteLine("OK.");
                else
                    Console.WriteLine("ERROR: " + errorMessage);
            }
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
                foreach (var item in info.FilesToCopy)
                {
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
                    if (Win32Api.DeleteFile(Path.Combine(info.TargetPath, item)))
                    {
                        Console.WriteLine("OK.");
                    }
                    else
                    {
                        Win32Api.MoveFileEx(Path.Combine(info.TargetPath, item), null, MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT);
                        Console.WriteLine("Scheduled for deletion after next reboot.");
                    }
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
        static private void CreateUninstaller(string pathToUninstaller, Version version)
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

                        string exe = pathToUninstaller;

                        key.SetValue("DisplayName", "taskbar-monitor");
                        key.SetValue("ApplicationVersion", version.ToString());
                        key.SetValue("Publisher", "Leandro Lugarinho");
                        key.SetValue("DisplayIcon", exe);
                        key.SetValue("DisplayVersion", version.ToString(3));
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

        static bool CleanUpPendingDeleteOperations(string basepath, out string errorMessage)
        {
            // here we check the registry for pending operations on the program files (previous pending uninstall)
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\", true))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue("PendingFileRenameOperations");
                        if (o != null)
                        {
                            var values = o as String[];
                            List<string> dest = new List<string>();
                            for (int i = 0; i < values.Length; i+=2)
                            {
                                if(!values[i].Contains(basepath))
                                {
                                    dest.Add(values[i]);
                                    dest.Add(values[i+1]);
                                }
                            }
                            //if (dest.Count > 0)
                                key.SetValue("PendingFileRenameOperations", dest.ToArray());
                            //else
                                //key.DeleteValue("PendingFileRenameOperations");
                        }
                    }
                }
                errorMessage = "";
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                errorMessage = "An error occurred cleaning up previous uninstall information to the registry. The program might be partially uninstalled on the next reboot.";                
                return false;                
            }
        }
    }
}
