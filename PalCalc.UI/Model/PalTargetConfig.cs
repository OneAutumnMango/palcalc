using Newtonsoft.Json;
using PalCalc.Model;
using System.Collections.Generic;
using System.Linq;

namespace PalCalc.UI.Model
{
    /// <summary>
    /// A portable copy of a breeding target's settings, exchanged as JSON via the clipboard. Pals and
    /// skills are stored by internal name so configs can be shared between saves and languages.
    /// </summary>
    public class PalTargetConfig
    {
        public const string FormatId = "palcalc-target";
        public const int CurrentVersion = 1;

        public string Format { get; set; } = FormatId;
        public int Version { get; set; } = CurrentVersion;

        public string Pal { get; set; }
        public List<string> RequiredPassives { get; set; } = [];
        public List<string> OptionalPassives { get; set; } = [];
        public List<string> ActiveSkills { get; set; } = [];
        public PalGender RequiredGender { get; set; } = PalGender.WILDCARD;
        public int MinIV_HP { get; set; }
        public int MinIV_Attack { get; set; }
        public int MinIV_Defense { get; set; }

        public string ToJson() => JsonConvert.SerializeObject(this, Formatting.Indented);

        public static PalTargetConfig FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            PalTargetConfig result;
            try
            {
                result = JsonConvert.DeserializeObject<PalTargetConfig>(json);
            }
            catch (JsonException)
            {
                return null;
            }

            if (result?.Format != FormatId || result.Version > CurrentVersion) return null;
            if (result.Pal == null) return null;

            var db = PalDB.LoadEmbedded();
            if (!db.Pals.Any(p => p.InternalName.Equals(result.Pal, System.StringComparison.OrdinalIgnoreCase)))
                return null;

            result.RequiredPassives ??= [];
            result.OptionalPassives ??= [];
            result.ActiveSkills ??= [];

            return result;
        }
    }
}
