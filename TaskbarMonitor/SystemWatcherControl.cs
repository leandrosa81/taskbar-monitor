using System;
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

namespace TaskbarMonitor
{
    public class Options
    {
        public bool CPUSingleView { get; set; }
        public bool DiskSingleView { get; set; }
    }
    public class GraphTheme : IDisposable
    {
        public Color GraphColor { get; set; }
        public Color BarColor { get; set; }
        public Color TextColor { get; set; }
        public Color TextShadowColor { get; set; }

        public SolidBrush BrushGraph { get; private set; }
        public SolidBrush BrushBar { get; private set; }
        public SolidBrush BrushText { get; private set; }
        public SolidBrush BrushTextShadow { get; private set; }

        public void init()
        {
            BrushGraph = new System.Drawing.SolidBrush(GraphColor);
            BrushBar = new System.Drawing.SolidBrush(BarColor);
            BrushText = new System.Drawing.SolidBrush(TextColor);
            BrushTextShadow = new System.Drawing.SolidBrush(TextShadowColor);            
        }

        public void Dispose()
        {
            BrushGraph.Dispose();
            BrushBar.Dispose();
            BrushText.Dispose();
            BrushTextShadow.Dispose();
        }
    }
    public partial class SystemWatcherControl: UserControl
    {
        
        Options options = new Options { CPUSingleView = true, DiskSingleView = true } ;
        List<Counters.ICounter> Counters;
        System.Drawing.Font fontCounter;
        Font fontTitle;
        int lastSize = 30;
        bool mouseOver = false;
        GraphTheme defaultTheme;
        GraphTheme alternateTheme;

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
            ContextMenu cm = new ContextMenu();            
            cm.MenuItems.Add(new MenuItem("CPU: Single view", MenuItem_onClick) {  Checked = options.CPUSingleView, Name = "CPUSingleViewEnable"  });
            cm.MenuItems.Add(new MenuItem("CPU: Multiple cores", MenuItem_onClick) { Checked = !options.CPUSingleView, Name = "CPUSingleViewDisable" });
            cm.MenuItems.Add(new MenuItem("Disk: Single view", MenuItem_onClick) { Checked = options.DiskSingleView, Name = "DiskSingleViewEnable" });
            cm.MenuItems.Add(new MenuItem("Disk: Separate Read & Write", MenuItem_onClick) { Checked = !options.DiskSingleView, Name = "DiskSingleViewDisable" });            
            this.ContextMenu = cm;

            defaultTheme = new GraphTheme
            {
                GraphColor = Color.FromArgb(255, 37, 84, 142),
                BarColor = Color.FromArgb(255, 176, 222, 255),
                TextColor = Color.FromArgb(170, 185, 255, 70),
                TextShadowColor = Color.FromArgb(255, 0, 0, 0)
            };
            defaultTheme.init();

            alternateTheme = new GraphTheme
            {
                GraphColor = Color.FromArgb(255, 176, 222, 255),
                BarColor = Color.FromArgb(255, 255, 255, 255),
                TextColor = Color.FromArgb(170, 185, 255, 70),
                TextShadowColor = Color.FromArgb(255, 0, 0, 0)
            };
            alternateTheme.init();

            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.UserPaint, true);

            InitializeComponent();
           

            float dpiX, dpiY;
            using (Graphics graphics = this.CreateGraphics())
            {
                dpiX = graphics.DpiX;
                dpiY = graphics.DpiY;
            }
            float fontSize = 7f;
            if (dpiX > 96)
                fontSize = 5f;

            fontCounter = new Font("Helvetica", fontSize, FontStyle.Bold);
            fontTitle = new Font("Arial", fontSize, FontStyle.Bold);

            Counters = new List<Counters.ICounter>();

