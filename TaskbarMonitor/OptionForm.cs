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
        private Options Options { get; set; }
        private Version Version;
        private GraphTheme Theme;
        private CounterOptions ActiveCounter = null;
        private bool initializing = true;
        public OptionForm(Options opt, GraphTheme theme, Version version)
        {
            this.Version = version;
            this.Theme = theme;
            this.Options = opt;
            
            InitializeComponent();
            this.editHistorySize.Value = opt.HistorySize;
            this.editPollTime.Value = opt.PollTime;
            this.listCounters.DataSource = opt.CounterOptions.Keys.AsEnumerable().ToList();
            this.listShowTitle.DataSource = Enum.GetValues(typeof(CounterOptions.DisplayType));            
            this.listShowCurrentValue.DataSource = Enum.GetValues(typeof(CounterOptions.DisplayType));            
            this.listSummaryPosition.DataSource = Enum.GetValues(typeof(CounterOptions.DisplayPosition));
            this.listTitlePosition.DataSource = Enum.GetValues(typeof(CounterOptions.DisplayPosition));

            lblVersion.Text = "v" + version.ToString(3);

            ActiveCounter = opt.CounterOptions.First().Value;
            UpdateForm();
            UpdateReplicateSettingsMenu();
            btnColorBar.BackColor = this.Theme.BarColor;
            btnColorCurrentValue.BackColor = this.Theme.TextColor;
            btnColorCurrentValueShadow.BackColor = this.Theme.TextShadowColor;
            btnColorTitle.BackColor = this.Theme.TitleColor;
            btnColorTitleShadow.BackColor = this.Theme.TitleShadowColor;
            btnColor1.BackColor = this.Theme.StackedColors[0];
            btnColor2.BackColor = this.Theme.StackedColors[1];

            swcPreview.Options = new Options
            {
                CounterOptions = new Dictionary<string, CounterOptions>
        {
            { "CPU", new CounterOptions {
                GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE,
                AvailableGraphTypes = new List<TaskbarMonitor.Counters.ICounter.CounterType>
                {
                    TaskbarMonitor.Counters.ICounter.CounterType.SINGLE,
                    TaskbarMonitor.Counters.ICounter.CounterType.STACKED
                }
            }
                    }
                }
            ,
                HistorySize = 50
        ,
                PollTime = 3
            };
            swcPreview.ApplyOptions();

            initializing = false;
        }

        private void EditHistorySize_ValueChanged(object sender, EventArgs e)
        {
            Options.HistorySize = Convert.ToInt32(editHistorySize.Value);
        }
        private void editPollTime_ValueChanged(object sender, EventArgs e)
        {
            Options.PollTime = Convert.ToInt32(editPollTime.Value);
        }

        private void ListCounters_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter = Options.CounterOptions[listCounters.Text];
            UpdateReplicateSettingsMenu();
            UpdateForm();
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
            this.listGraphType.DataSource = ActiveCounter.AvailableGraphTypes;
            initializing = false;
            this.listGraphType.Text = ActiveCounter.GraphType.ToString();
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
        }

        private void listShowCurrentValue_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter.ShowCurrentValue = (CounterOptions.DisplayType)Enum.Parse(typeof(CounterOptions.DisplayType), listShowCurrentValue.Text);
            UpdateFormShow();
        }

        private void listGraphType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter.GraphType = (TaskbarMonitor.Counters.ICounter.CounterType)Enum.Parse(typeof(TaskbarMonitor.Counters.ICounter.CounterType), listGraphType.Text);
            UpdateFormScales();
            UpdateFormOrder();
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
        }

        private void checkInvertOrder_CheckedChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter.InvertOrder = checkInvertOrder.Checked;
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
         
        private void btnColorBar_Click(object sender, EventArgs e)
        {
            if(ChooseColor(sender as Button))
                this.Theme.BarColor = (sender as Button).BackColor;
        }

        private void btnColorCurrentValue_Click(object sender, EventArgs e)
        {
            if (ChooseColor(sender as Button))
                this.Theme.TextColor = (sender as Button).BackColor;
        }

        private void btnColorCurrentValueShadow_Click(object sender, EventArgs e)
        {
            if (ChooseColor(sender as Button))
                this.Theme.TextShadowColor = (sender as Button).BackColor;
        }

        private void btnColorTitle_Click(object sender, EventArgs e)
        {
            if (ChooseColor(sender as Button))
                this.Theme.TitleColor = (sender as Button).BackColor;
        }

        private void btnColorTitleShadow_Click(object sender, EventArgs e)
        {
            if (ChooseColor(sender as Button))
                this.Theme.TitleShadowColor = (sender as Button).BackColor;
        }

        private void btnColor1_Click(object sender, EventArgs e)
        {
            if (ChooseColor(sender as Button))
                this.Theme.StackedColors[0] = (sender as Button).BackColor;
        }

        private void btnColor2_Click(object sender, EventArgs e)
        {
            if (ChooseColor(sender as Button))
                this.Theme.StackedColors[1] = (sender as Button).BackColor;
        }

        private void checkSeparateScales_CheckedChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter.SeparateScales = checkSeparateScales.Checked;
        }

        private void checkTitleShadowHover_CheckedChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter.ShowTitleShadowOnHover = checkTitleShadowHover.Checked;
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
        }

        private void checkValueShadowHover_CheckedChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter.ShowCurrentValueShadowOnHover = checkValueShadowHover.Checked;
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
        }

        private void linkLatestVersion_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            GithubUpdater update = new GithubUpdater("leandrosa81", "taskbar-monitor");
            System.Diagnostics.Process.Start(update.GetURL());
        }
    }
}
