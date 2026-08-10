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
        /// along with the minimum level requirements.
        /// </summary>
        public Dictionary<string, List<LearnableActiveSkill>> BreedingSkills { get; set; }

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

            // Load breeding_skills.json
            try
            {
                using (var stream = Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceStream($"{name}.breeding_skills.json"))
                {
                    if (stream != null)
                    {
                        using (var streamReader = new StreamReader(stream, Encoding.UTF8))
                        {
                            var breedingSkillsJson = streamReader.ReadToEnd();
                            var rawData = JsonConvert.DeserializeObject<Dictionary<string, List<LearnableActiveSkill>>>(breedingSkillsJson);
                            
                            result.BreedingSkills = new Dictionary<string, List<LearnableActiveSkill>>();
                            
                            // Populate skill names and resolve ActiveSkill references
                            foreach (var kvp in rawData)
                            {
                                var skillName = kvp.Key;
                                var learnableSkills = kvp.Value;
                                
                                foreach (var ls in learnableSkills)
                                {
                                    ls.SkillName = skillName;
                                    ls.Skill = result.ActiveSkills.FirstOrDefault(s => s.Name == skillName);
                                }
                                
                                result.BreedingSkills[skillName] = learnableSkills;
                            }
                            
                            logger.Information("Loaded {count} inheritable active skills", result.BreedingSkills.Count);
                        }
                    }
                    else
                    {
                        logger.Warning("breeding_skills.json not found in embedded resources");
                        result.BreedingSkills = new Dictionary<string, List<LearnableActiveSkill>>();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error loading breeding_skills.json");
                result.BreedingSkills = new Dictionary<string, List<LearnableActiveSkill>>();
            }

            logger.Information("Successfully loaded embedded pal DB in {ms}ms", sw.ElapsedMilliseconds);
            return result;
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
