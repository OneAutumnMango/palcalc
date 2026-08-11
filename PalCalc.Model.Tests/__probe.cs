using PalCalc.Model;
using System.Linq;

namespace PalCalc.Model.Tests
{
    [TestClass]
    public class ProbeTests
    {
        [TestMethod]
        public void Probe()
        {
            var db = PalDB.LoadEmbedded();
            Console.WriteLine($"ActiveSkills={db.ActiveSkills.Count} BreedingSkills={db.BreedingSkills.Count} SkillFruits={db.SkillFruits.Count}");
            Console.WriteLine(string.Join(", ", db.SkillFruits.Take(5).Select(f => f.SkillName + " -> " + f.FruitName)));
            var vixySkills = db.BreedingSkills.Values.SelectMany(s => s).Where(ls => ls.PalName == "Vixy").Select(ls => ls.SkillName).Distinct().ToList();
            Console.WriteLine($"Vixy learnable ({vixySkills.Count}): {string.Join(", ", vixySkills)}");
            var noPals = db.BreedingSkills.Where(kvp => kvp.Value.Count == 0).Select(kvp => kvp.Key).ToList();
            Console.WriteLine($"Skills with no pals: {noPals.Count}");
            var unresolved = db.BreedingSkills.Values.SelectMany(s => s).Count(ls => ls.Skill == null);
            Console.WriteLine($"Unresolved skill refs: {unresolved}");
            var fruitUnresolved = db.SkillFruits.Count(f => f.Skill == null);
            Console.WriteLine($"Unresolved fruit refs: {fruitUnresolved}");
        }
    }
}
