using PalCalc.Model;
using PalCalc.Solver.ResultPruning;

namespace PalCalc.Solver;

public sealed class SolverStateController(CancellationToken cancellationToken = default)
{
    private volatile bool isPaused;

    public CancellationToken CancellationToken { get; } = cancellationToken;
    public bool IsPaused => isPaused;

    public void Pause() => isPaused = true;
    public void Resume() => isPaused = false;

    internal void PauseIfRequested()
    {
        while (isPaused) Thread.Sleep(10);
    }
}

public sealed class BreedingSolverSettings
{
    public BreedingSolverSettings(
        PalDB db,
        PalBreedingDB breedingDB,
        GameSettings gameSettings,
        IEnumerable<PalInstance> ownedPals,
        ResultPruningPolicy resultPruning,
        int maxBreedingSteps,
        int maxSolverIterations,
        int maxWildPals,
        IEnumerable<Pal> allowedWildPals,
        IEnumerable<Pal> bannedBredPals,
        int maxInputIrrelevantPassives,
        int maxBredIrrelevantPassives,
        TimeSpan maxEffort,
        int maxThreads,
        int maxSurgeryCost,
        IEnumerable<PassiveSkill> allowedSurgeryPassives,
        bool useGenderReversers,
        bool useSkillFruits,
        IEnumerable<ActiveSkill> allowedSkillFruitSkills
    )
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(breedingDB);
        ArgumentNullException.ThrowIfNull(gameSettings);
        ArgumentNullException.ThrowIfNull(ownedPals);
        ArgumentNullException.ThrowIfNull(resultPruning);
        ArgumentNullException.ThrowIfNull(allowedWildPals);
        ArgumentNullException.ThrowIfNull(bannedBredPals);
        ArgumentNullException.ThrowIfNull(allowedSurgeryPassives);
        ArgumentNullException.ThrowIfNull(allowedSkillFruitSkills);

        DB = db;
        BreedingDB = breedingDB;
        GameSettings = gameSettings;
        OwnedPals = ownedPals.Where(p => p.Gender != PalGender.NONE).ToList();
        ResultPruning = resultPruning;
        MaxBreedingSteps = maxBreedingSteps;
        MaxSolverIterations = maxSolverIterations;
        MaxWildPals = maxWildPals;
        AllowedWildPals = allowedWildPals.ToList();
        BannedBredPals = bannedBredPals.ToList();
        MaxInputIrrelevantPassives = Math.Clamp(maxInputIrrelevantPassives, 0, GameConstants.MaxTotalPassives);
        MaxBredIrrelevantPassives = Math.Clamp(maxBredIrrelevantPassives, 0, GameConstants.MaxTotalPassives);
        MaxEffort = maxEffort;
        MaxThreads = maxThreads <= 0
            ? Environment.ProcessorCount
            : Math.Clamp(maxThreads, 1, Environment.ProcessorCount);
        MaxSurgeryCost = maxSurgeryCost;
        SurgeryPassives = allowedSurgeryPassives.ToList();
        UseGenderReversers = useGenderReversers;
        UseSkillFruits = useSkillFruits;
        SkillFruitSkills = allowedSkillFruitSkills.ToList();
    }

    public PalDB DB { get; }
    public PalBreedingDB BreedingDB { get; }
    public GameSettings GameSettings { get; }
    public IReadOnlyList<PalInstance> OwnedPals { get; }
    public ResultPruningPolicy ResultPruning { get; }
    public int MaxBreedingSteps { get; }
    public int MaxSolverIterations { get; }
    public int MaxWildPals { get; }
    public IReadOnlyList<Pal> AllowedWildPals { get; }
    public IReadOnlyList<Pal> BannedBredPals { get; }
    public int MaxInputIrrelevantPassives { get; }
    public int MaxBredIrrelevantPassives { get; }
    public TimeSpan MaxEffort { get; }
    public int MaxThreads { get; }
    public int MaxSurgeryCost { get; }
    public IReadOnlyList<PassiveSkill> SurgeryPassives { get; }
    public bool UseGenderReversers { get; }

    /// <summary>
    /// Whether target active skills may be taught directly with skill fruits instead of
    /// being inherited through breeding.
    /// </summary>
    public bool UseSkillFruits { get; }

    /// <summary>
    /// The active skills which may be taught with skill fruits, if <see cref="UseSkillFruits"/>.
    /// </summary>
    public IReadOnlyList<ActiveSkill> SkillFruitSkills { get; }
}
