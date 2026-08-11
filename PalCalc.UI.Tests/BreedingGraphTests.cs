using PalCalc.Model;
using PalCalc.Solver;
using PalCalc.Solver.PalReference;
using PalCalc.Solver.PalReference.Properties;
using PalCalc.Solver.Tree;
using PalCalc.UI.ViewModel.GraphSharp;

namespace PalCalc.UI.Tests;

[TestClass]
public class BreedingGraphTests
{
    private const int MaxLevel = 70;

    private static readonly PalDB DB = PalDB.LoadEmbedded();

    private static OwnedPalReference Owned(Pal pal, params ActiveSkill[] activeSkills) =>
        new(
            new PalInstance
            {
                InstanceId = "ui-test",
                OwnerPlayerId = "ui-test-player",
                Pal = pal,
                Gender = PalGender.MALE,
                Level = MaxLevel,
                PassiveSkills = [],
                Location = new PalLocation { ContainerId = "ui-test-palbox", Type = LocationType.Palbox, Index = 0 },
                ActiveSkills = activeSkills.ToList(),
                EquippedActiveSkills = [],
            },
            [],
            new IV_Set(IV_Value.Random, IV_Value.Random, IV_Value.Random),
            MaxLevel
        );

    [TestMethod]
    public void ResolveRequiredSkills_ShowsSkillsTaughtByFruitOnTheFinalPal()
    {
        var pal = "Lamball".ToPal(DB);
        var natural = ActiveSkillInheritance.NaturalSkillsOf(pal, MaxLevel);
        var taught = DB.ActiveSkills.First(s => !natural.Contains(s));

        var tree = new BreedingTree(new SkillFruitPalReference(Owned(pal), [taught]));
        var required = BreedingGraph.ResolveRequiredSkills(tree, [taught], MaxLevel);

        CollectionAssert.Contains(required[tree.Root], taught);
    }

    [TestMethod]
    public void ResolveRequiredSkills_IgnoresSkillsTheFinalPalDoesNotHave()
    {
        var pal = "Lamball".ToPal(DB);
        var natural = ActiveSkillInheritance.NaturalSkillsOf(pal, MaxLevel);
        var missing = DB.ActiveSkills.First(s => !natural.Contains(s));

        var tree = new BreedingTree(Owned(pal));
        var required = BreedingGraph.ResolveRequiredSkills(tree, [missing], MaxLevel);

        CollectionAssert.DoesNotContain(required[tree.Root], missing);
    }
}
