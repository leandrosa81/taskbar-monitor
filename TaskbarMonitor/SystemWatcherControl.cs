using System;
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

        // layout constants shared by the horizontal and the vertical taskbar layout
        private const int GraphSpacing = 10;        // gap reserved after each graph
        private const int CurrentValueBarWidth = 4; // bar drawn right of a graph showing its current value
        private const int VerticalGraphHeight = 30; // height of a single graph when the taskbar is vertical
        private const int VerticalSideMargin = 2;   // margin kept on both sides of a vertical taskbar
        private const int MinimumGraphWidth = 10;   // never shrink a graph below this
        private const float MinimumFontSize = 5f;   // never shrink a label below this

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

        /// <summary>
        /// Bounds of the taskbar window this control lives in. When set (Windows 11 mode, where
        /// TaskbarManager owns the positioning) it is used instead of the primary screen metrics,
        /// so that every taskbar - including secondary ones - is measured on its own.
        /// </summary>
        private Rectangle taskbarBounds = Rectangle.Empty;

        /// <summary>
        /// Width of a single graph. Equals Options.HistorySize on a horizontal taskbar, but gets
        /// reduced on a vertical taskbar that is narrower than the configured history size.
        /// </summary>
        private int graphWidth = 0;

        /// <summary>
        /// Width used to draw a single graph. Falls back to the configured history size while no
        /// layout pass has run yet (preview mode).
        /// </summary>
        private int CurrentGraphWidth
        {
            get
            {
                if (graphWidth > 0) return graphWidth;
                return Options != null ? Options.HistorySize : 0;
            }
        }

        // fonts scaled down to fit a narrow vertical taskbar, kept to avoid recreating them on every paint
        private readonly Dictionary<string, Font> shrunkFonts = new Dictionary<string, Font>();

        /// <summary>
        /// Measures <paramref name="text"/> and, on a vertical taskbar, returns a smaller font when
        /// the text would not fit into the available width. Counter labels such as "GPU VIDEO
        /// DECODE" are far wider than the few dozen pixels a vertical taskbar offers.
        /// </summary>
        private Font GetFittingFont(Graphics formGraphics, Font baseFont, string text, float available, ref SizeF size)
        {
            size = formGraphics.MeasureString(text, baseFont);
            if (!VerticalTaskbarMode || available <= 0 || size.Width <= available || size.Width <= 0)
                return baseFont;

            float scaled = baseFont.Size * (available / size.Width);
            // round down to half points so the cache stays small
            scaled = (float)Math.Floor(scaled * 2f) / 2f;
            if (scaled < MinimumFontSize) scaled = MinimumFontSize;
            if (scaled >= baseFont.Size) return baseFont;

            string key = baseFont.Name + "|" + baseFont.Style + "|" + scaled;
            Font font;
            if (!shrunkFonts.TryGetValue(key, out font))
            {
                font = new Font(baseFont.FontFamily, scaled, baseFont.Style);
                shrunkFonts[key] = font;
            }
            size = formGraphics.MeasureString(text, font);
            return font;
        }

        private void ClearShrunkFonts()
        {
            foreach (var font in shrunkFonts.Values)
                font.Dispose();
            shrunkFonts.Clear();
        }

        /// <summary>
        /// Horizontal position of a label centered over a graph. A vertical taskbar is only a few
        /// dozen pixels wide, so labels that are wider than the graph are kept inside the control
        /// instead of overflowing (and being clipped) on the left.
        /// </summary>
        private float GetCenteredTextPosition(int graphPosition, float textWidth, int graphW)
        {
            float x = graphPosition + (graphW / 2f) - (textWidth / 2f);
            if (VerticalTaskbarMode)
            {
                if (x + textWidth > this.Width)
                    x = this.Width - textWidth;
                if (x < 0)
                    x = 0;
            }
            return x;
        }

        public void SetTaskbarBounds(Rectangle bounds)
        {
            if (taskbarBounds == bounds)
                return;
            taskbarBounds = bounds;
            AdjustControlSize();
            this.Invalidate();
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
            ClearShrunkFonts();
            if(Monitor != null)
                Monitor.OnMonitorUpdated -= Monitor_OnMonitorUpdated;
            if(BLL.WindowsInformation.IsWindows11())
                StopMousePolling();
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
            {
                theme = lightTheme;
            }
            else if (opt.ThemeType == Options.ThemeList.CUSTOM)
            {
                customTheme = GraphTheme.ReadFromDisk();
                opt.Upgrade(customTheme);
                theme = customTheme;
            }
            else if (opt.ThemeType == Options.ThemeList.AUTOMATIC)
            {
                // Try to detect Windows app mode (light/dark) from registry
                bool? isLightTheme = null;
                try
                {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    {
                        if (key != null)
                        {
                            object regValue = key.GetValue("SystemUsesLightTheme");
                            if (regValue != null)
                            {
                                isLightTheme = ((int)regValue) > 0;
                            }
                            else
                            {
                                regValue = key.GetValue("AppsUseLightTheme");
                                if (regValue != null)
                                {
                                    isLightTheme = ((int)regValue) > 0;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore registry errors, fallback to color detection
                }

                if (isLightTheme.HasValue)
                {
                    theme = isLightTheme.Value ? lightTheme : darkTheme;
                }
                else
                {
                    // Sample several points along the taskbar and average their luminance
                    var taskbarRect = BLL.Win32Api.GetTaskbarPosition();
                    int sampleCount = 5;
                    double totalLuminance = 0;
                    for (int i = 0; i < sampleCount; i++)
                    {
                        int x, y;
                        if (taskbarRect.Width > taskbarRect.Height)
                        {
                            // Horizontal taskbar (bottom or top)
                            x = taskbarRect.Left + (i * taskbarRect.Width) / (sampleCount - 1);
                            y = taskbarRect.Top + taskbarRect.Height / 2;
                        }
                        else
                        {
                            // Vertical taskbar (left or right)
                            x = taskbarRect.Left + taskbarRect.Width / 2;
                            y = taskbarRect.Top + (i * taskbarRect.Height) / (sampleCount - 1);
                        }
                        Color color = BLL.Win32Api.GetColourAt(new Point(x, y));
                        double luminance = 0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B;
                        totalLuminance += luminance;
                    }
                    double avgLuminance = totalLuminance / sampleCount;
                    theme = avgLuminance > 128 ? lightTheme : darkTheme;
                }
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
            ClearShrunkFonts();

            if (!PreviewMode)
            {
                _contextMenu = new ContextMenu();
                _contextMenu.MenuItems.Add(new MenuItem("Settings...", MenuItem_Settings_onClick));
                _contextMenu.MenuItems.Add(new MenuItem("Open Task Manager...", (e, a) =>
                {
                    if (System.IO.File.Exists(Environment.SystemDirectory + @"\taskmgr.exe"))
                        System.Diagnostics.Process.Start(Environment.SystemDirectory + @"\taskmgr.exe");
                    else
                        System.Diagnostics.Process.Start(@"taskmgr.exe");
                }));
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
            if (BLL.WindowsInformation.IsWindows11())
                StartMousePolling();
            //BLL.Win32Api.SetWindowPos(this.Handle, new IntPtr(0), this.Left, this.Top, this.Width, this.Height, 0);

        }

        private void AdjustControlSize()
        {
            // SetTaskbarBounds can be called before the options have been applied
            if (Options == null)
                return;
            // in preview mode the control keeps its designer size until the form tells us
            // which taskbar to imitate
            if (PreviewMode && taskbarBounds == Rectangle.Empty)
                return;

            int taskbarThickness;

            if (taskbarBounds != Rectangle.Empty)
            {
                // the taskbar window is known (Windows 11 mode): measure that window instead of
                // deriving the taskbar size from the primary screen, so multi monitor setups and
                // mixed orientations are handled correctly.
                VerticalTaskbarMode = taskbarBounds.Height > taskbarBounds.Width;
                taskbarThickness = VerticalTaskbarMode ? taskbarBounds.Width : taskbarBounds.Height;
                taskbarHeight = VerticalTaskbarMode ? 0 : taskbarBounds.Height;
            }
            else
            {
                int taskbarWidth = GetTaskbarWidth();
                taskbarHeight = GetTaskbarHeight();

                // taskbar not being shown
                if (taskbarWidth == 0 && taskbarHeight == 0)
                {
                    return;
                }

                if (taskbarWidth > 0 && taskbarHeight == 0)
                    VerticalTaskbarMode = true;
                else if (taskbarWidth == 0 && taskbarHeight > 0)
                    VerticalTaskbarMode = false;

                taskbarThickness = VerticalTaskbarMode ? taskbarWidth : taskbarHeight;
            }

            if (taskbarThickness <= 0)
                return;

            int controlWidth;
            int controlHeight;

            if (VerticalTaskbarMode)
            {
                // On a vertical taskbar the available width is the (usually small) taskbar
                // thickness, so the graphs are stacked from top to bottom. If more than one graph
                // fits side by side we still use the available width, wrapping into extra lines.
                int availableWidth = taskbarThickness - (2 * VerticalSideMargin);
                if (availableWidth < MinimumGraphWidth + CurrentValueBarWidth)
                    availableWidth = MinimumGraphWidth + CurrentValueBarWidth;

                // the last graph of a line is followed by its current value bar only, not by
                // a gap, so that narrow taskbars are used up to their right edge
                int counterSize = Options.HistorySize + GraphSpacing;
                int countersPerLine = (availableWidth + GraphSpacing - CurrentValueBarWidth) / counterSize;

                if (countersPerLine < 1)
                {
                    // taskbar narrower than a full sized graph: shrink the graph to fit
                    countersPerLine = 1;
                    graphWidth = Math.Max(MinimumGraphWidth, availableWidth - CurrentValueBarWidth);
                }
                else
                {
                    graphWidth = Options.HistorySize;
                }
                if (countersPerLine > CountersCount && CountersCount > 0)
                    countersPerLine = CountersCount;

                int lines = CountersCount > 0
                    ? Convert.ToInt32(Math.Ceiling((float)CountersCount / (float)countersPerLine))
                    : 0;

                controlWidth = countersPerLine * (graphWidth + GraphSpacing) - GraphSpacing + CurrentValueBarWidth;
                controlHeight = lines * (VerticalGraphHeight + GraphSpacing);

                if (taskbarBounds == Rectangle.Empty)
                {
                    // deskband mode: keep the historical left padding
                    this.Left = 5;
                    controlWidth = controlWidth - 5;
                }
            }
            else
            {
                graphWidth = Options.HistorySize;

                int minimumHeight = taskbarThickness;
                if (minimumHeight < 20)
                    minimumHeight = 20;

                controlWidth = (graphWidth + GraphSpacing) * CountersCount;
                controlHeight = minimumHeight - 2;

                // the preview is placed by the options dialog, not by us
                if (!PreviewMode)
                    this.Top = 1;
            }

            if (controlWidth < 1) controlWidth = 1;
            if (controlHeight < 1) controlHeight = 1;

            if (this.Size.Width != controlWidth || this.Size.Height != controlHeight)
            {
                this.Size = new Size(controlWidth, controlHeight);
                if (OnChangeSize != null)
                    OnChangeSize(new Size(controlWidth, controlHeight));
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var maximumHeight = VerticalTaskbarMode ? VerticalGraphHeight : this.Height;
            int currentGraphWidth = CurrentGraphWidth;

            int graphPosition = 0;
            int graphPositionY = 0;


            System.Drawing.Graphics formGraphics = e.Graphics;// this.CreateGraphics();            
            formGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

            //formGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;//AntiAliasGridFit;
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
                if (Options == null)
                {
                    base.OnPaint(e);
                    return;
                }
                foreach (var pair in Options.CounterOptions.Where(x => x.Value.Enabled == true).OrderBy(x => x.Value.Order))
                {
                    var name = pair.Key;
                    var opt = pair.Value;
                    var ct = Monitor.Counters.Where(x => x.GetName() == name).SingleOrDefault();
                    if (ct == null) continue;

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

                    SizeF sizeTitle = SizeF.Empty;
                    Font titleFont = GetFittingFont(formGraphics, fontTitle, ct.GetLabel(), this.Width, ref sizeTitle);
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
                        
                        if (opt.ShowTitle == CounterOptions.DisplayType.HOVER && mouseOver)
                        {                            
                            //titleShadow = Color.FromArgb(40, titleShadow.R, titleShadow.G, titleShadow.B);
                        }
                        

                        System.Drawing.SolidBrush brushShadow = new System.Drawing.SolidBrush(titleShadow);
                        System.Drawing.SolidBrush brushTitle = new System.Drawing.SolidBrush(titleColor);

                        /*if (
                            (opt.ShowTitleShadowOnHover && opt.ShowTitle == CounterOptions.DisplayType.HOVER && !mouseOver)
                            || (opt.ShowTitle == CounterOptions.DisplayType.HOVER && mouseOver)
                            || opt.ShowTitle == CounterOptions.DisplayType.SHOW
                           )
                        {*/
                            // show shadow only on SHOW, or (HOVER and mouseover) or (HOVER and !mousever and showTitleShadow)
                            if ((opt.ShowTitle == CounterOptions.DisplayType.HOVER && opt.ShowTitleShadowOnHover) || mouseOver)
                            {
                            int offset = 1;
                            if (!mouseOver)
                                offset = 0;
                               formGraphics.DrawString(ct.GetLabel(), titleFont, brushShadow, new RectangleF(GetCenteredTextPosition(graphPosition, sizeTitle.Width, currentGraphWidth) + offset, positions[opt.TitlePosition] + offset, sizeTitle.Width, maximumHeight), new StringFormat());
                            }
                            // show title only on SHOW, or (HOVER and mouseover)
                            if ((opt.ShowTitle == CounterOptions.DisplayType.HOVER && mouseOver) || opt.ShowTitle == CounterOptions.DisplayType.SHOW)
                            {
                                formGraphics.DrawString(ct.GetLabel(), titleFont, brushTitle, new RectangleF(GetCenteredTextPosition(graphPosition, sizeTitle.Width, currentGraphWidth), positions[opt.TitlePosition], sizeTitle.Width, maximumHeight), new StringFormat());
                            }
                        //}
                        

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

                            SizeF sizeString = SizeF.Empty;
                            Font valueFont = GetFittingFont(formGraphics, fontCounter, text, this.Width, ref sizeString);
                            float ypos = positions[item.Key];

                            var textShadow = defaultTheme.TextShadowColor;
                            var textColor = defaultTheme.TextColor;

                            if (opt.ShowCurrentValue == CounterOptions.DisplayType.HOVER && mouseOver)
                            {
                                //textShadow = Color.FromArgb(40, textShadow.R, textShadow.G, textShadow.B);
                            }

                            SolidBrush BrushText = new SolidBrush(textColor);
                            SolidBrush BrushTextShadow = new SolidBrush(textShadow);

                            /*if (
                            (opt.ShowCurrentValueShadowOnHover && opt.ShowCurrentValue == CounterOptions.DisplayType.HOVER && !mouseOver)
                            || (opt.ShowCurrentValue == CounterOptions.DisplayType.HOVER && mouseOver)
                            || opt.ShowCurrentValue == CounterOptions.DisplayType.SHOW
                           )
                            {*/
                            if ((opt.ShowCurrentValue == CounterOptions.DisplayType.HOVER && opt.ShowCurrentValueShadowOnHover) || mouseOver)
                            {
                                int offset = 1;
                                if (!mouseOver)
                                    offset = 0;
                                formGraphics.DrawString(text, valueFont, BrushTextShadow, new RectangleF(GetCenteredTextPosition(graphPosition, sizeString.Width, currentGraphWidth) + offset, ypos + offset, sizeString.Width, maximumHeight), new StringFormat());
                            }
                            if ((opt.ShowCurrentValue == CounterOptions.DisplayType.HOVER && mouseOver) || opt.ShowCurrentValue == CounterOptions.DisplayType.SHOW)
                            { 
                                formGraphics.DrawString(text, valueFont, BrushText, new RectangleF(GetCenteredTextPosition(graphPosition, sizeString.Width, currentGraphWidth), ypos, sizeString.Width, maximumHeight), new StringFormat());
                            }
                            //}
                            BrushText.Dispose();
                            BrushTextShadow.Dispose();
                        }
                    }


                    graphPosition += currentGraphWidth + GraphSpacing;
                    if (VerticalTaskbarMode && graphPosition >= this.Size.Width)
                    {
                        graphPosition = 0;
                        graphPositionY += (maximumHeight + GraphSpacing);
                    }

                }
            }

            AdjustControlSize();
            base.OnPaint(e);
        }
         
        private void drawGraph(System.Drawing.Graphics formGraphics, int x, int y, int maxH, bool invertido, TaskbarMonitor.Counters.CounterInfo info, GraphTheme theme, CounterOptions opt)
        {
            if (info.MaximumValue == 0) return;
            int graphW = CurrentGraphWidth;
            if (graphW <= 0) return;

            var pos = maxH - ((info.CurrentValue * maxH) / info.MaximumValue);
            if (pos > Int32.MaxValue) pos = Int32.MaxValue;
            int posInt = Convert.ToInt32(Math.Round(pos)) + y;

            var height = (info.CurrentValue * maxH) / info.MaximumValue;
            if (height > Int32.MaxValue) height = Int32.MaxValue;
            int heightInt = Convert.ToInt32(Math.Round(height));

            using (SolidBrush BrushBar = new SolidBrush(theme.BarColor))
            {
                if (invertido)
                    formGraphics.FillRectangle(BrushBar, new Rectangle(x + graphW, maxH, CurrentValueBarWidth, heightInt));
                else
                    formGraphics.FillRectangle(BrushBar, new Rectangle(x + graphW, posInt, CurrentValueBarWidth, heightInt));
            }

            // when the graph is narrower than the history (vertical taskbar) only the most
            // recent samples are drawn
            int visibleCount = Math.Min(info.History.Count, graphW);
            if (visibleCount == 0) return;
            int firstVisible = info.History.Count - visibleCount;

            var initialGraphPosition = x + graphW - visibleCount;
            Point[] points = new Point[visibleCount + 2];
            int i = 0;
            int inverter = invertido ? -1 : 1;
            for (int idx = firstVisible; idx < info.History.Count; idx++)
            {
                var item = info.History[idx];
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
            int graphW = CurrentGraphWidth;
            if (graphW <= 0) return;

            // when the graph is narrower than the history (vertical taskbar) only the most
            // recent samples are drawn
            var fullHistorySize = values.Count > 0 ? values[0].Count : 0;
            var historySize = Math.Min(fullHistorySize, graphW);
            var firstVisible = fullHistorySize - historySize;
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
                formGraphics.FillRectangle(BrushBar, new Rectangle(x + graphW, posInt, CurrentValueBarWidth, heightInt));
                BrushBar.Dispose();

                int i = 0;
                var initialGraphPosition = x + graphW - historySize;
                Point[] points = new Point[historySize + 2];
                for (int idx = Math.Max(firstVisible, info.Count - historySize); idx < info.Count && i < historySize; idx++)
                {
                    var item = info[idx];
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
        private System.Windows.Forms.Timer mousePollTimer;
        private bool lastMouseOver = false;

        private void StartMousePolling()
        {
            mousePollTimer = new System.Windows.Forms.Timer();
            mousePollTimer.Interval = 50; // ms
            mousePollTimer.Tick += MousePollTimer_Tick;
            mousePollTimer.Start();
        }

        private void StopMousePolling()
        {
            if (mousePollTimer != null)
            {
                mousePollTimer.Stop();
                mousePollTimer.Tick -= MousePollTimer_Tick;
                mousePollTimer.Dispose();
                mousePollTimer = null;
            }
        }

        private void MousePollTimer_Tick(object sender, EventArgs e)
        {
            var cursorPos = Cursor.Position;
            if (this.Disposing || this.IsDisposed) return;
            var clientRect = this.RectangleToScreen(this.ClientRectangle);
            bool isOver = clientRect.Contains(cursorPos);

            if (isOver && !lastMouseOver)
            {
                lastMouseOver = true;
                SystemWatcherControl_MouseEnter(this, EventArgs.Empty);
            }
            else if (!isOver && lastMouseOver)
            {
                lastMouseOver = false;
                SystemWatcherControl_MouseLeave(this, EventArgs.Empty);
            }
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
                optForm = new OptionForm(this.Options, this.customTheme, this.Version, TaskbarManager.GetInstance());
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
            OpenSettings(3);

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
