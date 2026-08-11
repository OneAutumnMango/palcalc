using PalCalc.Model;
using PalCalc.Solver.PalReference;
using PalCalc.Solver.PalReference.Properties;

namespace PalCalc.Solver.Tests;

[TestClass]
public class ActiveSkillInheritanceTests
{
    private static readonly PalDB DB = SolverTestScenario.DB;
    private const int AnyLevel = 70;

    // Loomen learns Dark Ball at level 1, Depresso at level 15; neither Lamball nor Cattiva can learn it.
    private static ActiveSkill Skill(string name) => DB.ActiveSkills.Single(s => s.Name == name);

    private static List<ActiveSkill> Natural(string palName, int maxLevel = AnyLevel) =>
        ActiveSkillInheritance.NaturalSkillsOf(palName.ToPal(DB), maxLevel);

    private static IPalReference Owned(string palName, PalGender gender, int maxLevel = AnyLevel) =>
        new OwnedPalReference(
            SolverTestScenario.Owned(palName, gender),
            [],
            new IV_Set(IV_Value.Random, IV_Value.Random, IV_Value.Random),
            maxLevel
        );

    private static IPalReference Bred(string palName, IPalReference parent1, IPalReference parent2, int maxLevel = AnyLevel) =>
        new BredPalReference(
            new GameSettings { MaxPalLevel = maxLevel },
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
        CollectionAssert.Contains(Natural("Loomen"), Skill("Dark Ball"));

        Assert.IsTrue(ActiveSkillInheritance.LearnsNaturally("Loomen".ToPal(DB), Skill("Dark Ball"), AnyLevel));
        Assert.IsFalse(ActiveSkillInheritance.LearnsNaturally("Lamball".ToPal(DB), Skill("Dark Ball"), AnyLevel));
        Assert.IsFalse(ActiveSkillInheritance.LearnsNaturally("Cattiva".ToPal(DB), Skill("Dark Ball"), AnyLevel));
    }

    [TestMethod]
    public void NaturalSkillsOf_ExcludesSkillsLearnedAboveMaxLevel()
    {
        CollectionAssert.Contains(Natural("Depresso", 15), Skill("Dark Ball"));
        CollectionAssert.DoesNotContain(Natural("Depresso", 14), Skill("Dark Ball"));

        CollectionAssert.Contains(Natural("Loomen", 1), Skill("Dark Ball"));
    }

    [TestMethod]
    public void OwnedPal_SkillPoolRespectsMaxPalLevel()
    {
        CollectionAssert.Contains(Owned("Depresso", PalGender.MALE, 15).InheritedActiveSkills, Skill("Dark Ball"));
        CollectionAssert.DoesNotContain(Owned("Depresso", PalGender.MALE, 14).InheritedActiveSkills, Skill("Dark Ball"));
    }

    [TestMethod]
    public void CanProvide_RejectsSkillLockedBehindTooHighALevel()
    {
        var tooLow = Bred("Cattiva", Owned("Depresso", PalGender.MALE, 14), Owned("Cattiva", PalGender.FEMALE, 14), 14);
        Assert.IsFalse(ActiveSkillInheritance.CanProvide(tooLow, [Skill("Dark Ball")]));

        var highEnough = Bred("Cattiva", Owned("Depresso", PalGender.MALE, 15), Owned("Cattiva", PalGender.FEMALE, 15), 15);
        Assert.IsTrue(ActiveSkillInheritance.CanProvide(highEnough, [Skill("Dark Ball")]));
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
