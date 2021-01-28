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
    public partial class SystemWatcherControl: UserControl
    {

        public Options Options = new Options { CounterOptions = new Dictionary<string, CounterOptions>
        {
            { "CPU", new CounterOptions { GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE } },
            { "MEM", new CounterOptions { GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE } },
            { "DISK", new CounterOptions { GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE } },
            { "NET", new CounterOptions { GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE } }
        }
        , HistorySize = 50 };        
        public int CountersCount
        {
            get
            {
                if (Counters == null) return 0;
                return Counters.Count;
            }
        }
        List<Counters.ICounter> Counters;
        System.Drawing.Font fontCounter;
        System.Drawing.Font fontCounterMin;
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

            Counters = new List<Counters.ICounter>();

            {
                var ct = new Counters.CounterCPU(Options);
                ct.Initialize();
                Counters.Add(ct);
            }
            {
                var ct = new Counters.CounterMemory(Options);
                ct.Initialize();
                Counters.Add(ct);
            }
            {
                var ct = new Counters.CounterDisk(Options);
                ct.Initialize();
                Counters.Add(ct);
            }
            {
                var ct = new Counters.CounterNetwork(Options);
                ct.Initialize();
                Counters.Add(ct);
            }

            ContextMenu cm = new ContextMenu();            
            cm.MenuItems.Add(new MenuItem("CPU: Single view", MenuItem_onClick) {  Checked = Options.CounterOptions["CPU"].GraphType == TaskbarMonitor.Counters.ICounter.CounterType.SINGLE, Name = "CPU_SINGLE"  });
            cm.MenuItems.Add(new MenuItem("CPU: Multiple cores (STACKED)", MenuItem_onClick) { Checked = Options.CounterOptions["CPU"].GraphType == TaskbarMonitor.Counters.ICounter.CounterType.STACKED, Name = "CPU_STACKED" });
            
            cm.MenuItems.Add(new MenuItem("Disk: Single view", MenuItem_onClick) { Checked = Options.CounterOptions["DISK"].GraphType == TaskbarMonitor.Counters.ICounter.CounterType.SINGLE, Name = "DISK_SINGLE" });
            cm.MenuItems.Add(new MenuItem("Disk: Read & Write (MIRRORED)", MenuItem_onClick) { Checked = Options.CounterOptions["DISK"].GraphType == TaskbarMonitor.Counters.ICounter.CounterType.MIRRORED, Name = "DISK_MIRRORED" });
            cm.MenuItems.Add(new MenuItem("Disk: Read & Write (STACKED)", MenuItem_onClick) { Checked = Options.CounterOptions["DISK"].GraphType == TaskbarMonitor.Counters.ICounter.CounterType.STACKED, Name = "DISK_STACKED" });
            
            cm.MenuItems.Add(new MenuItem("Network: Single view", MenuItem_onClick) { Checked = Options.CounterOptions["NET"].GraphType == TaskbarMonitor.Counters.ICounter.CounterType.SINGLE, Name = "NET_SINGLE" });
            cm.MenuItems.Add(new MenuItem("Network: U/D (MIRRORED)", MenuItem_onClick) { Checked = Options.CounterOptions["NET"].GraphType == TaskbarMonitor.Counters.ICounter.CounterType.MIRRORED, Name = "NET_MIRRORED" });
            cm.MenuItems.Add(new MenuItem("Network: U/D (STACKED)", MenuItem_onClick) { Checked = Options.CounterOptions["NET"].GraphType == TaskbarMonitor.Counters.ICounter.CounterType.STACKED, Name = "NET_STACKED" });
            this.ContextMenu = cm;

            defaultTheme = new GraphTheme
            {
                GraphColor = Color.FromArgb(255, 37, 84, 142),
                BarColor = Color.FromArgb(255, 176, 222, 255),
                TextColor = Color.FromArgb(200, 185, 255, 70),
                TextShadowColor = Color.FromArgb(255, 0, 0, 0),
                StackedColors = new List<Color> 
                { 
                    Color.FromArgb(255, 37, 84, 142) ,
                    Color.FromArgb(255, 65, 144, 242)
                }
            };
            defaultTheme.init();

            alternateTheme = new GraphTheme
            {
                GraphColor = Color.FromArgb(255, 59, 131, 219),
                BarColor = Color.FromArgb(255, 255, 255, 255),
                TextColor = Color.FromArgb(200, 185, 255, 70),
                TextShadowColor = Color.FromArgb(255, 0, 0, 0),
                StackedColors = new List<Color>()
            };
            alternateTheme.init();

            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.UserPaint, true);            

            InitializeComponent();
            int taskbarWidth = GetTaskbarWidth();

            int counterSize = (Options.HistorySize + 10);            
            int controlWidth = counterSize * CountersCount;
            int controlHeight = 30;

            if (taskbarWidth > 0 && taskbarWidth < controlWidth)
            {
                int countersPerLine = Convert.ToInt32(Math.Floor((float)taskbarWidth / (float)counterSize));
                controlWidth = counterSize * countersPerLine;
                controlHeight = Convert.ToInt32(Math.Ceiling((float)CountersCount / (float)countersPerLine)) * (30 + 10);                
            }
            this.Size = new Size(controlWidth, controlHeight);


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
            fontCounterMin = new Font("Helvetica", fontSize-1, FontStyle.Bold);
            fontTitle = new Font("Arial", fontSize, FontStyle.Bold);

            
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
            int graphPositionY = 0;

            System.Drawing.SolidBrush brushShadow = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 0, 0, 0));
            System.Drawing.SolidBrush brushTitle = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(!mouseOver ? 40 : 255, 255, 255, 255));

            System.Drawing.Graphics formGraphics = e.Graphics;// this.CreateGraphics();

            Action<int, int, int, bool, bool, bool, TaskbarMonitor.Counters.CounterInfo, GraphTheme > drawGraph = (x, y, maxH, invertido, showText, textOnTop,info, theme) =>
            {
                var pos = maxH - ((info.CurrentValue * maxH) / info.MaximumValue);
                if (pos > Int32.MaxValue) pos = Int32.MaxValue;
                int posInt = Convert.ToInt32(pos) + y;

                var height = (info.CurrentValue * maxH) / info.MaximumValue;
                if (height > Int32.MaxValue) height = Int32.MaxValue;
                int heightInt = Convert.ToInt32(height);
                if (invertido)
                    formGraphics.FillRectangle(theme.BrushBar, new Rectangle(x + Options.HistorySize, maxH, 4, heightInt));
                else
                    formGraphics.FillRectangle(theme.BrushBar, new Rectangle(x + Options.HistorySize, posInt, 4, heightInt));

                var initialGraphPosition = x + Options.HistorySize - info.History.Count;
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

                
                if (showText)
                {
                    string text = info.StringValue;
                    if (info.Name != "default")
                        text = info.Name + ": " + text;
                    var sizeString = formGraphics.MeasureString(text, fontCounter);
                    int offset = invertido ? 2 : -2;
                    float ypos = textOnTop ? 1 + y : (maxH / 2.0f) - (sizeString.Height / 2) + 1 + y + offset;
                    Font font = maxH > 20 ? fontCounter : fontCounterMin;
                    formGraphics.DrawString(text, font, theme.BrushTextShadow, new RectangleF(x + (Options.HistorySize / 2) - (sizeString.Width / 2) + 1, ypos + 1, sizeString.Width, maxH), new StringFormat());
                    formGraphics.DrawString(text, font, theme.BrushText, new RectangleF(x + (Options.HistorySize / 2) - (sizeString.Width / 2), ypos, sizeString.Width, maxH), new StringFormat());
                }
            };
             
            Action<int, int, int, bool, bool, List<TaskbarMonitor.Counters.CounterInfo>, GraphTheme> drawStackedGraph = (x, y, maxH, showText, textOnTop, infos, theme) =>
            {
                float absMax = 0;
                List<float> lastValue = new List<float>();
                
                // accumulate values for stacked effect
                List<List<float>> values = new List<List<float>>();
                foreach (var info in infos.AsEnumerable().Reverse())
                {
                    absMax += info.MaximumValue;
                    var value = new List<float>();
                    int z = 0;
                    foreach (var item in info.History)
                    {
                        value.Add(item + (lastValue.Count > 0 ? lastValue.ElementAt(z) : 0));
                        z++;
                    }
                    values.Add(value);
                    lastValue = value;
                }
                var historySize = values.Count > 0 ? values[0].Count : 0;
                // now we draw it

                var colors = theme.GetColorGradient(theme.StackedColors[0], theme.StackedColors[1], values.Count);
                int w = 0;
                values.Reverse();
                foreach (var info in values)
                {
                    float currentValue = info.Count > 0 ? info.Last() : 0;
                     var pos = maxH - ((currentValue * maxH) / absMax);
                     if (pos > Int32.MaxValue) pos = Int32.MaxValue;
                     int posInt = Convert.ToInt32(pos) + y;

                     var height = (currentValue * maxH) / absMax;
                     if (height > Int32.MaxValue) height = Int32.MaxValue;
                     int heightInt = Convert.ToInt32(height);


                     formGraphics.FillRectangle(theme.BrushBar, new Rectangle(x + Options.HistorySize, posInt, 4, heightInt));
                    
                    int i = 0;
                    var initialGraphPosition = x + Options.HistorySize - historySize;
                    Point[] points = new Point[historySize + 2];
                    foreach (var item in info)
                    {
                        var heightItem = (item * maxH) / absMax;
                        if (heightItem > Int32.MaxValue) heightItem = Int32.MaxValue;
                        var convertido = Convert.ToInt32(heightItem);

                        points[i] = new Point(initialGraphPosition + i, maxH - convertido + y);
                        i++;
                    }
                    points[i] = new Point(initialGraphPosition + i, maxH + y);
                    points[i + 1] = new Point(initialGraphPosition, maxH + y);
                    
                    Brush brush = new SolidBrush(colors.ElementAt(w));
                    w++;
                    formGraphics.FillPolygon(brush, points);
                     

                }
                 

                if (showText)
                {
                    string text = infos[0].StringValue;                    
                    var sizeString = formGraphics.MeasureString(text, fontCounter);
                    int offset = -2;
                    float ypos = textOnTop ? 1 + y : (maxH / 2.0f) - (sizeString.Height / 2) + 1 + y + offset;
                    Font font = maxH > 20 ? fontCounter : fontCounterMin;
                    formGraphics.DrawString(text, font, theme.BrushTextShadow, new RectangleF(x + (Options.HistorySize / 2) - (sizeString.Width / 2) + 1, ypos + 1, sizeString.Width, maxH), new StringFormat());
                    formGraphics.DrawString(text, font, theme.BrushText, new RectangleF(x + (Options.HistorySize / 2) - (sizeString.Width / 2), ypos, sizeString.Width, maxH), new StringFormat());
                }
            };
             
            foreach (var ct in Counters)
            {
                var infos = ct.GetValues();
                

                if (ct.GetCounterType() == TaskbarMonitor.Counters.ICounter.CounterType.SINGLE)
                {
                    drawGraph(graphPosition, 0 + graphPositionY, maximumHeight, false, true, true, infos[0], defaultTheme);
                                         
                    var sizeTitle = formGraphics.MeasureString(ct.GetName(), fontTitle);
                    if (mouseOver)
                        formGraphics.DrawString(ct.GetName(), fontTitle, brushShadow, new RectangleF(graphPosition + (Options.HistorySize / 2) - (sizeTitle.Width / 2) + 1, (maximumHeight / 2 - sizeTitle.Height / 2) + 2 + graphPositionY, sizeTitle.Width, maximumHeight), new StringFormat());
                    formGraphics.DrawString(ct.GetName(), fontTitle, brushTitle, new RectangleF(graphPosition + (Options.HistorySize / 2) - (sizeTitle.Width / 2), (maximumHeight / 2 - sizeTitle.Height / 2) + 1 + graphPositionY, sizeTitle.Width, maximumHeight), new StringFormat());
                }
                else if (ct.GetCounterType() == TaskbarMonitor.Counters.ICounter.CounterType.MIRRORED)
                {
                    for (int z = 0; z < infos.Count; z++)
                    {
                        drawGraph(graphPosition, z * (maximumHeight / 2) + graphPositionY, maximumHeight / 2, z == 1, true, false, infos[z], z == 0 ? defaultTheme : alternateTheme);
                    }

                    var sizeTitle = formGraphics.MeasureString(ct.GetName(), fontTitle);
                    if (mouseOver)
                        formGraphics.DrawString(ct.GetName(), fontTitle, brushShadow, new RectangleF(graphPosition + (Options.HistorySize / 2) - (sizeTitle.Width / 2) + 1, (maximumHeight/ 2  - sizeTitle.Height / 2) + 2 + graphPositionY, sizeTitle.Width, maximumHeight), new StringFormat());
                    formGraphics.DrawString(ct.GetName(), fontTitle, brushTitle, new RectangleF(graphPosition + (Options.HistorySize / 2) - (sizeTitle.Width / 2), (maximumHeight /2 - sizeTitle.Height / 2) + 1 + graphPositionY, sizeTitle.Width, maximumHeight), new StringFormat());
                }
                else if (ct.GetCounterType() == TaskbarMonitor.Counters.ICounter.CounterType.STACKED)
                {                    
                    drawStackedGraph(graphPosition, 0 + graphPositionY, maximumHeight, true, true, infos, defaultTheme);                    

                    var sizeTitle = formGraphics.MeasureString(ct.GetName(), fontTitle);
                    if (mouseOver)
                        formGraphics.DrawString(ct.GetName(), fontTitle, brushShadow, new RectangleF(graphPosition + (Options.HistorySize / 2) - (sizeTitle.Width / 2) + 1, (maximumHeight / 2 - sizeTitle.Height / 2) + 2 + graphPositionY, sizeTitle.Width, maximumHeight), new StringFormat());
                    formGraphics.DrawString(ct.GetName(), fontTitle, brushTitle, new RectangleF(graphPosition + (Options.HistorySize / 2) - (sizeTitle.Width / 2), (maximumHeight / 2 - sizeTitle.Height / 2) + 1 + graphPositionY, sizeTitle.Width, maximumHeight), new StringFormat());
                }

                graphPosition += Options.HistorySize + 10;
                if(graphPosition >= this.Size.Width)
                {
                    graphPosition = 0;
                    graphPositionY += (maximumHeight + 10 );
                }
            }
            
            
       
            brushShadow.Dispose();
            brushTitle.Dispose();
        }
        public static int GetTaskbarWidth()
        {
            return Screen.PrimaryScreen.Bounds.Width - Screen.PrimaryScreen.WorkingArea.Width;
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
            if(menu.Name == "CPU_SINGLE")
            {                
                ContextMenu.MenuItems["CPU_SINGLE"].Checked = true;
                ContextMenu.MenuItems["CPU_STACKED"].Checked = false;
            }
            else if (menu.Name == "CPU_STACKED")
            {
                ContextMenu.MenuItems["CPU_SINGLE"].Checked = false;
                ContextMenu.MenuItems["CPU_STACKED"].Checked = true;
            }

            if (menu.Name == "DISK_SINGLE")
            {
                ContextMenu.MenuItems["DISK_SINGLE"].Checked = true;
                ContextMenu.MenuItems["DISK_MIRRORED"].Checked = false;
                ContextMenu.MenuItems["DISK_STACKED"].Checked = false;
            }
            else if (menu.Name == "DISK_MIRRORED")
            {
                ContextMenu.MenuItems["DISK_SINGLE"].Checked = false;
                ContextMenu.MenuItems["DISK_MIRRORED"].Checked = true;
                ContextMenu.MenuItems["DISK_STACKED"].Checked = false;
            }
            else if (menu.Name == "DISK_STACKED")
            {
                ContextMenu.MenuItems["DISK_SINGLE"].Checked = false;
                ContextMenu.MenuItems["DISK_MIRRORED"].Checked = false;
                ContextMenu.MenuItems["DISK_STACKED"].Checked = true;
            }

            if (menu.Name == "NET_SINGLE")
            {
                ContextMenu.MenuItems["NET_SINGLE"].Checked = true;
                ContextMenu.MenuItems["NET_MIRRORED"].Checked = false;
                ContextMenu.MenuItems["NET_STACKED"].Checked = false;
            }
            else if (menu.Name == "NET_MIRRORED")
            {
                ContextMenu.MenuItems["NET_SINGLE"].Checked = false;
                ContextMenu.MenuItems["NET_MIRRORED"].Checked = true;
                ContextMenu.MenuItems["NET_STACKED"].Checked = false;
            }
            else if (menu.Name == "NET_STACKED")
            {
                ContextMenu.MenuItems["NET_SINGLE"].Checked = false;
                ContextMenu.MenuItems["NET_MIRRORED"].Checked = false;
                ContextMenu.MenuItems["NET_STACKED"].Checked = true;
            }


            Options.CounterOptions["CPU"].GraphType = ContextMenu.MenuItems["CPU_SINGLE"].Checked ? TaskbarMonitor.Counters.ICounter.CounterType.SINGLE : TaskbarMonitor.Counters.ICounter.CounterType.STACKED;
            
            if(ContextMenu.MenuItems["DISK_SINGLE"].Checked)
                Options.CounterOptions["DISK"].GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE;
            else if (ContextMenu.MenuItems["DISK_MIRRORED"].Checked)
                Options.CounterOptions["DISK"].GraphType = TaskbarMonitor.Counters.ICounter.CounterType.MIRRORED;
            else if (ContextMenu.MenuItems["DISK_STACKED"].Checked)
                Options.CounterOptions["DISK"].GraphType = TaskbarMonitor.Counters.ICounter.CounterType.STACKED;

            if (ContextMenu.MenuItems["NET_SINGLE"].Checked)
                Options.CounterOptions["NET"].GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE;
            else if (ContextMenu.MenuItems["NET_MIRRORED"].Checked)
                Options.CounterOptions["NET"].GraphType = TaskbarMonitor.Counters.ICounter.CounterType.MIRRORED;
            else if (ContextMenu.MenuItems["NET_STACKED"].Checked)
                Options.CounterOptions["NET"].GraphType = TaskbarMonitor.Counters.ICounter.CounterType.STACKED;
            
            Invalidate();
        }

         
    }

    public class Options
    {
        public Dictionary<string, CounterOptions> CounterOptions { get; set; }         
        public int HistorySize { get; set; }
        public bool ShowTitle { get; set; }
        public bool ShowValue { get; set; }
        // themes
    }

    public class CounterOptions
    {
        public TaskbarMonitor.Counters.ICounter.CounterType GraphType { get; set; }
    }
    public class GraphTheme : IDisposable
    {
        public Color GraphColor { get; set; }
        public Color BarColor { get; set; }
        public Color TextColor { get; set; }
        public Color TextShadowColor { get; set; }

        public List<Color> StackedColors { get; set; }

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

        public IEnumerable<Color> GetColorGradient(Color from, Color to, int totalNumberOfColors)
        {
            if (totalNumberOfColors < 2)
            {
                throw new ArgumentException("Gradient cannot have less than two colors.", nameof(totalNumberOfColors));
            }

            double diffA = to.A - from.A;
            double diffR = to.R - from.R;
            double diffG = to.G - from.G;
            double diffB = to.B - from.B;

            var steps = totalNumberOfColors - 1;

            var stepA = diffA / steps;
            var stepR = diffR / steps;
            var stepG = diffG / steps;
            var stepB = diffB / steps;

            yield return from;

            for (var i = 1; i < steps; ++i)
            {
                yield return Color.FromArgb(
                    c(from.A, stepA),
                    c(from.R, stepR),
                    c(from.G, stepG),
                    c(from.B, stepB));

                int c(int fromC, double stepC)
                {
                    return (int)Math.Round(fromC + stepC * i);
                }
            }

            yield return to;
        }

        public void Dispose()
        {
            BrushGraph.Dispose();
            BrushBar.Dispose();
            BrushText.Dispose();
            BrushTextShadow.Dispose();
             
        }
    }


}
