using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.Model
{
    [JsonConverter(typeof(PalDBJsonConverter))]
    public class PalDB
    {
        internal PalDB() { }

        public string Version { get; set; }

        public Dictionary<PalId, Pal> PalsById { get; set; }

        public List<Human> Humans { get; set; }

        public Dictionary<Pal, Dictionary<PalGender, float>> BreedingGenderProbability { get; set; }

        public BreedingMechanics BreedingMechanics { get; set; }

        public List<PassiveSkill> PassiveSkills { get; set; }

        // passive skills which most users would expect, does *not* include passive effects
        // from items, partner skills, etc.
        public IEnumerable<PassiveSkill> StandardPassiveSkills => PassiveSkills.Where(p => p.IsStandardPassiveSkill);

        public IEnumerable<PassiveSkill> SurgeryPassiveSkills => PassiveSkills.Where(p => p.SupportsSurgery);

        public List<PalElement> Elements { get; set; }
        public List<ActiveSkill> ActiveSkills { get; set; }

        /// <summary>
        /// Dictionary mapping active skill names to lists of pals that can pass them through breeding,
        /// along with the minimum level requirements. Excludes pal-exclusive skills.
        /// </summary>
        public Dictionary<string, List<LearnableActiveSkill>> BreedingSkills { get; set; }

        /// <summary>
        /// Active skills which can be taught to any pal by feeding it a skill fruit.
        /// </summary>
        public List<SkillFruit> SkillFruits { get; set; } = [];

        public IEnumerable<ActiveSkill> SkillFruitSkills => SkillFruits.Select(f => f.Skill);

        public IEnumerable<Pal> Pals => PalsById.Values;

        private Dictionary<string, PassiveSkill> standardPassiveSkillsByName;
        public Dictionary<string, PassiveSkill> StandardPassiveSkillsByName =>
            standardPassiveSkillsByName ??= StandardPassiveSkills.GroupBy(t => t.Name).ToDictionary(t => t.Key, t => t.First(), StringComparer.OrdinalIgnoreCase);

        private Dictionary<Pal, PalGender> breedingMostLikelyGender;
        public Dictionary<Pal, PalGender> BreedingMostLikelyGender =>
            breedingMostLikelyGender ??= Pals.ToDictionary(
                p => p,
                p =>
                {
                    var genderProbability = BreedingGenderProbability[p];
                    var maleProbability = genderProbability[PalGender.MALE];
                    var femaleProbability = genderProbability[PalGender.FEMALE];

                    if (maleProbability > femaleProbability) return PalGender.MALE;
                    else if (femaleProbability > maleProbability) return PalGender.FEMALE;
                    else return PalGender.WILDCARD;
                }
            );


        private Dictionary<Pal, PalGender> breedingLeastLikelyGender;
        public Dictionary<Pal, PalGender> BreedingLeastLikelyGender =>
            breedingLeastLikelyGender ??= BreedingMostLikelyGender.ToDictionary(
                kvp => kvp.Key,
                kvp =>
                {
                    if (kvp.Value == PalGender.WILDCARD) return PalGender.WILDCARD;
                    else if (kvp.Value == PalGender.MALE) return PalGender.FEMALE;
                    else return PalGender.MALE;
                }
            );

        private static ILogger logger = Log.ForContext<PalDB>();

        private static object loadEmbeddedLock = new object();
        private static PalDB embedded = null;

        private static PalDB _LoadEmbedded()
        {
            logger.Information("Loading embedded pal DB");
            var info = Assembly.GetExecutingAssembly().GetName();
            var name = info.Name;
            
            var sw = Stopwatch.StartNew();
            PalDB result;
            
            // Load main db.json
            using (var stream = Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream($"{name}.db.json")!)
            using (var streamReader = new StreamReader(stream, Encoding.UTF8))
            {
                result = FromJson(streamReader.ReadToEnd());
            }

            result.BreedingSkills = [];
            result.SkillFruits = [];

            try
            {
                var skillsByName = result.ActiveSkills.GroupBy(s => s.Name).ToDictionary(g => g.Key, g => g.First());

                var exclusiveSkills = ReadEmbeddedJson<List<ExclusiveSkill>>($"{name}.exclusive_skills.json") ?? [];
                var exclusiveSkillNames = exclusiveSkills.Select(s => s.SkillName).ToHashSet();

                result.SkillFruits = (ReadEmbeddedJson<List<SkillFruit>>($"{name}.skill_fruits.json") ?? [])
                    .Where(f => skillsByName.ContainsKey(f.SkillName))
                    .DistinctBy(f => f.SkillName)
                    .ToList();

                foreach (var fruit in result.SkillFruits)
                    fruit.Skill = skillsByName[fruit.SkillName];

                var rawBreedingSkills = ReadEmbeddedJson<Dictionary<string, List<LearnableActiveSkill>>>($"{name}.breeding_skills.json") ?? [];

                // pal-exclusive skills can't be inherited through breeding
                foreach (var (skillName, learnableSkills) in rawBreedingSkills)
                {
                    if (exclusiveSkillNames.Contains(skillName)) continue;
                    if (!skillsByName.TryGetValue(skillName, out var skill)) continue;

                    foreach (var ls in learnableSkills)
                    {
                        ls.SkillName = skillName;
                        ls.Skill = skill;
                    }

                    result.BreedingSkills[skillName] = learnableSkills;
                }

                var inheritanceIndex = 0;
                foreach (var skillName in result.BreedingSkills.Keys.OrderBy(n => n, StringComparer.Ordinal))
                    skillsByName[skillName].InheritanceIndex = inheritanceIndex++;

                logger.Information(
                    "Loaded {count} inheritable active skills ({excluded} exclusive skills ignored), {fruits} skill fruits",
                    result.BreedingSkills.Count,
                    exclusiveSkillNames.Count,
                    result.SkillFruits.Count
                );
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error loading active skill data");
            }

            logger.Information("Successfully loaded embedded pal DB in {ms}ms", sw.ElapsedMilliseconds);
            return result;
        }

        private static T ReadEmbeddedJson<T>(string resourceName)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                logger.Warning("{name} not found in embedded resources", resourceName);
                return default;
            }

            using var streamReader = new StreamReader(stream, Encoding.UTF8);
            return JsonConvert.DeserializeObject<T>(streamReader.ReadToEnd());
        }

        public static void BeginLoadEmbedded()
        {
            Task.Run(() =>
            {
                lock (loadEmbeddedLock)
                {
                    if (embedded != null)
                    {
                        logger.Verbose("Pal DB already loaded");
                        return;
                    }

                    embedded = _LoadEmbedded();
                }
            });
        }

        public static PalDB LoadEmbedded()
        {
            lock (loadEmbeddedLock)
            {
                if (embedded != null)
                {
                    logger.Verbose("Using previously-loaded pal DB");
                    return embedded;
                }

                embedded = _LoadEmbedded();
                return embedded;
            }
        }

        public static PalDB FromJson(string json) => JsonConvert.DeserializeObject<PalDB>(json);

        public string ToJson() => JsonConvert.SerializeObject(this);

        // should only be used when constructing a DB for serialization
        public static PalDB MakeEmptyUnsafe(string version) => new PalDB() { Version = version };
    }
}
