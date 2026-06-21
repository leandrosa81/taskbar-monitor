using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TaskbarMonitor.Counters
{
    class CounterGPUMemory : ICounter
    {
        PerformanceCounterReader reader;

        private static long GetTotalGpuMemoryFromRegistry(RegistryView view)
        {
            const string classKey = "SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}";
            long total = 0;

            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                using (var key = baseKey.OpenSubKey(classKey))
                {
                    if (key == null) return 0;

                    foreach (var sub in key.GetSubKeyNames())
                    {
                        try
                        {
                            using (var sk = key.OpenSubKey(sub))
                            {
                                if (sk == null) continue;

                                var name = sk.GetValue("DriverDesc") as string;
                                object memVal = sk.GetValue("HardwareInformation.qwMemorySize");
                                if (memVal == null) memVal = sk.GetValue("HardwareInformation.MemorySize");
                                if (memVal == null) memVal = sk.GetValue("AdapterMemorySize");

                                if (memVal == null || string.IsNullOrEmpty(name)) continue;

                                long bytes = 0;

                                if (memVal is int) bytes = (int)memVal;
                                else if (memVal is long) bytes = (long)memVal;
                                else if (memVal is string)
                                {
                                    var s = (string)memVal;
                                    if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (long.TryParse(s.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out long v)) bytes = v;
                                    }
                                    else if (!long.TryParse(s, out bytes))
                                    {
                                        bytes = 0;
                                    }
                                }
                                else if (memVal is byte[])
                                {
                                    var b = (byte[])memVal;
                                    if (b.Length >= 8) bytes = (long)BitConverter.ToUInt64(b, 0);
                                    else if (b.Length >= 4) bytes = BitConverter.ToUInt32(b, 0);
                                }

                                if (bytes > 0)
                                {
                                    total += bytes;
                                }
                            }
                        }
                        catch
                        {
                            // ignore individual subkey errors
                        }
                    }
                }
            }
            catch
            {
                return 0;
            }

            return total;
        }

        private static long GetTotalGpuMemoryFromWmi()
        {
            try
            {
                long total = 0;

                var start = new ProcessStartInfo()
                {
                    FileName = "wmic",
                    Arguments = "path Win32_VideoController get AdapterRAM /format:list",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var proc = Process.Start(start))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(2000);

                    if (string.IsNullOrEmpty(output)) return 0;

                    var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('=');
                        if (parts.Length != 2) continue;
                        var key = parts[0].Trim();
                        var val = parts[1].Trim();
                        if (!key.Equals("AdapterRAM", StringComparison.OrdinalIgnoreCase)) continue;

                        if (long.TryParse(val, out long bytes))
                        {
                            if (bytes > 0) total += bytes;
                        }
                    }
                }

                return total;
            }
            catch
            {
                return 0;
            }
        }
        
        public CounterGPUMemory(Options options)
            : base(options)
        {
        }

        internal override void Initialize(PerformanceCounterReader reader)
        {
            float max = 100.0f;

            // Try to read installed GPU memory from registry. If that fails (may require admin) fallback to WMI which works as a normal user.
            try
            {
                long totalBytes = 0;

                try
                {
                    totalBytes = GetTotalGpuMemoryFromRegistry(RegistryView.Registry64);
                    if (totalBytes == 0)
                    {
                        // Fallback to 32-bit view if 64-bit view didn't return values
                        totalBytes = GetTotalGpuMemoryFromRegistry(RegistryView.Registry32);
                    }
                }
                catch
                {
                    // registry read may fail due to permissions; ignore and try WMI
                    totalBytes = 0;
                }

                if (totalBytes == 0)
                {
                    totalBytes = GetTotalGpuMemoryFromWmi();
                }

                if (totalBytes > 0)
                {
                    max = (float)totalBytes / (1024f * 1024f * 1024f);
                }
            }
            catch
            {
                // Ignore and keep default max
            }

            this.reader = reader;
            reader.AddPath(@"\GPU Adapter Memory(*)\Dedicated Usage");

            /*var category = new PerformanceCounterCategory("GPU Local Adapter Memory");
            var counterNames = category.GetInstanceNames();

            List<PerformanceCounter> gpuCounters = counterNames
                                    .Where(counterName => counterName.EndsWith("part_0"))
                                    .SelectMany(counterName => category.GetCounters(counterName))
                                    .Where(counter => counter.CounterName.Equals("Local Usage"))
                                    .ToList();

            gpuCounters.ForEach(x => x.NextValue());
            max = gpuCounters.Sum(x => x.NextValue()) / 1024;            */


             
            lock (ThreadLock)
            {
                InfoSummary = new CounterInfo() { Name = "GPU Summary", History = new List<float>(), MaximumValue = max };
                Infos = new List<CounterInfo>();
                Infos.Add(new CounterInfo() { Name = "GPU", History = new List<float>(), MaximumValue = max });
            }
        }
        public override void Update()
        {
            float currentValue = 0;

            try
            {
                currentValue = reader.Values.Where(x => x.Key.StartsWith(@"\GPU Adapter Memory(*)\Dedicated Usage")).Sum(x => x.Value) / 1024 / 1024 / 1024;                
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
            }

            lock (ThreadLock)
            {
                InfoSummary.CurrentValue = currentValue;
                InfoSummary.History.Add(currentValue);
                if (InfoSummary.History.Count > Options.HistorySize) InfoSummary.History.RemoveAt(0);

                 
                InfoSummary.CurrentStringValue = (InfoSummary.CurrentValue).ToString("0.0") + "GB";

                {
                    var info = Infos.Where(x => x.Name == "GPU").Single();
                    info.CurrentValue = currentValue;
                    info.History.Add(currentValue);
                    if (info.History.Count > Options.HistorySize) info.History.RemoveAt(0);

                    info.CurrentStringValue = (info.CurrentValue).ToString("0.0") + "GB";
                }
                 

            }
        }
       
        public override string GetName()
        {
            return "GPU MEM";
        }         

        public override CounterType GetCounterType()
        {
            return Options.CounterOptions[GetName()].GraphType;
        }

        public new static bool IsAvailable()
        {
            try
            {                                                               
                var category = new PerformanceCounterCategory("GPU Adapter Memory");
                var counterNames = category.GetInstanceNames();

                List<PerformanceCounter> gpuCounters = counterNames
                                        //.Where(counterName => counterName.EndsWith("engtype_3D"))
                                        .SelectMany(counterName => category.GetCounters(counterName))
                                        .Where(counter => counter.CounterName.Equals("Dedicated Usage"))
                                        .ToList();

                gpuCounters.ForEach(x => x.NextValue());                   
                
                return true;
            }
            catch (InvalidOperationException ex)
            {
                return false;
            }
        }
        
    }
}
