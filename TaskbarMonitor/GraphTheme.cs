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
         
        public Color BarColor { get; set; }
        public Color TextColor { get; set; }
        public Color TextShadowColor { get; set; }

        public Color TitleColor { get; set; }
        public Color TitleShadowColor { get; set; }

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
         
    }
}
