using PalCalc.Model;

namespace PalCalc.Solver.Tests;

[TestClass]
public class SolverDiagnosticsTests
{
    private static readonly PalDB DB = SolverTestScenario.DB;

    private static ActiveSkill Skill(string name) => DB.ActiveSkills.Single(s => s.Name == name);

    private static PassiveSkill Passive(string name) => name.ToStandardPassive(DB);

    private static IReadOnlyList<SolverDiagnostic> Analyze(
        SolverTestScenario.ConfiguredSolver solver,
        string targetPal,
        IEnumerable<PassiveSkill>? requiredPassives = null,
        IEnumerable<ActiveSkill>? targetActiveSkills = null
    ) =>
        SolverDiagnostics.Analyze(
            new BreedingSolverRequest(
                new PalSpecifier
                {
                    Pal = targetPal.ToPal(DB),
                    RequiredPassives = requiredPassives?.ToList() ?? [],
                    TargetActiveSkills = targetActiveSkills?.ToList() ?? [],
                },
                solver.Settings
            )
        );

    private static bool Has(IReadOnlyList<SolverDiagnostic> diagnostics, SolverDiagnosticCode code) =>
        diagnostics.Any(d => d.Code == code);

    [TestMethod]
    public void ReportsMissingOwnedPals()
    {
        var diagnostics = Analyze(SolverTestScenario.Solver([]), "Lamball");

        Assert.IsTrue(Has(diagnostics, SolverDiagnosticCode.NoOwnedPals));
    }

    [TestMethod]
    public void ReportsTargetOutOfBreedingStepRange()
    {
        var solver = SolverTestScenario.Solver(
            [SolverTestScenario.Owned("Lamball", PalGender.MALE)],
            maxBreedingSteps: 0
        );

        var diagnostics = Analyze(solver, "Beakon");
        var reported = diagnostics.Single(d => d.Code == SolverDiagnosticCode.NoPalsWithinBreedingSteps);

        Assert.IsTrue(reported.Value > 0);
    }

    [TestMethod]
    public void ReportsBannedTargetPal()
    {
        var solver = SolverTestScenario.Solver(
            [SolverTestScenario.Owned("Lamball", PalGender.MALE)],
            bannedBredPals: ["Beakon".ToPal(DB)]
        );

        Assert.IsTrue(Has(Analyze(solver, "Beakon"), SolverDiagnosticCode.TargetPalBanned));
    }

    [TestMethod]
    public void ReportsActiveSkillBlockedByMaxPalLevel()
    {
        // Depresso learns Dark Ball at 15, Loomen at 1
        var darkBall = Skill("Dark Ball");

        var blocked = Analyze(
            SolverTestScenario.Solver(
                [SolverTestScenario.Owned("Lamball", PalGender.MALE)],
                gameSettings: new GameSettings { MaxPalLevel = 0 }
            ),
            "Loomen",
            targetActiveSkills: [darkBall]
        );

        var reported = blocked.Single(d => d.Code == SolverDiagnosticCode.ActiveSkillAboveLevelCap);
        Assert.AreEqual(darkBall, reported.ActiveSkill);
        Assert.AreEqual(1, reported.Value);
        Assert.IsTrue(reported.SuggestedPals.Count > 0);

        var allowed = Analyze(
            SolverTestScenario.Solver(
                [SolverTestScenario.Owned("Lamball", PalGender.MALE)],
                gameSettings: new GameSettings { MaxPalLevel = 1 }
            ),
            "Loomen",
            targetActiveSkills: [darkBall]
        );

        Assert.IsFalse(Has(allowed, SolverDiagnosticCode.ActiveSkillAboveLevelCap));
    }

    [TestMethod]
    public void ReportsActiveSkillWhichCannotBeBred()
    {
        var exclusive = DB.ActiveSkills.First(s => !DB.BreedingSkills.ContainsKey(s.Name));

        var diagnostics = Analyze(
            SolverTestScenario.Solver([SolverTestScenario.Owned("Lamball", PalGender.MALE)]),
            "Lamball",
            targetActiveSkills: [exclusive]
        );

        Assert.AreEqual(exclusive, diagnostics.Single(d => d.Code == SolverDiagnosticCode.ActiveSkillNotBreedable).ActiveSkill);
    }

    [TestMethod]
    public void IgnoresActiveSkillsWhichWillBeTaughtWithFruit()
    {
        var exclusive = DB.ActiveSkills.First(s => !DB.BreedingSkills.ContainsKey(s.Name));

        var settings = SolverTestScenario.Solver(
            [SolverTestScenario.Owned("Lamball", PalGender.MALE)],
            allowedSkillFruitSkills: [exclusive]
        ).Settings;

        var diagnostics = SolverDiagnostics.Analyze(
            new BreedingSolverRequest(
                new PalSpecifier { Pal = "Lamball".ToPal(DB), TargetActiveSkills = [exclusive] },
                settings
            )
        );

        Assert.IsFalse(Has(diagnostics, SolverDiagnosticCode.ActiveSkillNotBreedable));
    }

