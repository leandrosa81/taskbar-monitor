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

namespace SystemWatchBand
{    
    public partial class SystemWatcherControl: UserControl
    {
        
        List<Counters.ICounter> Counters;
        System.Drawing.Font fontCounter;
        Font fontTitle;
        int lastSize = 30;
        bool mouseOver = false;

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

            fontCounter = new Font("Helvetica", 7f, FontStyle.Bold);
            fontTitle = new Font("Arial", 7f, FontStyle.Bold);

            Counters = new List<Counters.ICounter>();

            {
                var ct = new Counters.CounterCPU();
                ct.Initialize();
                Counters.Add(ct);
            }
            {
                var ct = new Counters.CounterMemory();
                ct.Initialize();
                Counters.Add(ct);
            }
            {
                var ct = new Counters.CounterDisk();
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

            System.Drawing.SolidBrush brushTitle = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(!mouseOver ? 120 : 250, 150, 150, 150));

            System.Drawing.Graphics formGraphics = e.Graphics;// this.CreateGraphics();
            foreach (var ct in Counters)
            {
                var lista = ct.GetValues(out float currentValue, out float max, out string stringValue);
                formGraphics.FillRectangle(brushRed, new Rectangle(graphPosition + 30, Convert.ToInt32(maximumHeight - ((currentValue * maximumHeight) / max)), 5, Convert.ToInt32((currentValue * maximumHeight) / max)));
                
                var initialGraphPosition = graphPosition + 30 - lista.Count;
                Point[] points = new Point[lista.Count + 2];
                int i = 0;
                foreach (var item in lista)
                {
                    var convertido = Convert.ToInt32((item * maximumHeight) / max);
                    points[i] = new Point(initialGraphPosition + i, Convert.ToInt32(maximumHeight - convertido));
                    i++;
                }
                points[i] = new Point(initialGraphPosition + i, maximumHeight);
                points[i + 1] = new Point(initialGraphPosition, maximumHeight);
                formGraphics.FillPolygon(brushBlue, points);


                var sizeString = formGraphics.MeasureString(stringValue, fontCounter);
                formGraphics.DrawString(stringValue, fontCounter, brushWhite, new RectangleF(graphPosition +  25 - (sizeString.Width / 2 ), (maximumHeight / 2.0f) - (sizeString.Height / 2), sizeString.Width, maximumHeight), new StringFormat());

                var sizeTitle = formGraphics.MeasureString(ct.GetName(), fontTitle);
                formGraphics.DrawString(ct.GetName(), fontTitle, brushTitle, new RectangleF(graphPosition + 25 - (sizeTitle.Width / 2), (maximumHeight - sizeTitle.Height) - 1, sizeTitle.Width, maximumHeight), new StringFormat());
                
                
                graphPosition += 50;
            }
            
            
            brushBlue.Dispose();
            brushRed.Dispose();
            brushWhite.Dispose();
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
    }

    
}
