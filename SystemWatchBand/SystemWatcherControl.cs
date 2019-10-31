﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//using CSDeskBand.Win;
using CSDeskBand;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace SystemWatchBand
{    
    public partial class SystemWatcherControl: UserControl
    {
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

        PerformanceCounter cpuCounter;
        PerformanceCounter ramCounter;
        PerformanceCounter diskReadCounter;
        PerformanceCounter diskWriteCounter;
        long totalMemory = 0;
        long totalIO = 1;
        float cpu = 0;
        float mem = 0;
        float diskRead = 0;
        float diskWrite = 0;
        List<int> cpus = new List<int>();
        List<int> mems = new List<int>();
        List<int> disks = new List<int>();        

        public SystemWatcherControl(CSDeskBand.CSDeskBandWin w)
        {
            Initialize();
        }
        public SystemWatcherControl()
        {
            Initialize();
        }
        private void Initialize()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.UserPaint, true);

            InitializeComponent();

            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
            diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
            GetPhysicallyInstalledSystemMemory(out totalMemory);
            totalMemory = totalMemory / 1024;
        }
       
 
        private void timer1_Tick(object sender, EventArgs e)
        {
            cpu = cpuCounter.NextValue();
            mem = totalMemory - ramCounter.NextValue();
            diskRead = diskReadCounter.NextValue();            
            diskWrite = diskWriteCounter.NextValue();
            if (diskRead + diskWrite > totalIO)
                totalIO = Convert.ToInt64(diskRead + diskWrite);


            cpus.Add(Convert.ToInt32((cpu * 30.0f) / 100.0f));
            if (cpus.Count > 30) cpus.RemoveAt(0);
            mems.Add(Convert.ToInt32((mem * 30.0f) / totalMemory));
            if (mems.Count > 30) mems.RemoveAt(0);
            disks.Add(Convert.ToInt32(((diskRead + diskWrite) * 30.0f) / totalIO));
            if (disks.Count > 30) disks.RemoveAt(0);
             

            this.Invalidate();
        }

        private void SystemWatcherControl_Paint(object sender, PaintEventArgs e)
        {
            int alturaMaxima = 30;
            int posGraficoCPU = 0;
            int posGraficoMem = 50;
            int posGraficoDisk = 100;
            int posCPU = 32;
            int posMem = 82;
            int posDisk = 132;
             
            System.Drawing.SolidBrush brushBlue = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 37, 84, 142));
            System.Drawing.SolidBrush brushRed = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 176, 222, 255));
            System.Drawing.SolidBrush brushWhite = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(170, 185, 255, 70));

            System.Drawing.Graphics formGraphics = e.Graphics;// this.CreateGraphics();

            formGraphics.FillRectangle(brushRed, new Rectangle(posCPU, Convert.ToInt32(alturaMaxima - ((cpu * 30.0f) / 100.0f)), 5, Convert.ToInt32((cpu * 30.0f) / 100.0f)));
            formGraphics.FillRectangle(brushRed, new Rectangle(posMem, Convert.ToInt32(alturaMaxima - ((mem * 30.0f) / totalMemory)), 5, Convert.ToInt32((mem * 30.0f) / totalMemory)));
            formGraphics.FillRectangle(brushRed, new Rectangle(posDisk, Convert.ToInt32(alturaMaxima - (((diskRead+diskWrite) * 30.0f) / totalIO)), 5, Convert.ToInt32(((diskRead + diskWrite) * 30.0f) / totalIO)));

            {
                posGraficoCPU += (30 - cpus.Count);
                Point[] points = new Point[cpus.Count + 2];
                int i = 0;
                foreach (var item in cpus)
                {
                    points[i] = new Point(posGraficoCPU + i, Convert.ToInt32(alturaMaxima - item));
                    i++;
                }
                points[i] = new Point(posGraficoCPU + i, 30);
                points[i + 1] = new Point(posGraficoCPU, 30);
                formGraphics.FillPolygon(brushBlue, points);
            }

            {
                posGraficoMem += (30 - mems.Count);
                Point[] points = new Point[cpus.Count + 2];
                int i = 0;
                foreach (var item in mems)
                {
                    points[i] = new Point(posGraficoMem + i, Convert.ToInt32(alturaMaxima - item));
                    i++;
                }
                points[i] = new Point(posGraficoMem + i, 30);
                points[i + 1] = new Point(posGraficoMem, 30);
                formGraphics.FillPolygon(brushBlue, points);
            }
            {
                posGraficoDisk += (30 - disks.Count);
                Point[] points = new Point[disks.Count + 2];
                int i = 0;
                foreach (var item in disks)
                {
                    points[i] = new Point(posGraficoDisk + i, Convert.ToInt32(alturaMaxima - item));
                    i++;
                }
                points[i] = new Point(posGraficoDisk + i, 30);
                points[i + 1] = new Point(posGraficoDisk, 30);
                formGraphics.FillPolygon(brushBlue, points);
            }
            System.Drawing.Font font = new Font("Helvetica", 7f, FontStyle.Bold);
            formGraphics.DrawString(cpu.ToString("0") + "%", font, brushWhite, new RectangleF(2, 10, 50, alturaMaxima), new StringFormat());
            formGraphics.DrawString((mem / 1024).ToString("0.0") + "GB", font, brushWhite, new RectangleF(45, 10, 50, alturaMaxima), new StringFormat());
            formGraphics.DrawString(((diskRead+diskWrite) / 1024 / 1024).ToString("0.0") + "MB/s", font, brushWhite, new RectangleF(95, 10, 60, alturaMaxima), new StringFormat());

            brushBlue.Dispose();
            brushRed.Dispose();
            brushWhite.Dispose();
        }
    }

    
}
