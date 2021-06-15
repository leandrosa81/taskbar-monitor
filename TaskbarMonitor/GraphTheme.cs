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
                TextShadowColor = Color.FromArgb(255, 0, 0, 0),
                TitleColor = Color.FromArgb(255, 255, 255, 255),
                TitleShadowColor = Color.FromArgb(255, 0, 0, 0),
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
                BarColor = Color.FromArgb(255, 0, 0, 0),
                TextColor = Color.FromArgb(200, 255, 0, 128),
                TextShadowColor = Color.FromArgb(255, 207, 207, 207),
                TitleColor = Color.FromArgb(255, 0, 0, 0),
                TitleShadowColor = Color.FromArgb(255, 214, 214, 214),
                TitleFont = "Arial",
                TitleFontStyle = FontStyle.Bold,
                TitleSize = 7f,
                CurrentValueFont = "Arial",
                CurrentValueFontStyle = FontStyle.Bold,
                CurrentValueSize = 7f,
                StackedColors = new List<Color>
                {
                    Color.FromArgb(255, 102, 102, 102) ,
                    Color.FromArgb(255, 145, 145, 145)
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

        public static bool IsCustom(GraphTheme theme)
        {
            var light = DefaultLightTheme();
            var dark = DefaultDarkTheme();
            if (theme.BarColor.ToArgb() != light.BarColor.ToArgb() && theme.BarColor.ToArgb() != dark.BarColor.ToArgb())
                return true;
            if (theme.TextColor.ToArgb() != light.TextColor.ToArgb() && theme.TextColor.ToArgb() != dark.TextColor.ToArgb())
                return true;
            if (theme.TextShadowColor.ToArgb() != light.TextShadowColor.ToArgb() && theme.TextShadowColor.ToArgb() != dark.TextShadowColor.ToArgb())
                return true;
            if (theme.TitleColor.ToArgb() != light.TitleColor.ToArgb() && theme.TitleColor.ToArgb() != dark.TitleColor.ToArgb())
                return true;
            if (theme.TitleShadowColor.ToArgb() != light.TitleShadowColor.ToArgb() && theme.TitleShadowColor.ToArgb() != dark.TitleShadowColor.ToArgb())
                return true;
            if (!theme.TitleFont.Equals(light) && !theme.TitleFont.Equals(dark))
                return true;
            if (!theme.TitleSize.Equals(light) && !theme.TitleSize.Equals(dark))
                return true;

            int i = 0;
            
            foreach (var item in theme.StackedColors)
            {
                if (light.StackedColors.Count <= i ) return true;
                if (dark.StackedColors.Count <= i ) return true;
                if (item.ToArgb() != light.StackedColors[i].ToArgb() && item.ToArgb() != dark.StackedColors[i].ToArgb())
                    return true;
                i++;
            }

            return false;
        }

    }
}
