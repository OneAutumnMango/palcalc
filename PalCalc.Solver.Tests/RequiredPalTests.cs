using PalCalc.Model;
using PalCalc.Solver.PalReference;
using PalCalc.Solver.Utils;

namespace PalCalc.Solver.Tests;

[TestClass]
public class RequiredPalTests
{
    private static IEnumerable<string> InstanceIdsOf(IPalReference reference) =>
        reference.AllReferences()
            .OfType<OwnedPalReference>()
            .Select(r => r.UnderlyingInstance.InstanceId);

    [TestMethod]
    public void Solve_WithoutRequiredPals_MayIgnoreASpecificInstance()
    {
        var katressA = SolverTestScenario.Owned("Katress", PalGender.MALE);
        var katressB = SolverTestScenario.Owned("Katress", PalGender.MALE);
        var wixen = SolverTestScenario.Owned("Wixen", PalGender.FEMALE);

        var solver = SolverTestScenario.Solver(
            [katressA, katressB, wixen],
            maxBreedingSteps: 1,
            maxSolverIterations: 1
        );

        var results = SolverTestScenario.Solve(solver, "Wixen Noct");

        Assert.IsTrue(results.Count > 0);
        Assert.IsFalse(results.All(r => InstanceIdsOf(r).Contains(katressB.InstanceId)));
    }

    [TestMethod]
    public void Solve_WithRequiredPal_UsesThatInstanceInEveryResult()
    {
        var katressA = SolverTestScenario.Owned("Katress", PalGender.MALE);
        var katressB = SolverTestScenario.Owned("Katress", PalGender.MALE);
        var wixen = SolverTestScenario.Owned("Wixen", PalGender.FEMALE);

        var solver = SolverTestScenario.Solver(
            [katressA, katressB, wixen],
            maxBreedingSteps: 1,
            maxSolverIterations: 1
        );

        var results = SolverTestScenario.Solve(
            solver,
            "Wixen Noct",
            requiredInstanceIds: [katressB.InstanceId]
        );

        Assert.IsTrue(results.Count > 0);
        Assert.IsTrue(results.All(r => InstanceIdsOf(r).Contains(katressB.InstanceId)));
    }

    [TestMethod]
    public void Solve_WithSeveralRequiredPals_UsesAllOfThem()
    {
        var katress = SolverTestScenario.Owned("Katress", PalGender.MALE);
        var wixen = SolverTestScenario.Owned("Wixen", PalGender.FEMALE);

        var solver = SolverTestScenario.Solver(
            [katress, wixen],
            maxBreedingSteps: 1,
            maxSolverIterations: 1
        );

        var results = SolverTestScenario.Solve(
            solver,
            "Wixen Noct",
            requiredInstanceIds: [katress.InstanceId, wixen.InstanceId]
        );

        Assert.IsTrue(results.Count > 0);
        Assert.IsTrue(results.All(r =>
            InstanceIdsOf(r).Contains(katress.InstanceId) &&
            InstanceIdsOf(r).Contains(wixen.InstanceId)
        ));
    }

    [TestMethod]
    public void Solve_WithUnusableRequiredPal_ReturnsNoResults()
    {
        var katress = SolverTestScenario.Owned("Katress", PalGender.MALE);
        var wixen = SolverTestScenario.Owned("Wixen", PalGender.FEMALE);
        var unrelated = SolverTestScenario.Owned("Lamball", PalGender.MALE);

        var solver = SolverTestScenario.Solver(
            [katress, wixen, unrelated],
            maxBreedingSteps: 1,
            maxSolverIterations: 1
        );

        var results = SolverTestScenario.Solve(
            solver,
            "Wixen Noct",
            requiredInstanceIds: [unrelated.InstanceId]
        );

        Assert.AreEqual(0, results.Count);
    }
}