    [TestMethod]
    public void ReportsUnobtainableRequiredPassive()
    {
        var passive = DB.StandardPassiveSkills.First(p => !p.SupportsSurgery);
        var solver = SolverTestScenario.Solver([SolverTestScenario.Owned("Lamball", PalGender.MALE)]);

        var diagnostics = Analyze(solver, "Lamball", requiredPassives: [passive]);

        Assert.AreEqual(passive, diagnostics.Single(d => d.Code == SolverDiagnosticCode.RequiredPassiveUnavailable).PassiveSkill);
    }

    [TestMethod]
    public void ReportsRequiredPassiveWhichOnlySurgeryCanProvide()
    {
        var passive = DB.SurgeryPassiveSkills.First();
        var owned = new[] { SolverTestScenario.Owned("Lamball", PalGender.MALE) };

        var surgeryDisabled = Analyze(SolverTestScenario.Solver(owned), "Lamball", requiredPassives: [passive]);
        Assert.AreEqual(passive, surgeryDisabled.Single(d => d.Code == SolverDiagnosticCode.RequiredPassiveNeedsSurgery).PassiveSkill);

        var budgetTooLow = Analyze(
            SolverTestScenario.Solver(owned, maxSurgeryCost: 0, allowedSurgeryPassives: [passive]),
            "Lamball",
            requiredPassives: [passive]
        );
        Assert.AreEqual(passive.SurgeryCost, budgetTooLow.Single(d => d.Code == SolverDiagnosticCode.SurgeryCostTooLow).Value);

        var affordable = Analyze(
            SolverTestScenario.Solver(owned, maxSurgeryCost: passive.SurgeryCost, allowedSurgeryPassives: [passive]),
            "Lamball",
            requiredPassives: [passive]
        );
        Assert.IsFalse(Has(affordable, SolverDiagnosticCode.SurgeryCostTooLow));
    }

    [TestMethod]
    public void AcceptsRequiredPassiveHeldByAnOwnedPal()
    {
        var runner = Passive("Runner");
        var solver = SolverTestScenario.Solver([
            SolverTestScenario.Owned("Lamball", PalGender.MALE, [runner])
        ]);

        var diagnostics = Analyze(solver, "Lamball", requiredPassives: [runner]);

        Assert.IsFalse(Has(diagnostics, SolverDiagnosticCode.RequiredPassiveUnavailable));
    }

    [TestMethod]
    public void FallsBackToAHintWhenNothingIsProvablyImpossible()
    {
        var solver = SolverTestScenario.Solver([
            SolverTestScenario.Owned("Lamball", PalGender.MALE),
            SolverTestScenario.Owned("Cattiva", PalGender.FEMALE),
        ]);

        var diagnostics = Analyze(solver, "Loomen");

        Assert.AreEqual(SolverDiagnosticCode.NoObviousCause, diagnostics.Single().Code);
        Assert.AreEqual(SolverDiagnosticSeverity.Hint, diagnostics.Single().Severity);
    }

    [TestMethod]
    public void ReportedCausesAreConsistentWithAnEmptySolve()
    {
        // a target which really is impossible: no pal can learn Dark Ball at level 0
        var solver = SolverTestScenario.Solver(
            [
                SolverTestScenario.Owned("Lamball", PalGender.MALE),
                SolverTestScenario.Owned("Cattiva", PalGender.FEMALE),
            ],
            gameSettings: new GameSettings { MaxPalLevel = 0 }
        );

        var request = new BreedingSolverRequest(
            new PalSpecifier { Pal = "Loomen".ToPal(DB), TargetActiveSkills = [Skill("Dark Ball")] },
            solver.Settings
        );

        var results = solver.Solver.Solve(request, new SolverStateController(CancellationToken.None)).Results;

        Assert.AreEqual(0, results.Count);
        Assert.IsTrue(SolverDiagnostics.Analyze(request).Any(d => d.Severity == SolverDiagnosticSeverity.Blocking));
    }

    [TestMethod]
    public void SolveStopsImmediatelyWhenATargetSkillCannotBeBred()
    {
        var exclusive = DB.ActiveSkills.First(s => !DB.BreedingSkills.ContainsKey(s.Name));

        var solver = SolverTestScenario.Solver([
            SolverTestScenario.Owned("Lamball", PalGender.MALE),
            SolverTestScenario.Owned("Cattiva", PalGender.FEMALE),
        ]);

        var request = new BreedingSolverRequest(
            new PalSpecifier { Pal = "Lamball".ToPal(DB), TargetActiveSkills = [exclusive] },
            solver.Settings
        );

        var expanded = 0;
        solver.Solver.StatusUpdated += _ => expanded++;

        var results = solver.Solver.Solve(request, new SolverStateController(CancellationToken.None)).Results;

        Assert.AreEqual(0, results.Count);
        Assert.AreEqual(0, expanded);
    }
}
