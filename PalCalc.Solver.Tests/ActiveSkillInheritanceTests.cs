using PalCalc.Model;
using PalCalc.Solver.PalReference;
using PalCalc.Solver.PalReference.Properties;

namespace PalCalc.Solver.Tests;

[TestClass]
public class ActiveSkillInheritanceTests
{
    private static readonly PalDB DB = SolverTestScenario.DB;
    private static readonly GameSettings Settings = new();

    // Loomen learns Dark Ball at level 1; neither Lamball nor Cattiva can learn it.
    private static ActiveSkill Skill(string name) => DB.ActiveSkills.Single(s => s.Name == name);
    private static List<ActiveSkill> Natural(string palName) =>
        ActiveSkillInheritance.NaturalSkillsOf(palName.ToPal(DB));

    private static IPalReference Owned(string palName, PalGender gender) =>
        new OwnedPalReference(
            SolverTestScenario.Owned(palName, gender),
            [],
            new IV_Set(IV_Value.Random, IV_Value.Random, IV_Value.Random)
        );

    private static IPalReference Bred(string palName, IPalReference parent1, IPalReference parent2) =>
        new BredPalReference(
            Settings,
            palName.ToPal(DB),
            parent1,
            parent2,
            [],
            1f,
            new IV_Set(IV_Value.Random, IV_Value.Random, IV_Value.Random),
            1f
        );

    [TestMethod]
    public void NaturalSkillsOf_IncludesLevelUpSkills()
    {
        CollectionAssert.Contains(ActiveSkillInheritance.NaturalSkillsOf("Loomen".ToPal(DB)), Skill("Dark Ball"));

        Assert.IsTrue(ActiveSkillInheritance.LearnsNaturally("Loomen".ToPal(DB), Skill("Dark Ball")));
        Assert.IsFalse(ActiveSkillInheritance.LearnsNaturally("Lamball".ToPal(DB), Skill("Dark Ball")));
        Assert.IsFalse(ActiveSkillInheritance.LearnsNaturally("Cattiva".ToPal(DB), Skill("Dark Ball")));
    }

    [TestMethod]
    public void BredPal_SkillPoolIncludesItsOwnNaturalSkills()
    {
        var loomen = Bred("Loomen", Owned("Lamball", PalGender.MALE), Owned("Cattiva", PalGender.FEMALE));

        CollectionAssert.Contains(loomen.InheritedActiveSkills, Skill("Dark Ball"));
    }

    [TestMethod]
    public void BredPal_SkillPoolPropagatesNaturalSkillsOfAncestors()
    {
        var loomen = Bred("Loomen", Owned("Lamball", PalGender.MALE), Owned("Cattiva", PalGender.FEMALE));
        var descendant = Bred("Cattiva", loomen, Owned("Lamball", PalGender.MALE));

        CollectionAssert.Contains(descendant.InheritedActiveSkills, Skill("Dark Ball"));
        Assert.IsTrue(ActiveSkillInheritance.CanProvide(descendant, [Skill("Dark Ball")]));
    }

    [TestMethod]
    public void PalSpecifier_IsSatisfiedBy_AcceptsSkillLearnedNaturallyByAnAncestor()
    {
        var loomen = Bred("Loomen", Owned("Lamball", PalGender.MALE), Owned("Cattiva", PalGender.FEMALE));
        var descendant = Bred("Cattiva", loomen, Owned("Lamball", PalGender.MALE));

        var spec = new PalSpecifier
        {
            Pal = "Cattiva".ToPal(DB),
            TargetActiveSkills = [Skill("Dark Ball")],
        };

        Assert.IsTrue(spec.IsSatisfiedBy(descendant));
    }

    [TestMethod]
    public void CanProvide_RejectsSkillNoAncestorCanSupply()
    {
        var reachable = Natural("Loomen").Concat(Natural("Lamball")).Concat(Natural("Cattiva")).ToHashSet();
        var unreachable = DB.ActiveSkills.First(s => !reachable.Contains(s));

        var loomen = Bred("Loomen", Owned("Lamball", PalGender.MALE), Owned("Cattiva", PalGender.FEMALE));

        Assert.IsFalse(ActiveSkillInheritance.CanProvide(loomen, [unreachable]));
    }

    [TestMethod]
    public void CanProvide_AllowsUpToThreeSkillsFromASingleParent()
    {
        var loomenNatural = Natural("Loomen").ToHashSet();
        var cattivaNatural = Natural("Cattiva").ToHashSet();

        // only Pengullet can supply these, and Loomen doesn't learn any of them naturally
        var pengulletOnly = Natural("Pengullet")
            .Where(s => !loomenNatural.Contains(s) && !cattivaNatural.Contains(s))
            .ToList();

        Assert.IsTrue(pengulletOnly.Count >= 4);

        var loomen = Bred("Loomen", Owned("Pengullet", PalGender.MALE), Owned("Cattiva", PalGender.FEMALE));

        Assert.IsTrue(ActiveSkillInheritance.CanProvide(loomen, pengulletOnly.Take(3).ToList()));
        Assert.IsFalse(ActiveSkillInheritance.CanProvide(loomen, pengulletOnly.Take(4).ToList()));
    }

    [TestMethod]
    public void CanProvide_IgnoresLimitForSkillsTheChildLearnsNaturally()
    {
        var natural = Natural("Loomen").Take(4).ToList();
        Assert.AreEqual(4, natural.Count);

        var loomen = Bred("Loomen", Owned("Lamball", PalGender.MALE), Owned("Cattiva", PalGender.FEMALE));

        Assert.IsTrue(ActiveSkillInheritance.CanProvide(loomen, natural));
    }
}
