﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
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
        public bool SHOW_DEBUG = false;
        int taskbarHeight = 0;
        public delegate void SizeChangeHandler(Size size);
        public event SizeChangeHandler OnChangeSize;
        public Version Version { get; set; } = new Version(Properties.Resources.Version);
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Options Options { get; set; }

        private bool _previewMode = false;
        private ContextMenu _contextMenu = null;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Monitor Monitor { get; private set; }
        public bool VerticalTaskbarMode
        {
            get; private set;
        }
        
        public bool PreviewMode
        {
            get
            {
                return _previewMode;
            }
            set
            {
                _previewMode = value;
                //this.ContextMenu = _previewMode ? null : _contextMenu;
            }
        }
        public int CountersCount
        {
            get
            {
                if (Monitor?.Counters == null) return 0;
                return Options.CounterOptions.Where(x => x.Value.Enabled == true).Count();
                //return Counters.Count;
            }
        }
        
        System.Drawing.Font fontCounter;
        Font fontTitle;
        int lastSize = 30;
        bool mouseOver = false;
        public GraphTheme customTheme;
        GraphTheme darkTheme;
        GraphTheme lightTheme;

        GraphTheme defaultTheme;


        Deskband AssociatedDeskband = null;
        TaskbarManager sTask;

        public SystemWatcherControl(Monitor monitor, bool verticalMode = false, Deskband associatedDeskband = null)//CSDeskBand.CSDeskBandWin w, 
        {
            this.VerticalTaskbarMode = verticalMode;
            this.AssociatedDeskband = associatedDeskband;
            this.SetStyle(ControlStyles.EnableNotifyMessage, true);
            AttachMonitor(monitor);
        }
        public SystemWatcherControl()
            :this(null)
        {
        }

        public SystemWatcherControl(Monitor monitor)            
        {            
            AttachMonitor(monitor); 
        }

        public void AttachMonitor(Monitor monitor)
        {
            Disposed += OnDispose;
            this.Monitor = monitor;
            if (this.Monitor != null)
            {                
                this.SetStyle(ControlStyles.EnableNotifyMessage, true);
                try
                {
                    Options opt = monitor.Options;
                    darkTheme = GraphTheme.DefaultDarkTheme();
                    lightTheme = GraphTheme.DefaultLightTheme();
                    customTheme = GraphTheme.ReadFromDisk();
                    opt.Upgrade(customTheme);

                    Initialize(opt);
                    this.BackColor = Color.Transparent;
                    monitor.OnMonitorUpdated += Monitor_OnMonitorUpdated;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading SystemWatcherControl: {ex.Message}", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnDispose(object sender, EventArgs e)
        {
            if(Monitor != null)
                Monitor.OnMonitorUpdated -= Monitor_OnMonitorUpdated;
        }

        private void Monitor_OnMonitorUpdated()
        {
            if (Options != null && this.Options.ThemeType == Options.ThemeList.AUTOMATIC)
            {
                this.defaultTheme = GetTheme(this.Options);
            }

            this.Invalidate();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if(sTask != null)
                sTask.RemoveControls();
            base.OnHandleDestroyed(e);
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
            this.Monitor.UpdateOptions(Options);            
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

                this.BackColor = Color.Transparent;
            }
            else
            {
                this.ContextMenu = null;

                var pos = BLL.Win32Api.GetTaskbarPosition();
                Color taskBarColour = BLL.Win32Api.GetColourAt(new Point(pos.Location.X + 1, pos.Location.Y + 1));
                this.BackColor = taskBarColour;
            }
            
            AdjustControlSize();
            //UpdateGraphs();
            this.Invalidate();

        }
        private void Initialize(Options opt)
        {

            var theme = GetTheme(opt);

           
            
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

            //BLL.Win32Api.SetWindowPos(this.Handle, new IntPtr(0), this.Left, this.Top, this.Width, this.Height, 0);
             
        }

        private void AdjustControlSize()
        {
            if (PreviewMode)
                return;
            int taskbarWidth = GetTaskbarWidth();
            taskbarHeight = GetTaskbarHeight();

            // taskbar not being shown
            if(taskbarWidth == 0 && taskbarHeight == 0)
            {
                return;
            }
            int minimumHeight = taskbarHeight;            
            if (minimumHeight < 20)
                minimumHeight = 20;

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
            if (!VerticalTaskbarMode)
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

        protected override void OnPaint(PaintEventArgs e)
        {
            var maximumHeight = this.Height;

            int graphPosition = 0;
            int graphPositionY = 0;


            System.Drawing.Graphics formGraphics = e.Graphics;// this.CreateGraphics();            
            formGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;//AntiAliasGridFit;
            formGraphics.Clear(Color.Transparent);
            if (SHOW_DEBUG)
            {
                using (SolidBrush BrushText = new SolidBrush(defaultTheme.TextColor))
                {
                    formGraphics.DrawString($"w: {this.Width}, h: {this.Height}", fontCounter, BrushText, new RectangleF(2, 2, 400, 100), new StringFormat());
                    formGraphics.DrawString($"tb h: {this.taskbarHeight}", fontCounter, BrushText, new RectangleF(2, 10, 400, 100), new StringFormat());

                    if (this.AssociatedDeskband != null)
                    {
                        formGraphics.DrawString($"db w: {this.AssociatedDeskband.Size.Width}, h: {this.AssociatedDeskband.Size.Height}", fontCounter, BrushText, new RectangleF(70, 2, 400, 100), new StringFormat());
                        formGraphics.DrawString($"tb h: {this.AssociatedDeskband.TaskbarInfo.Size.Height}", fontCounter, BrushText, new RectangleF(70, 10, 400, 100), new StringFormat());

                        formGraphics.DrawString($"min w: {this.AssociatedDeskband.Options.MinHorizontalSize.Width}, h: {this.AssociatedDeskband.Options.MinHorizontalSize.Height}", fontCounter, BrushText, new RectangleF(150, 2, 400, 100), new StringFormat());
                    }
                    using (Pen pen = new Pen(BrushText))
                    {
                        formGraphics.DrawRectangle(pen, new Rectangle(0, 0, this.Width - 1, this.Height - 1));
                    }
                }
            }
            else
            {
                foreach (var pair in Options.CounterOptions.Where(x => x.Value.Enabled == true))
                {
                    var name = pair.Key;
                    var opt = pair.Value;
                    var ct = Monitor.Counters.Where(x => x.GetName() == name).Single();
                    var infos = ct.Infos;
                    //var opt = Options.CounterOptions[ct.GetName()];
                    //if (!opt.Enabled) continue;
                    var showCurrentValue = !opt.CurrentValueAsSummary &&
                        (opt.ShowCurrentValue == CounterOptions.DisplayType.SHOW || (opt.ShowCurrentValue == CounterOptions.DisplayType.HOVER && mouseOver));

                    lock (ct.ThreadLock)
                    {
                        if (infos.Count == 0)
                            continue;

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
                        //formGraphics.DrawString(this.Left.ToString(), fontTitle, brushTitle, new RectangleF(graphPosition + (Options.HistorySize / 2) - (sizeTitle.Width / 2), positions[opt.TitlePosition] + 10, sizeTitle.Width, maximumHeight), new StringFormat());


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
                            BrushText.Dispose();
                            BrushTextShadow.Dispose();
                        }
                    }


                    graphPosition += Options.HistorySize + 10;
                    if (VerticalTaskbarMode && graphPosition >= this.Size.Width)
                    {
                        graphPosition = 0;
                        graphPositionY += (maximumHeight + 10);
                    }

                }
            }

            AdjustControlSize();
            base.OnPaint(e);
        }
         
        private void drawGraph(System.Drawing.Graphics formGraphics, int x, int y, int maxH, bool invertido, TaskbarMonitor.Counters.CounterInfo info, GraphTheme theme, CounterOptions opt)
        {
            var pos = maxH - ((info.CurrentValue * maxH) / info.MaximumValue);
            if (pos > Int32.MaxValue) pos = Int32.MaxValue;
            int posInt = Convert.ToInt32(Math.Round(pos)) + y;

            var height = (info.CurrentValue * maxH) / info.MaximumValue;
            if (height > Int32.MaxValue) height = Int32.MaxValue;
            int heightInt = Convert.ToInt32(Math.Round(height));

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
                var convertido = Convert.ToInt32(Math.Round(heightItem));


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
                int posInt = Convert.ToInt32(Math.Round(pos)) + y;

                var height = (currentValue * maxH) / absMax;
                if (height > Int32.MaxValue) height = Int32.MaxValue;
                int heightInt = Convert.ToInt32(Math.Round(height));

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
                    var convertido = Convert.ToInt32(Math.Round(heightItem));

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

        private static int GetTaskbarWidth()
        {
            return Screen.PrimaryScreen.Bounds.Width - Screen.PrimaryScreen.WorkingArea.Width;
        }

        private static int GetTaskbarHeight()
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
        protected override void WndProc(ref Message m)
        {            
            base.WndProc(ref m);
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

        private void SystemWatcherControl_DoubleClick(object sender, EventArgs e)
        {
#if(DEBUG)
            SHOW_DEBUG = !SHOW_DEBUG;
            this.Invalidate();
#endif
        }
        protected override void OnNotifyMessage(Message m)
        {
            base.OnNotifyMessage(m);
        }
    }




}
