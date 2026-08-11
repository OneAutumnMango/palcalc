using PalCalc.Solver.Processing;

namespace PalCalc.Solver;

/// <summary>
/// Runs a full breeding-path search and reports progress through status
/// snapshots. The request supplies all run-specific data and constraints.
/// </summary>
public sealed class BreedingSolver
{
    public event Action<SolverStatus> StatusUpdated;

    public TimeSpan StatusUpdateInterval { get; set; } = TimeSpan.FromMilliseconds(100);

    public BreedingSolverResult Solve(
        BreedingSolverRequest request,
        SolverStateController controller
    )
    {
        // a blocking diagnostic means no path can exist, so don't spend a full search proving it
        if (SolverDiagnostics.Analyze(request).Any(d => d.Severity == SolverDiagnosticSeverity.Blocking))
            return new([], controller.CancellationToken.IsCancellationRequested);

        var context = SolverRunContext.Create(request, controller);
        var run = new SolverRun(
            context,
            status => StatusUpdated?.Invoke(status),
            StatusUpdateInterval
        );

        return new(
            run.Execute(),
            controller.CancellationToken.IsCancellationRequested
        );
    }
}
