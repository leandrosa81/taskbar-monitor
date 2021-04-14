﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaskbarMonitor
{
    public partial class OptionForm : Form
    {
        private Options OriginalOptions;
        private GraphTheme OriginalTheme;

        private Options Options;
        private GraphTheme Theme;

        private Version Version;

        private CounterOptions ActiveCounter = null;
        private bool initializing = true;
        Dictionary<string, IList<TaskbarMonitor.Counters.ICounter.CounterType>> AvailableGraphTypes;

        SystemWatcherControl originalControl = null;

        private Font ChosenTitleFont;
        private Font ChosenCurrentValueFont;

        public OptionForm(Options opt, GraphTheme theme, Version version, SystemWatcherControl originalControl)
        {
            this.Version = version;
            
            this.Theme = new GraphTheme();
            this.OriginalTheme = theme;
            theme.CopyTo(this.Theme);

            this.Options = new Options();
            this.OriginalOptions = opt;
            opt.CopyTo(this.Options);

            this.originalControl = originalControl;

            AvailableGraphTypes = new Dictionary<string, IList<Counters.ICounter.CounterType>>
            {
                {"CPU",  new List<TaskbarMonitor.Counters.ICounter.CounterType>
                {
                    TaskbarMonitor.Counters.ICounter.CounterType.SINGLE,
                    TaskbarMonitor.Counters.ICounter.CounterType.STACKED
                } 
                },
                {"MEM",  new List<TaskbarMonitor.Counters.ICounter.CounterType>
                {
                    TaskbarMonitor.Counters.ICounter.CounterType.SINGLE
                }
                },
                {"DISK",  new List<TaskbarMonitor.Counters.ICounter.CounterType>
                {
                    TaskbarMonitor.Counters.ICounter.CounterType.SINGLE,
                    TaskbarMonitor.Counters.ICounter.CounterType.STACKED,
                    TaskbarMonitor.Counters.ICounter.CounterType.MIRRORED
                }
                },
                {"NET",  new List<TaskbarMonitor.Counters.ICounter.CounterType>
                {
                    TaskbarMonitor.Counters.ICounter.CounterType.SINGLE,
                    TaskbarMonitor.Counters.ICounter.CounterType.STACKED,
                    TaskbarMonitor.Counters.ICounter.CounterType.MIRRORED
                }
                }
            };
            InitializeComponent();
            /*
            float dpiX, dpiY;
            using (Graphics graphics = this.CreateGraphics())
            {
                dpiX = graphics.DpiX;
                dpiY = graphics.DpiY;
            }            

            if (dpiX >= 96)
            {
                var fontSize = 7.25f;
                this.Font = new Font("Calibri", fontSize, FontStyle.Regular);
            }
            */
            Initialize();
        }
        private void Initialize()
        {
            initializing = true;
            this.editHistorySize.Value = this.Options.HistorySize;
            this.editPollTime.Value = this.Options.PollTime;
            this.listThemeType.Text = this.Options.ThemeType.ToString();
            this.listCounters.DataSource = this.Options.CounterOptions.Keys.AsEnumerable().ToList();
            this.listShowTitle.DataSource = Enum.GetValues(typeof(CounterOptions.DisplayType));
            this.listShowCurrentValue.DataSource = Enum.GetValues(typeof(CounterOptions.DisplayType));
            this.listSummaryPosition.DataSource = Enum.GetValues(typeof(CounterOptions.DisplayPosition));
            this.listTitlePosition.DataSource = Enum.GetValues(typeof(CounterOptions.DisplayPosition));

            lblVersion.Text = "v" + Version.ToString(3);

            ActiveCounter = this.Options.CounterOptions.First().Value;
            UpdateForm();
            UpdateReplicateSettingsMenu();
            btnColorBar.BackColor = this.Theme.BarColor;
            btnColorCurrentValue.BackColor = this.Theme.TextColor;
            btnColorCurrentValueShadow.BackColor = this.Theme.TextShadowColor;
            btnColorTitle.BackColor = this.Theme.TitleColor;
            btnColorTitleShadow.BackColor = this.Theme.TitleShadowColor;
            
            ChosenTitleFont = new Font(this.Theme.TitleFont, this.Theme.TitleSize, FontStyle.Bold);
            linkTitleFont.Text = ChosenTitleFont.Name + ", " + ChosenTitleFont.Size + "pt";

            ChosenCurrentValueFont = new Font(this.Theme.CurrentValueFont, this.Theme.CurrentValueSize, FontStyle.Bold);
            linkCurrentValueFont.Text = ChosenCurrentValueFont.Name + ", " + Math.Round(ChosenCurrentValueFont.Size) + "pt";

            btnColor1.BackColor = this.Theme.StackedColors[0];
            btnColor2.BackColor = this.Theme.StackedColors[1];

            UpdatePreview();

            initializing = false;
        }

        private void UpdatePreview()
        {
            var previewOptions = new Options();
            this.Options.CopyTo(previewOptions);
            /*
            for (int i = 0; i < previewOptions.CounterOptions.Keys.Count; i++)
            {
                string key = previewOptions.CounterOptions.Keys.ElementAt(i);
                if (key != this.listCounters.Text)
                {
                    previewOptions.CounterOptions.Remove(key);
                    i--;
                }
            }*/
            GraphTheme previewTheme = GraphTheme.DefaultDarkTheme();

            if (previewOptions.ThemeType == Options.ThemeList.LIGHT)
                previewTheme = GraphTheme.DefaultLightTheme();
            else if (previewOptions.ThemeType == Options.ThemeList.CUSTOM)
            {
                previewTheme = new GraphTheme();
                this.Theme.CopyTo(previewTheme);
            }
            else if (previewOptions.ThemeType == Options.ThemeList.AUTOMATIC)
            {
                Color taskBarColour = BLL.Win32Api.GetColourAt(BLL.Win32Api.GetTaskbarPosition().Location);
                if (taskBarColour.R + taskBarColour.G + taskBarColour.B > 382)
                    previewTheme = GraphTheme.DefaultLightTheme();
                else
                    previewTheme = GraphTheme.DefaultDarkTheme();
            }

            swcPreview.ApplyOptions(previewOptions, previewTheme);
        }

        private void EditHistorySize_ValueChanged(object sender, EventArgs e)
        {
            Options.HistorySize = Convert.ToInt32(editHistorySize.Value);
            UpdatePreview();
        }
        private void editPollTime_ValueChanged(object sender, EventArgs e)
        {
            Options.PollTime = Convert.ToInt32(editPollTime.Value);
            UpdatePreview();
        }

        private void ListCounters_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter = Options.CounterOptions[listCounters.Text];
            UpdateReplicateSettingsMenu();
            UpdateForm();
            UpdatePreview();
        }

        private void UpdateReplicateSettingsMenu()
        {
            contextMenuStripReplicateSettings.Items.Clear();
            contextMenuStripReplicateSettings.Items.Add(new ToolStripMenuItem("All other graphs", null, contextMenuStripReplicateSettings_OnClick));
            contextMenuStripReplicateSettings.Items.Add(new ToolStripSeparator());
            foreach (var item in Options.CounterOptions.Keys.AsEnumerable().ToList())
            {
                if (item != listCounters.Text)
                {
                    contextMenuStripReplicateSettings.Items.Add(new ToolStripMenuItem(item, null, contextMenuStripReplicateSettings_OnClick));
                }
            }
        }
        private void contextMenuStripReplicateSettings_OnClick(object sender, EventArgs e)
        {
            var menu = sender as ToolStripMenuItem;
            List<string> destiny = new List<string>();
            if(menu.Text == "All other graphs")
            {
                foreach (var item in Options.CounterOptions.Keys.AsEnumerable().ToList())
                {
                    if (item != listCounters.Text)
                    {
                        destiny.Add(item);
                    }
                }
            }
            else
            {
                destiny.Add(menu.Text);
            }

            foreach (var item in destiny)
            {
                Options.CounterOptions[item].ShowTitle = ActiveCounter.ShowTitle;
                Options.CounterOptions[item].ShowCurrentValue = ActiveCounter.ShowCurrentValue;
                Options.CounterOptions[item].CurrentValueAsSummary = ActiveCounter.CurrentValueAsSummary;
                Options.CounterOptions[item].SummaryPosition = ActiveCounter.SummaryPosition;
                Options.CounterOptions[item].ShowTitleShadowOnHover = ActiveCounter.ShowTitleShadowOnHover;
                Options.CounterOptions[item].ShowCurrentValueShadowOnHover = ActiveCounter.ShowCurrentValueShadowOnHover;
                Options.CounterOptions[item].TitlePosition = ActiveCounter.TitlePosition;                

            }
        }

        private void UpdateForm()
        {
            initializing = true;
            this.listGraphType.DataSource = this.AvailableGraphTypes[listCounters.Text];
            initializing = false;
            this.listGraphType.Text = ActiveCounter.GraphType.ToString();
            checkEnabled.Checked = ActiveCounter.Enabled;
            listShowTitle.Text = ActiveCounter.ShowTitle.ToString();
            listShowCurrentValue.Text = ActiveCounter.ShowCurrentValue.ToString();
            checkShowSummary.Checked = ActiveCounter.CurrentValueAsSummary;            
            listSummaryPosition.Text = ActiveCounter.SummaryPosition.ToString();
            checkInvertOrder.Checked = ActiveCounter.InvertOrder;
            checkSeparateScales.Checked = ActiveCounter.SeparateScales;
            checkTitleShadowHover.Checked = ActiveCounter.ShowTitleShadowOnHover;
            checkValueShadowHover.Checked = ActiveCounter.ShowCurrentValueShadowOnHover;
            listTitlePosition.Text = ActiveCounter.TitlePosition.ToString();
            UpdateFormScales();
            UpdateFormOrder();
             
            UpdateFormShow();
        }

        

        private void UpdateFormScales()
        {
            checkSeparateScales.Enabled = this.listGraphType.Text == "MIRRORED";
        }

        private void UpdateFormOrder()
        {
            checkInvertOrder.Enabled = this.listGraphType.Text != "SINGLE";
        }

        private void UpdateFormShow()
        {
            checkTitleShadowHover.Enabled = listShowTitle.Text == "HOVER";
            checkValueShadowHover.Enabled = listShowCurrentValue.Text == "HOVER";
            listTitlePosition.Enabled = listShowTitle.Text != "HIDDEN";
            listSummaryPosition.Enabled = checkShowSummary.Checked && listShowCurrentValue.Text != "HIDDEN";
            checkShowSummary.Enabled = listShowCurrentValue.Text != "HIDDEN";
        }

        private void ListShowTitle_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter.ShowTitle = (CounterOptions.DisplayType)Enum.Parse(typeof(CounterOptions.DisplayType), listShowTitle.Text);
            UpdateFormShow();
            UpdatePreview();
        }

        private void listShowCurrentValue_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter.ShowCurrentValue = (CounterOptions.DisplayType)Enum.Parse(typeof(CounterOptions.DisplayType), listShowCurrentValue.Text);
            UpdateFormShow();
            UpdatePreview();
        }

        private void listGraphType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter.GraphType = (TaskbarMonitor.Counters.ICounter.CounterType)Enum.Parse(typeof(TaskbarMonitor.Counters.ICounter.CounterType), listGraphType.Text);
            UpdateFormScales();
            UpdateFormOrder();
            UpdatePreview();
        }

        private void btnMenu_Click(object sender, EventArgs e)
        {

            int i = 0;
            foreach (var item in panelMenu.Controls.OfType<Button>().ToList().OrderBy(x => x.Top))
            {                
                if (item == sender)
                {
                    tabControl1.SelectedIndex = i;
                    UpdateMenuColors(item);
                }
                i++;
            }
            
        } 
        private void UpdateMenuColors(Button active)
        {
            active.BackColor = Color.SteelBlue;
            foreach (var item in panelMenu.Controls.OfType<Button>())
            {
                if (item != active)
                    item.BackColor  = Color.FromArgb(255, 64, 64, 64);
            }
        }

        public void OpenTab(int i)
        {
            tabControl1.SelectedIndex = i;
            UpdateMenuColors(panelMenu.Controls.OfType<Button>().ToList().OrderBy(x => x.Top).ElementAt(i));
        }
 
        private void linkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start((sender as LinkLabel).Text);
        }

        private void checkShowSummary_CheckedChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter.CurrentValueAsSummary = checkShowSummary.Checked;
            UpdateFormShow();
            UpdatePreview();
        }

        private void checkInvertOrder_CheckedChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter.InvertOrder = checkInvertOrder.Checked;
            UpdatePreview();
        }

        private void btnCheckUpdate_Click(object sender, EventArgs e)
        {
            GithubUpdater update = new GithubUpdater("leandrosa81", "taskbar-monitor");

            if (btnCheckUpdate.Text == "Check for updates")            
            {
                btnCheckUpdate.Enabled = false;
                btnCheckUpdate.Text = "checking...";

                
                var task = update.GetLastestVersionAsync();

                var latestVersion = task.Result;
                
                if(latestVersion != null)
                {
                    if (latestVersion.CompareTo(this.Version) > 0)
                    {
                        btnCheckUpdate.Text = "there is an update!";
                        lblLatestVersion.Visible = false;
                        linkLatestVersion.Text = "v" + latestVersion.ToString();
                        linkLatestVersion.Top = lblLatestVersion.Top;
                        linkLatestVersion.Left = lblLatestVersion.Left;
                        linkLatestVersion.Visible = true;
                    }
                    else
                    {
                        btnCheckUpdate.Text = "no updates";
                        lblLatestVersion.Text = "v" + latestVersion.ToString();
                    }
                }
                else
                {
                    btnCheckUpdate.Text = "check failed";
                }
                btnCheckUpdate.Enabled = true;
            }
            

        }

        private bool ChooseColor(Button sender)
        {
            ColorDialog MyDialog = new ColorDialog();
            // Keeps the user from selecting a custom color.
            MyDialog.AllowFullOpen = true;
            // Allows the user to get help. (The default is false.)
            MyDialog.ShowHelp = false;
            // Sets the initial color select to the current text color.
            MyDialog.Color = sender.BackColor;

            // Update the text box color if the user clicks OK 
            if (MyDialog.ShowDialog() == DialogResult.OK)
            {
                sender.BackColor = MyDialog.Color;
                return true;
            }
            return false;
        }

        private bool ChooseFont(LinkLabel sender, ref Font font)
        {
            FontDialog MyDialog = new FontDialog();
            MyDialog.ShowColor = false;
            MyDialog.ShowEffects = false;            
            MyDialog.ShowApply = false;
            MyDialog.ShowHelp = false;
            MyDialog.FontMustExist = true;
            MyDialog.MaxSize = 16;
            MyDialog.Font = font;

            // Update the text box color if the user clicks OK 
            if (MyDialog.ShowDialog() == DialogResult.OK)
            {
                font = MyDialog.Font;
                sender.Text = font.Name + ", " + Math.Round(font.Size) + "pt";
                return true;
            }
            return false;
        }

        private void btnColorBar_Click(object sender, EventArgs e)
        {
            if(ChooseColor(sender as Button))
                this.Theme.BarColor = (sender as Button).BackColor;

            UpdatePreview();
        }

        private void btnColorCurrentValue_Click(object sender, EventArgs e)
        {
            if (ChooseColor(sender as Button))
                this.Theme.TextColor = (sender as Button).BackColor;

            UpdatePreview();
        }

        private void btnColorCurrentValueShadow_Click(object sender, EventArgs e)
        {
            if (ChooseColor(sender as Button))
                this.Theme.TextShadowColor = (sender as Button).BackColor;

            UpdatePreview();
        }

        private void btnColorTitle_Click(object sender, EventArgs e)
        {
            if (ChooseColor(sender as Button))
                this.Theme.TitleColor = (sender as Button).BackColor;

            UpdatePreview();
        }

        private void btnColorTitleShadow_Click(object sender, EventArgs e)
        {
            if (ChooseColor(sender as Button))
                this.Theme.TitleShadowColor = (sender as Button).BackColor;

            UpdatePreview();
        }

        private void btnColor1_Click(object sender, EventArgs e)
        {
            if (ChooseColor(sender as Button))
                this.Theme.StackedColors[0] = (sender as Button).BackColor;

            UpdatePreview();
        }

        private void btnColor2_Click(object sender, EventArgs e)
        {
            if (ChooseColor(sender as Button))
                this.Theme.StackedColors[1] = (sender as Button).BackColor;

            UpdatePreview();
        }

        private void checkSeparateScales_CheckedChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter.SeparateScales = checkSeparateScales.Checked;
            UpdatePreview();
        }

        private void checkTitleShadowHover_CheckedChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter.ShowTitleShadowOnHover = checkTitleShadowHover.Checked;
            UpdatePreview();
        }

        private void listTitlePosition_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter.TitlePosition = (CounterOptions.DisplayPosition)Enum.Parse(typeof(CounterOptions.DisplayPosition), listTitlePosition.Text);
            if (ActiveCounter.SummaryPosition == ActiveCounter.TitlePosition)
            {
                var vals = Enum.GetValues(typeof(CounterOptions.DisplayPosition)).Cast<CounterOptions.DisplayPosition>().Where(x => x != ActiveCounter.TitlePosition).ToList();
                listSummaryPosition.Text = vals.First().ToString();
                //ActiveCounter.SummaryPosition = vals.First();
            }
            UpdatePreview();
        }

        private void checkValueShadowHover_CheckedChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter.ShowCurrentValueShadowOnHover = checkValueShadowHover.Checked;
            UpdatePreview();
        }

        private void listSummaryPosition_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter.SummaryPosition = (CounterOptions.DisplayPosition)Enum.Parse(typeof(CounterOptions.DisplayPosition), listSummaryPosition.Text);
            if (ActiveCounter.SummaryPosition == ActiveCounter.TitlePosition)
            {
                var vals = Enum.GetValues(typeof(CounterOptions.DisplayPosition)).Cast<CounterOptions.DisplayPosition>().Where(x => x != ActiveCounter.SummaryPosition).ToList();
                listTitlePosition.Text = vals.First().ToString();
                //ActiveCounter.TitlePosition = vals.First();
            }
            UpdatePreview();
        }

        private void linkLatestVersion_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            GithubUpdater update = new GithubUpdater("leandrosa81", "taskbar-monitor");
            System.Diagnostics.Process.Start(update.GetURL());
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonResetDefaults_Click(object sender, EventArgs e)
        {
            Options.DefaultOptions().CopyTo(this.Options);
            GraphTheme.DefaultDarkTheme().CopyTo(this.Theme);
            Initialize();
        }

        private void checkEnabled_CheckedChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter.Enabled = checkEnabled.Checked;
            UpdatePreview();
        }

        private void buttonApply_Click(object sender, EventArgs e)
        {
            this.Options.CopyTo(this.OriginalOptions);
            this.Options.SaveToDisk();

            this.Theme.CopyTo(this.OriginalTheme);
            this.Theme.SaveToDisk();
            this.Close();

            originalControl.ApplyOptions(this.OriginalOptions);
        }

        private void linkTitleFont_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (ChooseFont(sender as LinkLabel, ref ChosenTitleFont))
            {                
                this.Theme.TitleFont = ChosenTitleFont.Name;
                this.Theme.TitleSize = ChosenTitleFont.Size;
            }

            UpdatePreview();
        }

        private void linkCurrentValueFont_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (ChooseFont(sender as LinkLabel, ref ChosenCurrentValueFont))
            {
                this.Theme.CurrentValueFont = ChosenCurrentValueFont.Name;
                this.Theme.CurrentValueSize = ChosenCurrentValueFont.Size;
            }

            UpdatePreview();
        }

        private void listThemeType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Options.ThemeType = (Options.ThemeList)Enum.Parse(typeof(Options.ThemeList), listThemeType.Text);
            UpdatePreview();
            UpdateThemeOptions();
        }

        private void UpdateThemeOptions()
        {
            btnColorBar.Enabled = Options.ThemeType == Options.ThemeList.CUSTOM;
            btnColor1.Enabled = Options.ThemeType == Options.ThemeList.CUSTOM;
            btnColor2.Enabled = Options.ThemeType == Options.ThemeList.CUSTOM;
            btnColorTitle.Enabled = Options.ThemeType == Options.ThemeList.CUSTOM;
            btnColorTitleShadow.Enabled = Options.ThemeType == Options.ThemeList.CUSTOM;
            btnColorCurrentValue.Enabled = Options.ThemeType == Options.ThemeList.CUSTOM;
            btnColorCurrentValueShadow.Enabled = Options.ThemeType == Options.ThemeList.CUSTOM;
            linkTitleFont.Enabled = Options.ThemeType == Options.ThemeList.CUSTOM;
            linkCurrentValueFont.Enabled = Options.ThemeType == Options.ThemeList.CUSTOM;
        }
    }
}
