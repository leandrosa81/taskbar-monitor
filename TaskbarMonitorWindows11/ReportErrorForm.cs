using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaskbarMonitorWindows11
{
    public partial class ReportErrorForm : Form
    {
        int[] sizes = new int[] {260, 550};
        bool moreDetails = false;
        bool running = true;
        public Exception Exception { get; private set; }
        public ReportErrorForm(Exception ex)
        {
            this.Exception = ex;
            InitializeComponent();

            this.Height = moreDetails ? sizes[1] : sizes[0];
            this.txtMoreDetails.Text = BuildReport();
        }
        public void Run()
        {
            while (running)
            {
                Application.DoEvents();
            }
        }

        private void btnMoreDetails_Click(object sender, EventArgs e)
        {
            moreDetails = !moreDetails;

            this.Height = moreDetails ? sizes[1] : sizes[0];
            btnMoreDetails.Text = moreDetails ? "Less details <<<" : "More details >>>";
            
        }

        private void ReportErrorForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            running = false;
        }

        private string BuildReport()
        {
            string stack = "";
            if (Exception != null)
            {
                Exception ex = Exception;
                int i = 0;
                while (ex != null)
                {
                    string tabs = new String(' ', i * 2);

                    stack += tabs + "Exception:\r\n";
                    stack += tabs + " " + Exception.Message + "\r\n";
                    stack += tabs + "Source:\n";
                    stack += tabs + " " + Exception.Source + "\r\n";
                    if (Exception.TargetSite != null)
                    {
                        stack += tabs + "Target site:\n";
                        stack += tabs + " " + Exception.TargetSite.Name + "\r\n";
                    }
                    stack += tabs + "Stack trace:\n";
                    stack += tabs + " " + Exception.StackTrace + "\r\n" + "\r\n";


                    ex = Exception.InnerException;
                    i++;
                }
            }
            
            string report = "";
            
            report += "Version: " + Application.ProductVersion + "\r\n";
            report += "Date: " + DateTime.Now.ToShortDateString() + "\r\n";
            report += "Time: " + DateTime.Now.ToLongTimeString() + "\r\n";
            report += "\r\n";
            report += stack;
            return report;
        }
    }
}
