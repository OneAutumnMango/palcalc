using PalCalc.Model;
using PalCalc.Solver.PalReference;
using PalCalc.Solver.Processing;

namespace PalCalc.Solver.Tests.Processing;

[TestClass]
public class InitialPalBuilderTests
{
    [TestMethod]
    public void Build_CombinesEquivalentOppositeGenderOwnedPals()
    {
        var male = SolverTestScenario.Owned(
            "Katress",
            PalGender.MALE
        );
        var female = SolverTestScenario.Owned(
            "Katress",
            PalGender.FEMALE
        );
        var configuredSolver = SolverTestScenario.Solver(
            [male, female],
            maxBreedingSteps: 1
        );

        var seeds = new InitialPalBuilder(
            configuredSolver.Settings,
            configuredSolver.Settings.DB.BreedingMechanics,
            configuredSolver.Settings.BreedingDB
        ).Build(Target());

        Assert.AreEqual(1, seeds.Count);
        var composite =
            seeds.Single() as CompositeOwnedPalReference;
        Assert.IsNotNull(composite);
        Assert.AreSame(male, composite.Male.UnderlyingInstance);
        Assert.AreSame(female, composite.Female.UnderlyingInstance);
    }

    [TestMethod]
    public void Build_SelectsOwnedInstanceWithFewerIrrelevantPassives()
    {
        var irrelevant =
            "Swift".ToStandardPassive(SolverTestScenario.DB);
        var clean = SolverTestScenario.Owned(
            "Katress",
            PalGender.MALE
        );
        var noisy = SolverTestScenario.Owned(
            "Katress",
            PalGender.MALE,
            passives: [irrelevant]
        );
        var configuredSolver = SolverTestScenario.Solver(
            [noisy, clean],
            maxBreedingSteps: 1
        );

        var seeds = new InitialPalBuilder(
            configuredSolver.Settings,
            configuredSolver.Settings.DB.BreedingMechanics,
            configuredSolver.Settings.BreedingDB
        ).Build(Target());

        Assert.AreEqual(1, seeds.Count);
        var selected = seeds.Single() as OwnedPalReference;
        Assert.IsNotNull(selected);
        Assert.AreSame(clean, selected.UnderlyingInstance);
    }

    [TestMethod]
    public void Build_AddsConfiguredWildPassiveCountVariants()
    {
        var katress = "Katress".ToPal(SolverTestScenario.DB);
        var configuredSolver = SolverTestScenario.Solver(
            ownedPals: [],
            maxBreedingSteps: 1,
            maxWildPals: 1,
            allowedWildPals: [katress]
        );

        var seeds = new InitialPalBuilder(
            configuredSolver.Settings,
            configuredSolver.Settings.DB.BreedingMechanics,
            configuredSolver.Settings.BreedingDB
        ).Build(Target());

        CollectionAssert.AreEqual(
            new[] { 0, 1, 2, 3 },
            seeds
                .OfType<WildPalReference>()
                .Select(reference =>
                    reference.EffectivePassives.Count(
                        passive => passive is RandomPassiveSkill
                    )
                )
                .Order()
                .ToArray()
        );
    }

    private static PalSpecifier Target() =>
        new()
        {
            Pal = "Wixen Noct".ToPal(SolverTestScenario.DB),
        };

    // Depresso learns Dark Ball at level 15
    [TestMethod]
    public void Build_KeepsOwnedPalsWithDifferentKnownSkillsWhenUsingCurrentLevels()
    {
        var seeds = DepressoSeeds(useCurrentPalLevels: true, out var low, out var high);

        CollectionAssert.AreEquivalent(
            new[] { low, high },
            seeds.OfType<OwnedPalReference>().Select(r => r.UnderlyingInstance).ToArray()
        );

        var darkBall = SolverTestScenario.DB.ActiveSkills.Single(s => s.Name == "Dark Ball");
        var byInstance = seeds.OfType<OwnedPalReference>().ToDictionary(r => r.UnderlyingInstance);

        Assert.IsFalse(byInstance[low].InheritableActiveSkills.Contains(darkBall));
        Assert.IsTrue(byInstance[high].InheritableActiveSkills.Contains(darkBall));
    }

    [TestMethod]
    public void Build_MergesOwnedPalsOfTheSameSpeciesWhenNotUsingCurrentLevels()
    {
        var seeds = DepressoSeeds(useCurrentPalLevels: false, out _, out _);

        var darkBall = SolverTestScenario.DB.ActiveSkills.Single(s => s.Name == "Dark Ball");

        Assert.AreEqual(1, seeds.Count);
        Assert.IsTrue(seeds.Single().InheritableActiveSkills.Contains(darkBall));
    }

    private static List<IPalReference> DepressoSeeds(
        bool useCurrentPalLevels,
        out PalInstance low,
        out PalInstance high
    )
    {
        low = SolverTestScenario.Owned("Depresso", PalGender.MALE, level: 14);
        high = SolverTestScenario.Owned("Depresso", PalGender.MALE, level: 15);

        var configuredSolver = SolverTestScenario.Solver(
            [low, high],
            maxBreedingSteps: 1,
            gameSettings: new GameSettings { UseCurrentPalLevels = useCurrentPalLevels }
        );

        return new InitialPalBuilder(
            configuredSolver.Settings,
            configuredSolver.Settings.DB.BreedingMechanics,
            configuredSolver.Settings.BreedingDB
        ).Build(new PalSpecifier { Pal = "Depresso".ToPal(SolverTestScenario.DB) });
    }
}
