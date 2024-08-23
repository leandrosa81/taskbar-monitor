using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace TaskbarMonitor
{
    public partial class ScreenPositioning : UserControl
    {
        public ScreenPositioning()
        {
            InitializeComponent();
            SelectedScreen = Screen.AllScreens.First();
        }

        public Screen SelectedScreen { get; private set; }

        public delegate void SelectedScreenChangeHandler(Screen selectedScreen);
        public event SelectedScreenChangeHandler OnSelectedScreenChange;

        private void ScreenPositioning_Paint(object sender, PaintEventArgs e)
        {
            System.Drawing.Graphics formGraphics = e.Graphics;
            formGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;//AntiAliasGridFit;
            formGraphics.Clear(Color.FromArgb(255, 245, 245, 245));

            using (SolidBrush BrushBack = new SolidBrush(Color.FromArgb(255, 220, 220, 220)))
            {
                using (SolidBrush BrushFront = new SolidBrush(Color.FromArgb(255, 20, 20, 20)))
                {
                    using (SolidBrush BrushSelectedBack = new SolidBrush(Color.FromArgb(255, 0, 120, 212)))
                    {
                        using (SolidBrush BrushSelectedBorder = new SolidBrush(Color.FromArgb(255, 0, 0, 0)))
                        {
                            using (SolidBrush BrushSelectedFront = new SolidBrush(Color.White))
                            {
                                using (Font font = new Font("Arial", 15))
                                {
                                    using (Font pFont = new Font("Arial", 6))
                                    {
                                        Action<Screen> draw = (screen) =>
                                        {
                                            Rectangle adjustedSize = GetAdjustedSize(screen.Bounds);

                                            string text = screen.DeviceName;
                                            var match = Regex.Match(text, @"(\d+)");
                                            if (match.Success)
                                                text = match.Groups[1].Value;

                                            var textSize = formGraphics.MeasureString(text, font);
                                            RectangleF textLocation = new RectangleF(adjustedSize.X + (adjustedSize.Width / 2) - (textSize.Width / 2), adjustedSize.Y + (adjustedSize.Height / 2) - (textSize.Height / 2), textSize.Width, textSize.Height);


                                            if (screen == SelectedScreen)
                                            {
                                                formGraphics.FillRoundedRectangle(BrushSelectedBack, adjustedSize, 15);
                                                using (Pen pen = new Pen(BrushSelectedBorder, 2))
                                                    formGraphics.DrawRoundedRectangle(pen, adjustedSize, 15);

                                                formGraphics.DrawString(text, font, BrushSelectedFront,
                                                    textLocation,
                                                    new StringFormat());
                                            }
                                            else
                                            {
                                                formGraphics.FillRoundedRectangle(BrushBack, adjustedSize, 15);
                                                formGraphics.DrawString(text, font, BrushFront,
                                                    textLocation,
                                                    new StringFormat());
                                            }
                                            if(screen.Primary)
                                            {
                                                text = "(primary)";
                                                textSize = formGraphics.MeasureString(text, pFont);
                                                textLocation = new RectangleF(adjustedSize.X + (adjustedSize.Width / 2) - (textSize.Width / 2), adjustedSize.Y + (adjustedSize.Height / 2) - (textSize.Height / 2) + 18, textSize.Width, textSize.Height);
                                                formGraphics.DrawString(text, pFont, BrushFront,
                                                    textLocation,
                                                    new StringFormat());
                                            }
                                        };
                                        foreach (var screen in Screen.AllScreens.Where(x => x != SelectedScreen))
                                        {
                                            draw(screen);
                                        }
                                        foreach (var screen in Screen.AllScreens.Where(x => x == SelectedScreen))
                                        {
                                            draw(screen);
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
            }
        }

        private Rectangle GetAdjustedSize(Rectangle bounds)
        {
            int maxWidth = Convert.ToInt32(Screen.AllScreens.Max(x => x.Bounds.Right));
            int maxHeight = Convert.ToInt32(Screen.AllScreens.Max(x => x.Bounds.Bottom));

            int adjustedMaxWidth = Convert.ToInt32(this.Size.Width * 0.6);
            int adjustedMaxHeight = Convert.ToInt32(this.Size.Height * 0.6);

            int offsetX = (this.Size.Width / 2) - (adjustedMaxWidth / 2);
            int offsetY = (this.Size.Height / 2) - (adjustedMaxHeight / 2);

            Rectangle adjustedSize = new Rectangle(
                    ((adjustedMaxWidth * bounds.Left) / maxWidth) + offsetX,
                    ((adjustedMaxHeight * bounds.Top) / maxHeight) + offsetY,
                    (adjustedMaxWidth * (bounds.Right - bounds.Left)) / maxWidth,
                    (adjustedMaxHeight * (bounds.Bottom - bounds.Top)) / maxHeight
                    );

            return adjustedSize;
        }
 
        private void ScreenPositioning_MouseClick(object sender, MouseEventArgs e)
        {
            Screen selectedScreen = null;
            // detect if we click inside any monitor bounds rectangle
            foreach (var screen in Screen.AllScreens)
            {
                Rectangle adjustedSize = GetAdjustedSize(screen.Bounds);
                if(e.X >= adjustedSize.X 
                    && e.X <= adjustedSize.X + adjustedSize.Width
                    && e.Y >= adjustedSize.Y
                    && e.Y <= adjustedSize.Y + adjustedSize.Height)
                {
                    selectedScreen = screen;
                }

            }

            if(selectedScreen != null && selectedScreen != SelectedScreen)
            {
                SelectedScreen = selectedScreen;
                // fire event
                if(OnSelectedScreenChange != null)
                    OnSelectedScreenChange(SelectedScreen);
                Invalidate();
            }
        }
    }

    public static class GraphicsExtensions
    {
        public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            // top left arc  
            path.AddArc(arc, 180, 90);

            // top right arc  
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // bottom right arc  
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // bottom left arc 
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int cornerRadius)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");
            if (pen == null)
                throw new ArgumentNullException("pen");

            using (GraphicsPath path = RoundedRect(bounds, cornerRadius))
            {
                graphics.DrawPath(pen, path);
            }
        }

        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int cornerRadius)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");
            if (brush == null)
                throw new ArgumentNullException("brush");

            using (GraphicsPath path = RoundedRect(bounds, cornerRadius))
            {
                graphics.FillPath(brush, path);
            }
        }
    }
}
