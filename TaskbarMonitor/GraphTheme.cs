using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskbarMonitor
{

    public class GraphTheme 
    {
        public static readonly int LATESTTHEMEVERSION = 1;
        public int ThemeVersion = LATESTTHEMEVERSION;
        public Color BarColor { get; set; }
        public Color TextColor { get; set; }
        public Color TextShadowColor { get; set; }

        public Color TitleColor { get; set; }
        public Color TitleShadowColor { get; set; }
        public string TitleFont { get; set; } = "Arial";
        public float TitleSize { get; set; } = 7f;

        public string CurrentValueFont { get; set; } = "Arial";
        public float CurrentValueSize { get; set; } = 7f;

        public List<Color> StackedColors { get; set; }

        public Color getNthColor(int total, int n)
        {
            var colors = GetColorGradient(StackedColors.First(), StackedColors.Last(), total);
            return colors.ElementAt(n);
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

        public void CopyTo(GraphTheme theme)
        {
            theme.BarColor = this.BarColor;
            theme.TextColor = this.TextColor;
            theme.TextShadowColor = this.TextShadowColor;
            theme.TitleColor = this.TitleColor;
            theme.TitleShadowColor = this.TitleShadowColor;
            theme.TitleFont = this.TitleFont;
            theme.TitleSize = this.TitleSize;
            theme.CurrentValueFont = this.CurrentValueFont;
            theme.CurrentValueSize = this.CurrentValueSize;
            
            theme.StackedColors = new List<Color>();

            foreach (var item in this.StackedColors)
            {                
                theme.StackedColors.Add(item);
            }
        }
        public static GraphTheme DefaultTheme()
        {
            return new GraphTheme
            {
                BarColor = Color.FromArgb(255, 176, 222, 255),
                TextColor = Color.FromArgb(200, 185, 255, 70),
                TextShadowColor = Color.FromArgb(255, 0, 0, 0),
                TitleColor = Color.FromArgb(255, 255, 255, 255),
                TitleShadowColor = Color.FromArgb(255, 0, 0, 0),
                TitleFont = "Arial",
                TitleSize = 7f,
                CurrentValueFont = "Arial",
                CurrentValueSize = 7f,
                StackedColors = new List<Color>
                {
                    Color.FromArgb(255, 37, 84, 142) ,
                    Color.FromArgb(255, 65, 144, 242)
                }
            };
        }
        public static GraphTheme ReadFromDisk()
        {
            GraphTheme theme = DefaultTheme();

            var folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "taskbar-monitor");
            var origin = System.IO.Path.Combine(folder, "theme.json");
            if (System.IO.File.Exists(origin))
            {
                theme = JsonConvert.DeserializeObject<GraphTheme>(System.IO.File.ReadAllText(origin));
                if (theme.Upgrade()) // do a inplace upgrade
                {
                    theme.SaveToDisk();
                }
            }
            return theme;
        }
        public bool SaveToDisk()
        {
            var folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "taskbar-monitor");
            var origin = System.IO.Path.Combine(folder, "theme.json");
            if (!System.IO.Directory.Exists(folder))
                System.IO.Directory.CreateDirectory(folder);

            System.IO.File.WriteAllText(origin, JsonConvert.SerializeObject(this));
            return true;
        }
        private bool Upgrade()
        {
            if (GraphTheme.LATESTTHEMEVERSION > this.ThemeVersion)
            {
                switch (this.ThemeVersion)
                {
                    case 0:
                        this.ThemeVersion = LATESTTHEMEVERSION;
                        return true;
                    case 1:
                    default:
                        break;
                }
            }
            return false;
        }

    }
}
