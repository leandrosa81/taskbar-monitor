using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

// control architecture

// Deskband
//      Options (class holding all options loaded from disk)
//      SystemWatcherControl(Options) (main control that displays graph and has context menu)
//      Settings dialog window (receives copy of options)
//          SystemWatcherControl(CopyOfOptions) (another instance for preview)        
namespace TaskbarMonitor
{
    public partial class SystemWatcherControl : UserControl
    {
        public delegate void SizeChangeHandler(Size size);
        public event SizeChangeHandler OnChangeSize;
        public Version Version { get; set; } = new Version(Properties.Resources.Version);
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Options Options { get; set; }

        private bool _previewMode = false;
        private ContextMenu _contextMenu = null;
        private bool VerticalTaskbarMode = false;
        private System.Timers.Timer pollingTimer;
        public bool PreviewMode
        {
            get
            {
                return _previewMode;
            }
            set
            {
                _previewMode = value;
                this.ContextMenu = _previewMode ? null : _contextMenu;
            }
        }
        public int CountersCount
        {
            get
            {
                if (Counters == null) return 0;
                return Options.CounterOptions.Where(x => x.Value.Enabled == true).Count();
                //return Counters.Count;
            }
        }
        List<Counters.ICounter> Counters;
        System.Drawing.Font fontCounter;
        Font fontTitle;
        int lastSize = 30;
        bool mouseOver = false;
        GraphTheme customTheme;
        GraphTheme darkTheme;
        GraphTheme lightTheme;

        GraphTheme defaultTheme;

