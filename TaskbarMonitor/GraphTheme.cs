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
        public System.Drawing.FontStyle TitleFontStyle { get; set; } =  FontStyle.Bold;
        public float TitleSize { get; set; } = 7f;

        public string CurrentValueFont { get; set; } = "Arial";
        public System.Drawing.FontStyle CurrentValueFontStyle { get; set; } = FontStyle.Bold;
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
            theme.TitleFontStyle = this.TitleFontStyle;
            theme.TitleSize = this.TitleSize;
            theme.CurrentValueFont = this.CurrentValueFont;
            theme.CurrentValueFontStyle = this.CurrentValueFontStyle;
            theme.CurrentValueSize = this.CurrentValueSize;
            
            theme.StackedColors = new List<Color>();

            foreach (var item in this.StackedColors)
            {                
                theme.StackedColors.Add(item);
            }
        }
        public static GraphTheme DefaultDarkTheme()
        {
            return new GraphTheme
            {
                BarColor = Color.FromArgb(255, 176, 222, 255),
                TextColor = Color.FromArgb(200, 185, 255, 70),
                TextShadowColor = Color.FromArgb(255, 100, 100, 100),
                TitleColor = Color.FromArgb(255, 255, 255, 255),
                TitleShadowColor = Color.FromArgb(255, 100, 100, 100),
                TitleFont = "Arial",
                TitleFontStyle = FontStyle.Bold,
                TitleSize = 7f,
                CurrentValueFont = "Arial",
                CurrentValueFontStyle = FontStyle.Bold,
                CurrentValueSize = 7f,
                StackedColors = new List<Color>
                {
                    Color.FromArgb(255, 37, 84, 142) ,
                    Color.FromArgb(255, 65, 144, 242)
                }
            };
        }
        public static GraphTheme DefaultLightTheme()
        {
            return new GraphTheme
            {
                BarColor = Color.FromArgb(255, 255, 0, 128),
                TextColor = Color.FromArgb(255, 0, 128, 192),
                TextShadowColor = Color.FromArgb(255, 207, 207, 207),
                TitleColor = Color.FromArgb(255, 255, 0, 128),
                TitleShadowColor = Color.FromArgb(255, 170, 170, 170),                
                TitleFont = "Arial",
                TitleFontStyle = FontStyle.Bold,
                TitleSize = 7f,
                CurrentValueFont = "Arial",
                CurrentValueFontStyle = FontStyle.Bold,
                CurrentValueSize = 7f,
                StackedColors = new List<Color>
                {
                    Color.FromArgb(255, 162, 162, 162) ,
                    Color.FromArgb(255, 200, 200, 200)
                }
            };
        }
        public static GraphTheme ReadFromDisk()
        {
            GraphTheme theme = DefaultDarkTheme();

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

            System.IO.File.WriteAllText(origin, JsonConvert.SerializeObject(this, Formatting.Indented));
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

        public static bool IsCustom(GraphTheme theme)
        {
            var light = DefaultLightTheme();
            var dark = DefaultDarkTheme();

            return !IsThemeEqual(theme, light) && !IsThemeEqual(theme, dark);
        }

        private static bool IsThemeEqual(GraphTheme a, GraphTheme b)
        {
            if (a == null || b == null) return false;

            if (a.BarColor.ToArgb() != b.BarColor.ToArgb()) return false;
            if (a.TextColor.ToArgb() != b.TextColor.ToArgb()) return false;
            if (a.TextShadowColor.ToArgb() != b.TextShadowColor.ToArgb()) return false;
            if (a.TitleColor.ToArgb() != b.TitleColor.ToArgb()) return false;
            if (a.TitleShadowColor.ToArgb() != b.TitleShadowColor.ToArgb()) return false;
            if (!string.Equals(a.TitleFont, b.TitleFont, StringComparison.Ordinal)) return false;
            if (a.TitleFontStyle != b.TitleFontStyle) return false;
            if (a.TitleSize != b.TitleSize) return false;
            if (!string.Equals(a.CurrentValueFont, b.CurrentValueFont, StringComparison.Ordinal)) return false;
            if (a.CurrentValueFontStyle != b.CurrentValueFontStyle) return false;
            if (a.CurrentValueSize != b.CurrentValueSize) return false;

            if (!AreStackedColorsEqual(a.StackedColors, b.StackedColors)) return false;

            return true;
        }

        private static bool AreStackedColorsEqual(List<Color> a, List<Color> b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].ToArgb() != b[i].ToArgb()) return false;
            }
            return true;
        }
    }
}
