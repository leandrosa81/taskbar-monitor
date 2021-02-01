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
        public Options Options { get; private set; }
        private CounterOptions ActiveCounter = null;
        private bool initializing = true;
        public OptionForm(Options opt)
        {
            this.Options = opt;
            InitializeComponent();
            this.editHistorySize.Value = opt.HistorySize;
            this.listTheme.Text = "default";
            this.listCounters.DataSource = opt.CounterOptions.Keys.AsEnumerable().ToList();
            this.listShowTitle.DataSource = Enum.GetValues(typeof(CounterOptions.DisplayType));            
            this.listShowCurrentValue.DataSource = Enum.GetValues(typeof(CounterOptions.DisplayType));

            ActiveCounter = opt.CounterOptions.First().Value;
            UpdateForm();
            initializing = false;
        }

        private void EditHistorySize_ValueChanged(object sender, EventArgs e)
        {
            Options.HistorySize = Convert.ToInt32(editHistorySize.Value);
        }

        private void ListCounters_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter = Options.CounterOptions[listCounters.Text];
            UpdateForm();
        }

        private void UpdateForm()
        {
            initializing = true;
            this.listGraphType.DataSource = ActiveCounter.AvailableGraphTypes;
            initializing = false;
            this.listGraphType.Text = ActiveCounter.GraphType.ToString();
            listShowTitle.Text = ActiveCounter.ShowTitle.ToString();
            listShowCurrentValue.Text = ActiveCounter.ShowCurrentValue.ToString();
        }

        private void ListShowTitle_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter.ShowTitle = (CounterOptions.DisplayType)Enum.Parse(typeof(CounterOptions.DisplayType), listShowTitle.Text);            
        }

        private void listShowCurrentValue_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter.ShowCurrentValue = (CounterOptions.DisplayType)Enum.Parse(typeof(CounterOptions.DisplayType), listShowCurrentValue.Text);
        }

        private void listGraphType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            ActiveCounter.GraphType = (TaskbarMonitor.Counters.ICounter.CounterType)Enum.Parse(typeof(TaskbarMonitor.Counters.ICounter.CounterType), listGraphType.Text);
        }
    }
}
