﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TaskbarMonitor.Counters
{
    internal class PerformanceCounterReader: IDisposable
    {
        private List<string> paths = new List<string>();
        public IReadOnlyList<string> Paths
        {
            get
            {
                return paths;
            }
        }
        private bool changedPaths = false;
        private DateTime lastRefresh = DateTime.MinValue;
        private readonly TimeSpan refreshInterval;

        private IntPtr queryHandle = IntPtr.Zero;
        private Dictionary<string, List<IntPtr>> counterHandles = new Dictionary<string, List<IntPtr>>();

        public Dictionary<string, float> Values = new Dictionary<string, float>();

        public PerformanceCounterReader(TimeSpan refreshInterval)
        {            
            this.refreshInterval = refreshInterval;

            // Open PDH query
            if (PdhOpenQuery(null, IntPtr.Zero, out queryHandle) != 0)
                throw new InvalidOperationException("Failed to open PDH query.");
            RefreshCounters();
        }

        public static bool IsAvailable()
        {
            IntPtr query;
            if (PdhOpenQuery(null, IntPtr.Zero, out query) != 0)
                return false;
            PdhCloseQuery(query);
            return true;
        }

        public void AddPath(string path)
        {
            paths.Add(path);
            changedPaths = true;
        }

        private void RefreshCounters()
        {
            // Clear previous counters
            foreach (var path in counterHandles.Keys)
            {
                foreach (var handle in counterHandles[path])
                {
                    PdhRemoveCounter(handle);
                }
            }
            counterHandles.Clear();

            foreach (var path in Paths)
            {
                int status = PdhAddEnglishCounter(queryHandle, path, IntPtr.Zero, out IntPtr counterHandle);
                if (status == 0)
                {
                    if (!counterHandles.ContainsKey(path))
                        counterHandles.Add(path, new List<IntPtr>());

                    counterHandles[path].Add(counterHandle);
                }
            }
        }

        public Dictionary<string, float> ReadCounters()
        {
            Values = new Dictionary<string, float>();
            try
            {
                if (changedPaths || (DateTime.Now - lastRefresh) > refreshInterval)
                {
                    changedPaths = false;
                    RefreshCounters();
                    lastRefresh = DateTime.Now;
                }
                if (PdhCollectQueryData(queryHandle) != 0)
                    throw new InvalidOperationException("Failed to collect PDH data.");

                foreach (var countersByPath in counterHandles)
                {
                    foreach (var counterHandle in countersByPath.Value)
                    {
                        uint bufferSize = 0;
                        uint itemCount = 0;
                        int status = PdhGetFormattedCounterArray(counterHandle, PDH_FMT_DOUBLE, ref bufferSize, ref itemCount, IntPtr.Zero);
                        if ((uint)status == PDH_MORE_DATA && bufferSize > 0 && itemCount > 0)
                        {
                            IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
                            try
                            {
                                status = PdhGetFormattedCounterArray(counterHandle, PDH_FMT_DOUBLE, ref bufferSize, ref itemCount, buffer);
                                if (status == 0)
                                {
                                    int structSize = Marshal.SizeOf(typeof(PDH_FMT_COUNTERVALUE_ITEM));
                                    for (int i = 0; i < itemCount; i++)
                                    {
                                        IntPtr itemPtr = new IntPtr(buffer.ToInt64() + i * structSize);
                                        var item = Marshal.PtrToStructure<PDH_FMT_COUNTERVALUE_ITEM>(itemPtr);
                                        string instanceName = Marshal.PtrToStringUni(item.szName);
                                        float value = (float)item.value.doubleValue;
                                        Values[countersByPath.Key + ":" + instanceName] = value;
                                    }
                                }
                            }
                            finally
                            {
                                Marshal.FreeHGlobal(buffer);
                            }
                        }
                    }
                }
                
                 
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
            }
            return Values;
        }

        public void Dispose()
        {
            foreach (var path in counterHandles.Keys)
            {
                foreach (var handle in counterHandles[path])
                {
                    PdhRemoveCounter(handle);
                }
            }

            if (queryHandle != IntPtr.Zero)
                PdhCloseQuery(queryHandle);
        }

        #region PDH API imports and structures
        private const int PDH_FMT_DOUBLE = 0x00000200;
        private const uint PDH_MORE_DATA = 0x800007D2;

        [StructLayout(LayoutKind.Sequential)]
        private struct PDH_FMT_COUNTERVALUE
        {
            public uint CStatus;
            public double doubleValue;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PDH_FMT_COUNTERVALUE_ITEM
        {
            public IntPtr szName; // LPWSTR
            public PDH_FMT_COUNTERVALUE value;
        }

        [DllImport("pdh.dll", SetLastError = true)]
        private static extern int PdhOpenQuery(string dataSource, IntPtr userData, out IntPtr query);

        [DllImport("pdh.dll", SetLastError = true)]
        private static extern int PdhAddCounter(IntPtr query, string counterPath, IntPtr userData, out IntPtr counter);
        [DllImport("pdh.dll", SetLastError = true)]
        private static extern int PdhAddEnglishCounter(IntPtr query, string counterPath, IntPtr userData, out IntPtr counter);

        [DllImport("pdh.dll", SetLastError = true)]
        private static extern int PdhCollectQueryData(IntPtr query);

        [DllImport("pdh.dll", SetLastError = true)]
        private static extern int PdhGetFormattedCounterValue(IntPtr counter, int format, out uint type, out PDH_FMT_COUNTERVALUE value);

        [DllImport("pdh.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int PdhGetFormattedCounterArray(
            IntPtr hCounter,
            int dwFormat,
            ref uint lpdwBufferSize,
            ref uint lpdwItemCount,
            IntPtr ItemBuffer
        );

        [DllImport("pdh.dll", SetLastError = true)]
        private static extern int PdhRemoveCounter(IntPtr counter);

        [DllImport("pdh.dll", SetLastError = true)]
        private static extern int PdhCloseQuery(IntPtr query);


        #endregion
    }
}
