using Newtonsoft.Json;

namespace PalCalc.Model
{
    /// <summary>
    /// An active skill which can be taught directly to any pal by feeding it the given fruit item.
    /// </summary>
    public class SkillFruit
    {
        [JsonProperty("fruit_name")]
        public string FruitName { get; set; }

        [JsonProperty("skill_name")]
        public string SkillName { get; set; }

        [JsonIgnore]
        public ActiveSkill Skill { get; set; }

        public override string ToString() => FruitName;
    }

    /// <summary>
    /// An active skill which is exclusive to a single pal and cannot be inherited through breeding.
    /// </summary>
    public class ExclusiveSkill
    {
        [JsonProperty("skill_name")]
        public string SkillName { get; set; }

        [JsonProperty("pal_owner")]
        public string PalOwner { get; set; }

        public override string ToString() => $"{SkillName} ({PalOwner})";
    }
}
