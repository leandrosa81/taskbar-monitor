using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
                FilesToCopy = new List<string> { "SystemWatchBand.dll" },
                FilesToRegister = new List<string> { "SystemWatchBand.dll" },
                TargetPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SystemWatchBand")
            };            

            if (args.Length > 0 && args[0].ToLower() == "/uninstall")
                RollBack(info);
            else
                Install(info);            
        }

        static void Install(InstallInfo info)
        {
            // create directory
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

            // register assemblies
            //RegistrationServices regAsm = new RegistrationServices();
            foreach (var item in info.FilesToRegister)
            {
                var targetFilePath = System.IO.Path.Combine(info.TargetPath, item);
                var regAsm64Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Microsoft.NET\Framework64\v4.0.30319\regasm.exe");
                Console.Write(String.Format("registering {0}... ", item));
                RunProgram(regAsm64Path, @"/nologo /codebase """ + targetFilePath + @"""");
                Console.WriteLine("OK.");
                //RunProgram(regAsm64Path, @"/nologo """ + targetFilePath + @"""");

                var regAsmPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Microsoft.NET\Framework\v4.0.30319\regasm.exe");
                //RunProgram(regAsmPath, @"/nologo /codebase """ + targetFilePath + @"""");
                //RunProgram(regAsmPath, @"/nologo """ + targetFilePath + @"""");

                /*Assembly asm = Assembly.LoadFile(targetFilePath);
                bool bResult = regAsm.RegisterAssembly(asm, AssemblyRegistrationFlags.SetCodeBase);
                if (!bResult)
                {
                    Console.Error.WriteLine("Error registering DLLs, you need to run this with administrator privileges.");
                    RollBack(info);
                    return;
                }*/
            }            
             
            // pause
            System.Console.ReadKey();
        }

        static bool RollBack(InstallInfo info)
        {
            // deactivate the band

            // unregister assembly
            //RegistrationServices regAsm = new RegistrationServices();
            foreach (var item in info.FilesToRegister)
            {
                var targetFilePath = System.IO.Path.Combine(info.TargetPath, item);
                //Assembly asm = Assembly.LoadFile(targetFilePath);                
                //regAsm.UnregisterAssembly(asm);
                RunProgram(@"%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe", @"/nologo /unregister """ + targetFilePath + @"""");
            }

            // delete files

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
