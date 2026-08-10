using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PalCalc.Model
{
    /// <summary>
    /// Represents an active skill that can be learned/passed through breeding,
    /// along with the minimum level requirement for the pal to have it.
    /// </summary>
    public class LearnableActiveSkill
    {
        [JsonProperty("pal_name")]
        public string PalName { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonIgnore]
        public string SkillName { get; set; }

        [JsonIgnore]
        public ActiveSkill Skill { get; set; }

        public override string ToString() => $"{PalName} - {SkillName ?? "?"} (Level {Level})";
    }
}
