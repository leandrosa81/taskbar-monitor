using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TaskbarMonitorInstaller.BLL;

namespace TaskbarMonitorInstaller
{
    class Program
    {
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
            }
            else
            {
                //foreach (var item in info.FilesToRegister)
                //{
                //    var targetFilePath = System.IO.Path.Combine(info.TargetPath, item);                    
                //    RegisterDLL(targetFilePath, false, true);
                //    RegisterDLL(targetFilePath, true, true);
                //    Console.WriteLine("OK.");

                //}

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

            if (Directory.Exists(info.TargetPath))
            {
                Console.Write("Deleting target directory... ");
                Directory.Delete(info.TargetPath);
                Console.WriteLine("OK.");
            }

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
    }
}
