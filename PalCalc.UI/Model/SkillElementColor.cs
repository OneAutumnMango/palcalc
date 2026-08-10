using PalCalc.Model;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace PalCalc.UI.Model
{
    internal static class SkillElementColor
    {
        // sampled from the element banner images in Resources/SkillElements
        private static readonly Dictionary<string, string> Colors = new()
        {
            { "Normal", "#A2877E" },
            { "Fire", "#D65431" },
            { "Water", "#1870D6" },
            { "Electricity", "#CEAA00" },
            { "Leaf", "#66A700" },
            { "Ice", "#19B1C0" },
            { "Earth", "#8F5423" },
            { "Dark", "#3C2355" },
            { "Dragon", "#BB4EE4" },
        };

        private static Dictionary<string, Brush> brushes;
        public static Dictionary<string, Brush> Brushes =>
            brushes ??= PalDB.LoadEmbedded().Elements.ToDictionary(
                e => e.InternalName,
                e =>
                {
                    var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Colors.GetValueOrElse(e.InternalName, "#A2877E")));
                    brush.Freeze();
                    return (Brush)brush;
                }
            );

        public static Brush DefaultBrush => Brushes["Normal"];
    }
}
