using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using TaskbarMonitorInstaller.BLL;
using static System.Net.Mime.MediaTypeNames;

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
            public string LegacyTargetPath { get; set; }
        }

        static void Main(string[] args)
        {
            Console.Title = "Taskbar Monitor Installer";

            InstallInfo info = new InstallInfo
            {
                FilesToCopy = new Dictionary<string, byte[]> { 
                    { "TaskbarMonitor.dll", Properties.Resources.TaskbarMonitor }, 
                    { "Newtonsoft.Json.dll", Properties.Resources.Newtonsoft_Json },
                    { "TaskbarMonitorWindows11.exe", Properties.Resources.TaskbarMonitorWindows11 },
                    { "TaskbarMonitorInstaller.exe", File.ReadAllBytes(Assembly.GetExecutingAssembly().Location)        }
                },
                FilesToRegister = new List<string> { "TaskbarMonitor.dll" },
                //TargetPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "TaskbarMonitor")
                TargetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "TaskbarMonitor"),
                LegacyTargetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "TaskbarMonitor")
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
            Console.WriteLine("Installing taskbar-monitor on your computer, please wait.");
            if (WindowsInformation.IsWindows11())
                Console.WriteLine("Windows 11 detected.");

            Action copyFiles = () =>
            {
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
            };

            // Create directory
            if (!Directory.Exists(info.TargetPath))
            {
                Console.Write("Creating target directory... ");
                Directory.CreateDirectory(info.TargetPath);
                Console.WriteLine("OK.");

                copyFiles();
            }
            else
            {
                if (WindowsInformation.IsWindows11())
                {
                    // we unregister old versions
                    foreach (var item in info.FilesToRegister)
                    {
                        var targetFilePath = Path.Combine(info.TargetPath, item);
                        Console.Write($"Unregistering {item}... ");
                        RegisterDLL(targetFilePath, false, false);
                        RegisterDLL(targetFilePath, true, false);

                        targetFilePath = Path.Combine(info.LegacyTargetPath, item);                        
                        RegisterDLL(targetFilePath, false, false);
                        RegisterDLL(targetFilePath, true, false);
                    }
                    // then we terminate existing processes
                    KillProcess("TaskbarMonitorWindows11");

                    // then copy files
                    copyFiles();
                }
                else
                {
                    RestartExplorer restartExplorer = new RestartExplorer();
                    restartExplorer.Execute(copyFiles);
                }
            }

            if(Directory.Exists(info.LegacyTargetPath))
            {
                DeleteFiles(info, true);
            }
            
            // if not on windows 11 we must register the DLLs to run on the deskband
            if (!WindowsInformation.IsWindows11())
            {
                // Register assemblies
                //RegistrationServices regAsm = new RegistrationServices();
                foreach (var item in info.FilesToRegister)
                {
                    var targetFilePath = Path.Combine(info.TargetPath, item);
                    Console.Write($"Registering {item}... ");
                    RegisterDLL(targetFilePath, true, false);
                    Console.WriteLine("OK.");
                }
            }

            // register the uninstaller
            Console.Write("Registering uninstaller... ");
            CreateUninstaller(Path.Combine(info.TargetPath, "TaskbarMonitorInstaller.exe"));
            Console.WriteLine("OK.");

            // finally, remove pending delete operations
            {
                Console.Write("Cleaning up previous pending uninstalls... ");
                if (CleanUpPendingDeleteOperations(info.TargetPath, out string errorMessage))
                    Console.WriteLine("OK.");
                else
                    Console.WriteLine("ERROR: " + errorMessage);
            }

            if (WindowsInformation.IsWindows11())
            {
                Console.Write("Registering on startup... ");
                try
                {
                    SetStartup("taskbar-monitor", Path.Combine(info.TargetPath, "TaskbarMonitorWindows11.exe"));
                    Console.WriteLine("OK.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR.");
                    Console.WriteLine(" " + ex.Message);
                }

                Console.Write("Runnning... ");
                try
                {
                    RunProgram(Path.Combine(info.TargetPath, "TaskbarMonitorWindows11.exe"), "", false);
                    Console.WriteLine("OK.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR.");
                    Console.WriteLine(" " + ex.Message);
                }
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
            // Unregister assemblies          
            foreach (var item in info.FilesToRegister)
            {                
                var targetFilePath = Path.Combine(info.TargetPath, item);
                Console.Write($"Unregistering {item}... ");
                RegisterDLL(targetFilePath, false, true);
                RegisterDLL(targetFilePath, true, true);
                Console.WriteLine("OK.");
            }

            if (!WindowsInformation.IsWindows11())
            {
                // Delete files
                RestartExplorer restartExplorer = new RestartExplorer();
                restartExplorer.Execute(() => { DeleteFiles(info); });
            }
            else
            {
                // then we terminate existing processes
                KillProcess("TaskbarMonitorWindows11.exe");

                DeleteFiles(info);
                if (Directory.Exists(info.LegacyTargetPath))
                {
                    DeleteFiles(info, true);
                }

                Console.Write("Unregistering startup... ");
                try
                {
                    DeleteStartup("taskbar-monitor");
                    Console.WriteLine("OK.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR.");
                    Console.WriteLine(" " + ex.Message);
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

        private static void DeleteFiles (InstallInfo info, bool legacy = false)
        {
            var path = legacy ? info.LegacyTargetPath : info.TargetPath;
            foreach (var file in info.FilesToCopy)
            {
                var item = file.Key;
                var targetFilePath = Path.Combine(path, item);
                if (File.Exists(targetFilePath))
                {
                    Console.Write($"Deleting {item}... ");
                    try
                    {
                        if (Win32Api.DeleteFile(Path.Combine(path, item)))
                        {
                            Console.WriteLine("OK.");
                        }
                        else
                        {
                            Win32Api.MoveFileEx(Path.Combine(path, item), null, MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT);
                            Console.WriteLine("Scheduled for deletion after next reboot.");
                        }
                    }
                    catch
                    {
                        Win32Api.MoveFileEx(Path.Combine(path, item), null, MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT);
                        Console.WriteLine("Scheduled for deletion after next reboot.");
                    }
                    Console.WriteLine("OK.");
                }
            }
        }

        static private void SetStartup(string appName, string path)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            
                rk.SetValue(appName, "\"" + path + "\"");
        }

        static private void DeleteStartup(string appName)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);            

            rk.DeleteValue(appName, false);

        }

        static string RunProgram(string path, string args, bool wait=true)
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
                if (wait)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return output;
                }

                return null;
            }
        }

        static bool KillProcess(String name)
        {
            try
            {
                Process[] workers = Process.GetProcessesByName(name);
                foreach (Process worker in workers)
                {
                    worker.Kill();
                    worker.WaitForExit();
                    worker.Dispose();
                }
                return true;
            }
            catch
            {
                return false;
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
            using (RegistryKey localKey32 =
    RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine,
        RegistryView.Registry32))
            {
                using (Microsoft.Win32.RegistryKey parent = localKey32.OpenSubKey(
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
                        DeleteUninstaller();
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
    public static class WindowsInformation
    {
        public static bool IsWindows11()
        {
            return System.Environment.OSVersion.Version.Major >= 10 && System.Environment.OSVersion.Version.Build >= 21996;
        }
    }
}
