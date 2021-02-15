using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TaskbarMonitorInstaller.BLL;

namespace TaskbarMonitorInstaller
{
    class Program
    {
        class InstallInfo
        {
            public List<String> FilesToCopy { get; set; }
            public List<String> FilesToRegister { get; set; }
            public string TargetPath { get; set; }
        }
        static void Main(string[] args)
        {
            InstallInfo info = new InstallInfo { 
                FilesToCopy = new List<string> { "TaskbarMonitor.dll", "Newtonsoft.Json.dll" },                


                FilesToRegister = new List<string> { "TaskbarMonitor.dll" },
                //TargetPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "TaskbarMonitor")
                TargetPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "TaskbarMonitor")
            };            

            if (args.Length > 0 && args[0].ToLower() == "/uninstall")
                RollBack(info);
            else
                Install(info);

            // pause
            Console.Write("Press any key to close this window...");
            System.Console.ReadKey();
        }

        static void Install(InstallInfo info)
        {
            RestartExplorer restartExplorer = new RestartExplorer();
            //restartExplorer.ReportProgress += Console.WriteLine;
            //restartExplorer.ReportPercentage += (percentage) =>
            //Console.WriteLine($"Percentage: {percentage}");

            // create directory
            if (!System.IO.Directory.Exists(info.TargetPath))
            {
                Console.Write("Creating target directory... ");
                System.IO.Directory.CreateDirectory(info.TargetPath);
                Console.WriteLine("OK.");

                // first copy files to program files folder          
                foreach (var item in info.FilesToCopy)
                {
                    var targetFilePath = System.IO.Path.Combine(info.TargetPath, item);
                    Console.Write(String.Format("copying {0}... ", item));
                    System.IO.File.Copy(item, targetFilePath, true);
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
                    // first copy files to program files folder          
                    foreach (var item in info.FilesToCopy)
                    {
                        var targetFilePath = System.IO.Path.Combine(info.TargetPath, item);
                        Console.Write(String.Format("copying {0}... ", item));
                        System.IO.File.Copy(item, targetFilePath, true);
                        Console.WriteLine("OK.");
                    }

                });
            }
            

            // register assemblies
            //RegistrationServices regAsm = new RegistrationServices();
            foreach (var item in info.FilesToRegister)
            {
                var targetFilePath = System.IO.Path.Combine(info.TargetPath, item);
                Console.Write(String.Format("registering {0}... ", item));
                RegisterDLL(targetFilePath, true, false);
                Console.WriteLine("OK.");

            }


            
        }

        static bool RegisterDLL(string target, bool bit64 = false, bool unregister = false)
        {
            string args = unregister ? "/unregister" : "/nologo /codebase";
            var regAsmPath = bit64 ? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Microsoft.NET\Framework64\v4.0.30319\regasm.exe") :
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Microsoft.NET\Framework\v4.0.30319\regasm.exe");            
            RunProgram(regAsmPath, args + @" """ + target + @"""");            
            return true;
        }

        static bool RollBack(InstallInfo info)
        {            
            // unregister assembly
            //RegistrationServices regAsm = new RegistrationServices();
            foreach (var item in info.FilesToRegister)
            {
                var targetFilePath = System.IO.Path.Combine(info.TargetPath, item);
                RegisterDLL(targetFilePath, false, true);
                RegisterDLL(targetFilePath, true, true);
            }

            // delete files
            RestartExplorer restartExplorer = new RestartExplorer();
            restartExplorer.Execute(() =>
            {
                // first copy files to program files folder          
                foreach (var item in info.FilesToCopy)
                {
                    var targetFilePath = System.IO.Path.Combine(info.TargetPath, item);
                    if (System.IO.File.Exists(targetFilePath))
                    {
                        Console.Write(String.Format("deleting {0}... ", item));
                        System.IO.File.Delete(targetFilePath);
                        Console.WriteLine("OK.");
                    }
                }
            });

            if (System.IO.Directory.Exists(info.TargetPath))
            {
                Console.Write("Deleting target directory... ");
                System.IO.Directory.Delete(info.TargetPath);
                Console.WriteLine("OK.");
            }

            return true;
        }

        static string RunProgram(string path, string args)
        {
            using (System.Diagnostics.Process pProcess = new System.Diagnostics.Process())
            {
                pProcess.StartInfo.FileName = path;
                pProcess.StartInfo.Arguments = args; //argument
                pProcess.StartInfo.UseShellExecute = false;
                pProcess.StartInfo.RedirectStandardOutput = true;
                pProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                pProcess.StartInfo.CreateNoWindow = true; //not diplay a windows
                pProcess.Start();
                string output = pProcess.StandardOutput.ReadToEnd(); //The output result
                pProcess.WaitForExit();
                return output;
            }
        }
    }
}