            {
                var ct = new Counters.CounterCPU(options);
                ct.Initialize();
                Counters.Add(ct);
            }
            {
                var ct = new Counters.CounterMemory(options);
                ct.Initialize();
                Counters.Add(ct);
            }
            {
                var ct = new Counters.CounterDisk(options);
                ct.Initialize();
                Counters.Add(ct);
            }
            {
                var ct = new Counters.CounterNetwork(options);
                ct.Initialize();
                Counters.Add(ct);
            }            
        }
       
 
        private void timer1_Tick(object sender, EventArgs e)
        {            
            foreach (var ct in Counters)
            {
                ct.Update();
            }

            this.Invalidate();
        }

        private void SystemWatcherControl_Paint(object sender, PaintEventArgs e)
        {
            int maximumHeight = GetTaskbarHeight();
            if (maximumHeight <= 0)
                maximumHeight = 30;

            if(lastSize  != maximumHeight)
            {
                this.Height = maximumHeight;
                lastSize = maximumHeight;
            }

            int graphPosition = 0;
            
            System.Drawing.SolidBrush brushBlue = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 37, 84, 142));
            System.Drawing.SolidBrush brushRed = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 176, 222, 255));
            System.Drawing.SolidBrush brushWhite = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(170, 185, 255, 70));

            System.Drawing.SolidBrush brushShadow = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 0, 0, 0));
            System.Drawing.SolidBrush brushTitle = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(!mouseOver ? 40 : 255, 255, 255, 255));

            System.Drawing.Graphics formGraphics = e.Graphics;// this.CreateGraphics();

            Action<int, int, int, bool, TaskbarMonitor.Counters.CounterInfo, GraphTheme > drawGraph = (x, y, maxH, invertido, info, theme) =>
            {
                var pos = maxH - ((info.CurrentValue * maxH) / info.MaximumValue);
                if (pos > Int32.MaxValue) pos = Int32.MaxValue;
                int posInt = Convert.ToInt32(pos) + y;

                var height = (info.CurrentValue * maxH) / info.MaximumValue;
                if (height > Int32.MaxValue) height = Int32.MaxValue;
                int heightInt = Convert.ToInt32(height);
                if (invertido)
                    formGraphics.FillRectangle(theme.BrushBar, new Rectangle(x + 40, maxH, 5, heightInt));
                else
                    formGraphics.FillRectangle(theme.BrushBar, new Rectangle(x + 40, posInt, 5, heightInt));

                var initialGraphPosition = x + 40 - info.History.Count;
                Point[] points = new Point[info.History.Count + 2];
                int i = 0;
                int inverter = invertido ? -1 : 1;
                foreach (var item in info.History)
                {
                    var heightItem = (item * maxH) / info.MaximumValue;
                    if (heightItem > Int32.MaxValue) height = Int32.MaxValue;
                    var convertido = Convert.ToInt32(heightItem);

                    
                    if(invertido)
                        points[i] = new Point(initialGraphPosition + i, 0 + convertido + y);
                    else
                        points[i] = new Point(initialGraphPosition + i, maxH - convertido + y);
                    i++;
                }
                if (invertido)
                {
                    points[i] = new Point(initialGraphPosition + i, 0 + y);
                    points[i + 1] = new Point(initialGraphPosition, 0 + y);
                }
                else
                {
                    points[i] = new Point(initialGraphPosition + i, maxH + y);
                    points[i + 1] = new Point(initialGraphPosition, maxH + y);
                }
                formGraphics.FillPolygon(theme.BrushGraph, points);

                
                if (mouseOver)
                {
                    string text = info.Name + ": " + info.StringValue;
                    var sizeString = formGraphics.MeasureString(text, fontCounter);
                    formGraphics.DrawString(text, fontCounter, theme.BrushTextShadow, new RectangleF(x + 25 - (sizeString.Width / 2) + 1, (maxH / 2.0f) - (sizeString.Height / 2) + 1 + y, sizeString.Width, maxH), new StringFormat());
                    formGraphics.DrawString(text, fontCounter, theme.BrushText, new RectangleF(x + 25 - (sizeString.Width / 2), (maxH / 2.0f) - (sizeString.Height / 2) + y, sizeString.Width, maxH), new StringFormat());
                }
            };
            foreach (var ct in Counters)
            {
                var infos = ct.GetValues();
                

                if (ct.GetCounterType() == TaskbarMonitor.Counters.ICounter.CounterType.SINGLE)
                {
                    var lista = infos[0].History;
                    var currentValue = infos[0].CurrentValue;
                    var max = infos[0].MaximumValue;
                    var stringValue = infos[0].StringValue;
                    

                    var pos = maximumHeight - ((currentValue * maximumHeight) / max);
                    if (pos > Int32.MaxValue) pos = Int32.MaxValue;
                    int posInt = Convert.ToInt32(pos);

                    var height = (currentValue * maximumHeight) / max;
                    if (height > Int32.MaxValue) height = Int32.MaxValue;
                    int heightInt = Convert.ToInt32(height);
                    formGraphics.FillRectangle(brushRed, new Rectangle(graphPosition + 40, posInt, 5, heightInt));

                    var initialGraphPosition = graphPosition + 40 - lista.Count;
                    Point[] points = new Point[lista.Count + 2];
                    int i = 0;
                    foreach (var item in lista)
                    {
                        var heightItem = (item * maximumHeight) / max;
                        if (heightItem > Int32.MaxValue) height = Int32.MaxValue;
                        var convertido = Convert.ToInt32(heightItem);

                        points[i] = new Point(initialGraphPosition + i, maximumHeight - convertido);
                        i++;
                    }
                    points[i] = new Point(initialGraphPosition + i, maximumHeight);
                    points[i + 1] = new Point(initialGraphPosition, maximumHeight);
                    formGraphics.FillPolygon(brushBlue, points);


                    var sizeString = formGraphics.MeasureString(stringValue, fontCounter);
                    formGraphics.DrawString(stringValue, fontCounter, brushShadow, new RectangleF(graphPosition + 25 - (sizeString.Width / 2) + 1, (maximumHeight / 2.0f) - (sizeString.Height / 2) + 1, sizeString.Width, maximumHeight), new StringFormat());
                    formGraphics.DrawString(stringValue, fontCounter, brushWhite, new RectangleF(graphPosition + 25 - (sizeString.Width / 2), (maximumHeight / 2.0f) - (sizeString.Height / 2), sizeString.Width, maximumHeight), new StringFormat());


                    var sizeTitle = formGraphics.MeasureString(ct.GetName(), fontTitle);
                    if (mouseOver)
                        formGraphics.DrawString(ct.GetName(), fontTitle, brushShadow, new RectangleF(graphPosition + 25 - (sizeTitle.Width / 2) + 1, (maximumHeight - sizeTitle.Height), sizeTitle.Width, maximumHeight), new StringFormat());
                    formGraphics.DrawString(ct.GetName(), fontTitle, brushTitle, new RectangleF(graphPosition + 25 - (sizeTitle.Width / 2), (maximumHeight - sizeTitle.Height) - 1, sizeTitle.Width, maximumHeight), new StringFormat());
                }
                else if (ct.GetCounterType() == TaskbarMonitor.Counters.ICounter.CounterType.MIRRORED)
                {
                    for (int z = 0; z < infos.Count; z++)
                    {
                        drawGraph(graphPosition, z * (maximumHeight / 2), maximumHeight / 2, z == 1, infos[z], z == 0 ? defaultTheme : alternateTheme);
                    }

                    var sizeTitle = formGraphics.MeasureString(ct.GetName(), fontTitle);
                    if (mouseOver)
                        formGraphics.DrawString(ct.GetName(), fontTitle, brushShadow, new RectangleF(graphPosition + 25 - (sizeTitle.Width / 2) + 1, (maximumHeight/ 2  - sizeTitle.Height / 2) + 2, sizeTitle.Width, maximumHeight), new StringFormat());
                    formGraphics.DrawString(ct.GetName(), fontTitle, brushTitle, new RectangleF(graphPosition + 25 - (sizeTitle.Width / 2), (maximumHeight /2 - sizeTitle.Height / 2) + 1, sizeTitle.Width, maximumHeight), new StringFormat());
                }

                graphPosition += 50;
            }
            
            
            brushBlue.Dispose();
            brushRed.Dispose();
            brushWhite.Dispose();
            brushShadow.Dispose();
            brushTitle.Dispose();
        }
        public static int GetTaskbarHeight()
        {
            return Screen.PrimaryScreen.Bounds.Height - Screen.PrimaryScreen.WorkingArea.Height;
        }

        private void SystemWatcherControl_MouseEnter(object sender, EventArgs e)
        {
            mouseOver = true;

            this.Invalidate();
        }

        private void SystemWatcherControl_MouseLeave(object sender, EventArgs e)
        {
            mouseOver = false;
            this.Invalidate();
        }

        public void MenuItem_onClick(object sender, EventArgs e)
        {
            MenuItem menu = (MenuItem)sender;
            if(menu.Name == "CPUSingleViewEnable" && !menu.Checked)
            {
                menu.Checked = true;                
                ContextMenu.MenuItems["CPUSingleViewDisable"].Checked = false;
            }
            else if (menu.Name == "CPUSingleViewDisable" && !menu.Checked)
            {
                menu.Checked = true;
                ContextMenu.MenuItems["CPUSingleViewEnable"].Checked = false;
            }


            if (menu.Name == "DiskSingleViewEnable" && !menu.Checked)
            {
                menu.Checked = true;
                ContextMenu.MenuItems["DiskSingleViewDisable"].Checked = false;
            }
            else if (menu.Name == "DiskSingleViewDisable" && !menu.Checked)
            {
                menu.Checked = true;
                ContextMenu.MenuItems["DiskSingleViewEnable"].Checked = false;
            }

            options.CPUSingleView = ContextMenu.MenuItems["CPUSingleViewEnable"].Checked;
            options.DiskSingleView = ContextMenu.MenuItems["DiskSingleViewEnable"].Checked;
            Invalidate();
        }

         
    }

    
}