        public SystemWatcherControl(Options opt, bool addSecondControl = false)//CSDeskBand.CSDeskBandWin w, 
        {
            try
            {
                darkTheme = GraphTheme.DefaultDarkTheme();            
                lightTheme = GraphTheme.DefaultLightTheme();
                customTheme = GraphTheme.ReadFromDisk();
                opt.Upgrade(customTheme);

                Initialize(opt);
                if (addSecondControl)
                {
                    SecondaryTaskBar sTask = new SecondaryTaskBar();
                    sTask.AddControl();
                }                
                this.BackColor = Color.Transparent;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading SystemWatcherControl: {ex.Message}", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public SystemWatcherControl()
            :this(true)
        {            
        }
        public SystemWatcherControl(bool addSecondControl = false)
        {
            try
            {
                Options opt = TaskbarMonitor.Options.ReadFromDisk();
                darkTheme = GraphTheme.DefaultDarkTheme();
                lightTheme = GraphTheme.DefaultLightTheme();
                customTheme = GraphTheme.ReadFromDisk();
                opt.Upgrade(customTheme);

                Initialize(opt);
                if (addSecondControl)
                {
                    SecondaryTaskBar sTask = new SecondaryTaskBar();
                    sTask.AddControl();
                }                
                this.BackColor = Color.Transparent;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading SystemWatcherControl: {ex.Message}", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private GraphTheme GetTheme(Options opt)
        {
            GraphTheme theme = darkTheme;

            if (opt.ThemeType == Options.ThemeList.LIGHT)
                theme = lightTheme;
            else if (opt.ThemeType == Options.ThemeList.CUSTOM)
                theme = customTheme;
            else if (opt.ThemeType == Options.ThemeList.AUTOMATIC)
            {
                Color taskBarColour = BLL.Win32Api.GetColourAt(BLL.Win32Api.GetTaskbarPosition().Location);
                if (taskBarColour.R + taskBarColour.G + taskBarColour.B > 382)
                    theme = lightTheme;
                else
                    theme = darkTheme;
            }
            return theme;
        }

        private void PollingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            UpdateGraphs();

            if (this.Options.ThemeType == Options.ThemeList.AUTOMATIC)
            {
                this.defaultTheme = GetTheme(this.Options);
            }

            this.Invalidate();
        }

        public bool IsCustomTheme()
        {
            return GraphTheme.IsCustom(this.defaultTheme);
        }
        public void ApplyOptions(Options Options)
        {
            ApplyOptions(Options, GetTheme(Options));
        }

        public void ApplyOptions(Options Options, GraphTheme theme)
        {
            this.Options = Options;
            this.defaultTheme = theme;

            fontTitle = new Font(defaultTheme.TitleFont, defaultTheme.TitleSize, defaultTheme.TitleFontStyle);
            fontCounter = new Font(defaultTheme.CurrentValueFont, defaultTheme.CurrentValueSize, defaultTheme.CurrentValueFontStyle);

            if (!PreviewMode)
            {
                _contextMenu = new ContextMenu();
                _contextMenu.MenuItems.Add(new MenuItem("Settings...", MenuItem_Settings_onClick));
                _contextMenu.MenuItems.Add(new MenuItem("Open Resource Monitor...", (e, a) =>
                {
                    System.Diagnostics.Process.Start("resmon.exe");
                }));
                _contextMenu.MenuItems.Add(new MenuItem(String.Format("About taskbar-monitor (v{0})...", Version.ToString(3)), MenuItem_About_onClick));
                this.ContextMenu = _contextMenu;
            }


            if (PreviewMode)
            {
                var pos = BLL.Win32Api.GetTaskbarPosition();
                Color taskBarColour = BLL.Win32Api.GetColourAt(new Point(pos.Location.X + 1, pos.Location.Y + 1));
                this.BackColor = taskBarColour;

            }
            else
            {
                this.BackColor = Color.Transparent;
            }

            /*
                float dpiX, dpiY;
                using (Graphics graphics = this.CreateGraphics())
                {
                    dpiX = graphics.DpiX;
                    dpiY = graphics.DpiY;
                }
                float fontSize = 7f;
                if (dpiX > 96)
                    fontSize = 6f;
                */

            AdjustControlSize();
            UpdateGraphs();
            this.Invalidate();

        }
        private void Initialize(Options opt)
        {

            var theme = GetTheme(opt);

            Counters = new List<Counters.ICounter>();
            if (opt.CounterOptions.ContainsKey("CPU"))
            {
                var ct = new Counters.CounterCPU(opt);
                ct.Initialize();
                Counters.Add(ct);
            }
            if (opt.CounterOptions.ContainsKey("MEM"))
            {
                var ct = new Counters.CounterMemory(opt);
                ct.Initialize();
                Counters.Add(ct);
            }
            if (opt.CounterOptions.ContainsKey("DISK"))
            {
                var ct = new Counters.CounterDisk(opt);
                ct.Initialize();
                Counters.Add(ct);
            }
            if (opt.CounterOptions.ContainsKey("NET"))
            {
                var ct = new Counters.CounterNetwork(opt);
                ct.Initialize();
                Counters.Add(ct);
            }

            
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.Opaque, true);
            ApplyOptions(opt, theme);
            //Initialize();

            InitializeComponent();
            AdjustControlSize();

            pollingTimer = new System.Timers.Timer(opt.PollTime * 1000);
            pollingTimer.Enabled = true;
            pollingTimer.Elapsed += PollingTimer_Elapsed;
            pollingTimer.Start();

        }

        private void AdjustControlSize()
        {
            int taskbarWidth = GetTaskbarWidth();
            int taskbarHeight = GetTaskbarHeight();
            int minimumHeight = taskbarHeight;
            if (minimumHeight < 30)
                minimumHeight = 30;

            if (taskbarWidth > 0 && taskbarHeight == 0)
                VerticalTaskbarMode = true;

            int counterSize = (Options.HistorySize + 10);
            int controlWidth = counterSize * CountersCount;
            int controlHeight = minimumHeight;

            if (VerticalTaskbarMode && taskbarWidth < controlWidth)
            {
                int countersPerLine = Convert.ToInt32(Math.Floor((float)taskbarWidth / (float)counterSize));
                controlWidth = counterSize * countersPerLine;
                controlHeight = Convert.ToInt32(Math.Ceiling((float)CountersCount / (float)countersPerLine)) * (30 + 10);
            }
            if (!VerticalTaskbarMode && !PreviewMode)
            {
                this.Top = 1;
                controlHeight = controlHeight - 2;
            }
            if (this.Size.Width != controlWidth || this.Size.Height != controlHeight)
            {
                this.Size = new Size(controlWidth, controlHeight);
                if (OnChangeSize != null)
                    OnChangeSize(new Size(controlWidth, controlHeight));
            }
        }

        private void UpdateGraphs()
        {
            foreach (var ct in Counters)
            {
                ct.Update();
            }
            if (pollingTimer != null && pollingTimer.Interval != Options.PollTime * 1000)
                pollingTimer.Interval = Options.PollTime * 1000;
        }

        private void SystemWatcherControl_Paint(object sender, PaintEventArgs e)
        {
            int maximumHeight = GetTaskbarHeight();
            if (maximumHeight <= 0)
                maximumHeight = 30;

            if (lastSize != maximumHeight)
            {
                this.Height = maximumHeight;
                lastSize = maximumHeight;
            }

            int graphPosition = 0;
            int graphPositionY = 0;


            System.Drawing.Graphics formGraphics = e.Graphics;// this.CreateGraphics();            
            formGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;//AntiAliasGridFit;
            formGraphics.Clear(this.BackColor);
            //formGraphics.Clear(Color.Transparent);
            foreach (var pair in Options.CounterOptions.Where(x => x.Value.Enabled == true))
            {
                var name = pair.Key;
                var opt = pair.Value;
                var ct = Counters.Where(x => x.GetName() == name).Single();
                var infos = ct.Infos;
                //var opt = Options.CounterOptions[ct.GetName()];
                //if (!opt.Enabled) continue;
                var showCurrentValue = !opt.CurrentValueAsSummary &&
                    (opt.ShowCurrentValue == CounterOptions.DisplayType.SHOW || (opt.ShowCurrentValue == CounterOptions.DisplayType.HOVER && mouseOver));

                lock (ct.ThreadLock)
                {
                    if (ct.GetCounterType() == TaskbarMonitor.Counters.ICounter.CounterType.SINGLE)
                    {
                        var info = infos[0];
                        drawGraph(formGraphics, graphPosition, 0 + graphPositionY, maximumHeight, false, info, defaultTheme, opt);

                    }
                    else if (ct.GetCounterType() == TaskbarMonitor.Counters.ICounter.CounterType.MIRRORED)
                    {


                        for (int z = 0; z < infos.Count; z++)
                        {
                            var info = opt.InvertOrder ? infos[infos.Count - 1 - z] : infos[z];
                            drawGraph(formGraphics, graphPosition, z * (maximumHeight / 2) + graphPositionY, maximumHeight / 2, z == 1, info, defaultTheme, opt);
                        }


                    }
                    else if (ct.GetCounterType() == TaskbarMonitor.Counters.ICounter.CounterType.STACKED)
                    {
                        drawStackedGraph(formGraphics, graphPosition, 0 + graphPositionY, maximumHeight, opt.InvertOrder, infos, defaultTheme, opt);


                    }
                }

                var sizeTitle = formGraphics.MeasureString(ct.GetName(), fontTitle);
                Dictionary<CounterOptions.DisplayPosition, float> positions = new Dictionary<CounterOptions.DisplayPosition, float>();

                positions.Add(CounterOptions.DisplayPosition.MIDDLE, (maximumHeight / 2 - sizeTitle.Height / 2) + 1 + graphPositionY);
                positions.Add(CounterOptions.DisplayPosition.TOP, graphPositionY);
                positions.Add(CounterOptions.DisplayPosition.BOTTOM, (maximumHeight - sizeTitle.Height + 1) + graphPositionY);

                CounterOptions.DisplayPosition? usedPosition = null;
                if (opt.ShowTitle == CounterOptions.DisplayType.SHOW
                 || opt.ShowTitle == CounterOptions.DisplayType.HOVER)
                {

                    usedPosition = opt.TitlePosition;
                    var titleShadow = defaultTheme.TitleShadowColor;
                    var titleColor = defaultTheme.TitleColor;

                    if (opt.ShowTitle == CounterOptions.DisplayType.HOVER && !mouseOver)
                    {
                        titleColor = Color.FromArgb(40, titleColor.R, titleColor.G, titleColor.B);
                    }


                    System.Drawing.SolidBrush brushShadow = new System.Drawing.SolidBrush(titleShadow);
                    System.Drawing.SolidBrush brushTitle = new System.Drawing.SolidBrush(titleColor);
                    
                    if (
                        (opt.ShowTitleShadowOnHover && opt.ShowTitle == CounterOptions.DisplayType.HOVER && !mouseOver)
                        || (opt.ShowTitle == CounterOptions.DisplayType.HOVER && mouseOver)
                        || opt.ShowTitle == CounterOptions.DisplayType.SHOW
                       )
                    {
                        if ((opt.ShowTitle == CounterOptions.DisplayType.HOVER && mouseOver) || opt.ShowTitle == CounterOptions.DisplayType.SHOW)
                        {
                            formGraphics.DrawString(ct.GetName(), fontTitle, brushShadow, new RectangleF(graphPosition + (Options.HistorySize / 2) - (sizeTitle.Width / 2) + 1, positions[opt.TitlePosition] + 1, sizeTitle.Width, maximumHeight), new StringFormat());
                        }
                        formGraphics.DrawString(ct.GetName(), fontTitle, brushTitle, new RectangleF(graphPosition + (Options.HistorySize / 2) - (sizeTitle.Width / 2), positions[opt.TitlePosition], sizeTitle.Width, maximumHeight), new StringFormat());
                    }


                    brushShadow.Dispose();
                    brushTitle.Dispose();
                }

                if (opt.ShowCurrentValue == CounterOptions.DisplayType.SHOW
                 || opt.ShowCurrentValue == CounterOptions.DisplayType.HOVER)
                {
                    Dictionary<CounterOptions.DisplayPosition, string> texts = new Dictionary<CounterOptions.DisplayPosition, string>();

                    if (opt.CurrentValueAsSummary || infos.Count > 2)
                    {
                        texts.Add(opt.SummaryPosition, ct.InfoSummary.CurrentStringValue);

                    }
                    else
                    {
                        List<CounterOptions.DisplayPosition> positionsAvailable = new List<CounterOptions.DisplayPosition> { CounterOptions.DisplayPosition.TOP, CounterOptions.DisplayPosition.MIDDLE, CounterOptions.DisplayPosition.BOTTOM };
                        if (usedPosition.HasValue)
                            positionsAvailable.Remove(usedPosition.Value);
                        var showName = infos.Count > 1;
                        for (int i = 0; i < infos.Count && i < 2; i++)
                        {
                            texts.Add(positionsAvailable[i], (showName ? infos[i].Name + " " : "") + infos[i].CurrentStringValue);
                        }
                    }
                    foreach (var item in texts)
                    {
                        string text = item.Value;

                        var sizeString = formGraphics.MeasureString(text, fontCounter);
                        float ypos = positions[item.Key];

                        var titleShadow = defaultTheme.TextShadowColor;
                        var titleColor = defaultTheme.TextColor;

                        if (opt.ShowCurrentValue == CounterOptions.DisplayType.HOVER && !mouseOver)
                        {
                            titleColor = Color.FromArgb(40, titleColor.R, titleColor.G, titleColor.B);
                            //titleShadow = Color.FromArgb(40, titleShadow.R, titleShadow.G, titleShadow.B);
                        }

                        SolidBrush BrushText = new SolidBrush(titleColor);
                        SolidBrush BrushTextShadow = new SolidBrush(titleShadow);

                        if (
                        (opt.ShowCurrentValueShadowOnHover && opt.ShowCurrentValue == CounterOptions.DisplayType.HOVER && !mouseOver)
                        || (opt.ShowCurrentValue == CounterOptions.DisplayType.HOVER && mouseOver)
                        || opt.ShowCurrentValue == CounterOptions.DisplayType.SHOW
                       )
                        {
                            if ((opt.ShowCurrentValue == CounterOptions.DisplayType.HOVER && mouseOver) || opt.ShowCurrentValue == CounterOptions.DisplayType.SHOW)
                            {
                                formGraphics.DrawString(text, fontCounter, BrushTextShadow, new RectangleF(graphPosition + (Options.HistorySize / 2) - (sizeString.Width / 2) + 1, ypos + 1, sizeString.Width, maximumHeight), new StringFormat());                                
                            }
                            formGraphics.DrawString(text, fontCounter, BrushText, new RectangleF(graphPosition + (Options.HistorySize / 2) - (sizeString.Width / 2), ypos, sizeString.Width, maximumHeight), new StringFormat());
                        }
                        //formGraphics.DrawString(titleShadow.ToString(), fontCounter, BrushText, new RectangleF(0, 15, 400, 100), new StringFormat());
                        BrushText.Dispose();
                        BrushTextShadow.Dispose();
                    }
                }
                

                graphPosition += Options.HistorySize + 10;
                if (graphPosition >= this.Size.Width)
                {
                    graphPosition = 0;
                    graphPositionY += (maximumHeight + 10);
                }
            }
            AdjustControlSize();

        }


        private void drawGraph(System.Drawing.Graphics formGraphics, int x, int y, int maxH, bool invertido, TaskbarMonitor.Counters.CounterInfo info, GraphTheme theme, CounterOptions opt)
        {
            var pos = maxH - ((info.CurrentValue * maxH) / info.MaximumValue);
            if (pos > Int32.MaxValue) pos = Int32.MaxValue;
            int posInt = Convert.ToInt32(pos) + y;

            var height = (info.CurrentValue * maxH) / info.MaximumValue;
            if (height > Int32.MaxValue) height = Int32.MaxValue;
            int heightInt = Convert.ToInt32(height);

            using (SolidBrush BrushBar = new SolidBrush(theme.BarColor))
            {
                if (invertido)
                    formGraphics.FillRectangle(BrushBar, new Rectangle(x + Options.HistorySize, maxH, 4, heightInt));
                else
                    formGraphics.FillRectangle(BrushBar, new Rectangle(x + Options.HistorySize, posInt, 4, heightInt));
            }

            var initialGraphPosition = x + Options.HistorySize - info.History.Count;
            Point[] points = new Point[info.History.Count + 2];
            int i = 0;
            int inverter = invertido ? -1 : 1;
            foreach (var item in info.History)
            {
                var heightItem = (item * maxH) / info.MaximumValue;
                if (heightItem > Int32.MaxValue) height = Int32.MaxValue;
                var convertido = Convert.ToInt32(heightItem);


                if (invertido)
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
            using (SolidBrush BrushGraph = new SolidBrush(theme.getNthColor(2, invertido ? 1 : 0)))
            {
                formGraphics.FillPolygon(BrushGraph, points);
            }

        }

        private void drawStackedGraph(System.Drawing.Graphics formGraphics, int x, int y, int maxH, bool invertido, List<TaskbarMonitor.Counters.CounterInfo> infos, GraphTheme theme, CounterOptions opt)
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
            if (!invertido)
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

                SolidBrush BrushBar = new SolidBrush(theme.BarColor);
                formGraphics.FillRectangle(BrushBar, new Rectangle(x + Options.HistorySize, posInt, 4, heightInt));
                BrushBar.Dispose();

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
                brush.Dispose();


            }
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

        private void OpenSettings(int activeIndex = 0)
        {
            var qtd = Application.OpenForms.OfType<OptionForm>();
            OptionForm optForm = null;
            if (qtd.Count() == 0)
            {
                optForm = new OptionForm(this.Options, this.customTheme, this.Version, this);
                optForm.Show();
            }
            else
            {
                optForm = qtd.First();
                optForm.Focus();
            }
            optForm.OpenTab(activeIndex);
        }
        private void MenuItem_Settings_onClick(object sender, EventArgs e)
        {
            OpenSettings();
        }
        private void MenuItem_About_onClick(object sender, EventArgs e)
        {
            OpenSettings(2);

        }

        protected override void OnParentBackColorChanged(EventArgs e)
        {
            this.Invalidate();
            base.OnParentBackColorChanged(e);
        }


    }




}
