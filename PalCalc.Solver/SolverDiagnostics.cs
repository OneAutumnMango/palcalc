using PalCalc.Model;

namespace PalCalc.Solver;

public enum SolverDiagnosticCode
{
    NoOwnedPals,
    NoPalsWithinBreedingSteps,
    AllOwnedPalsFilteredByIrrelevantPassives,
    TargetPalBanned,
    ActiveSkillNotBreedable,
    ActiveSkillAboveLevelCap,
    ActiveSkillNeedsLevelling,
    ActiveSkillNoAvailableSource,
    ActiveSkillNeedsWildPals,
    RequiredPassiveUnavailable,
    RequiredPassiveNeedsSurgery,
    SurgeryCostTooLow,
    NoObviousCause,
}

public enum SolverDiagnosticSeverity
{
    /// <summary>The run could not possibly have succeeded while this holds.</summary>
    Blocking,
    /// <summary>Not proven fatal, but the most likely thing to change.</summary>
    Hint,
}

public sealed record SolverDiagnostic(SolverDiagnosticCode Code, SolverDiagnosticSeverity Severity)
{
    public Pal Pal { get; init; }
    public ActiveSkill ActiveSkill { get; init; }
    public PassiveSkill PassiveSkill { get; init; }

    /// <summary>A code-specific number, e.g. the level a skill is learned at.</summary>
    public int Value { get; init; }

    /// <summary>Pals the user could obtain to resolve this, at most a handful.</summary>
    public IReadOnlyList<Pal> SuggestedPals { get; init; } = [];
}

/// <summary>
/// Explains why a solver run produced no results. Every check is a necessary condition for
/// success, so a <see cref="SolverDiagnosticSeverity.Blocking"/> entry always means the run
/// could not have succeeded - never the other way around.
/// </summary>
public static class SolverDiagnostics
{
    private const int MaxSuggestedPals = 5;

    public static IReadOnlyList<SolverDiagnostic> Analyze(BreedingSolverRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var settings = request.Settings;
        var db = settings.DB;
        var breedingDB = settings.BreedingDB;

        var target = request.Target;
        if (settings.UseSkillFruits && target.TargetActiveSkills.Count > 0)
            target.TargetActiveSkills = target.TargetActiveSkills.Except(settings.SkillFruitSkills).ToList();

        bool WithinSteps(Pal pal) =>
            breedingDB.MinBreedingSteps[pal][target.Pal] <= settings.MaxBreedingSteps;

        var results = new List<SolverDiagnostic>();
        void Add(SolverDiagnostic diagnostic) => results.Add(diagnostic);

        if (settings.BannedBredPals.Contains(target.Pal))
            Add(new(SolverDiagnosticCode.TargetPalBanned, SolverDiagnosticSeverity.Blocking) { Pal = target.Pal });

        var wildPals = settings.MaxWildPals > 0 ? settings.AllowedWildPals : [];
        var usableOwned = CheckOwnedPals(settings, target, WithinSteps, wildPals, Add);

        // species that could appear anywhere in a breeding tree ending at the target
        var reachableSpecies = db.Pals.Where(WithinSteps).ToHashSet();
        var obtainableSpecies = usableOwned.Select(p => p.Pal).Concat(wildPals).ToHashSet();

        CheckActiveSkills(db, settings.GameSettings, target, reachableSpecies, obtainableSpecies, usableOwned, settings.MaxWildPals > 0, Add);
        CheckRequiredPassives(db, settings, target, usableOwned, wildPals, Add);

        if (results.Count == 0)
            Add(new(SolverDiagnosticCode.NoObviousCause, SolverDiagnosticSeverity.Hint));

        return results;
    }

    private static List<PalInstance> CheckOwnedPals(
        BreedingSolverSettings settings,
        PalSpecifier target,
        Func<Pal, bool> withinSteps,
        IReadOnlyList<Pal> wildPals,
        Action<SolverDiagnostic> add
    )
    {
        // wild pals can stand in for owned pals, so nothing about the owned list is fatal on its own
        var haveUsableWildPals = wildPals.Any(withinSteps);

        if (settings.OwnedPals.Count == 0)
        {
            if (!haveUsableWildPals)
                add(new(SolverDiagnosticCode.NoOwnedPals, SolverDiagnosticSeverity.Blocking));

            return [];
        }

        var withinBreedingSteps = settings.OwnedPals.Where(p => withinSteps(p.Pal)).ToList();
        if (withinBreedingSteps.Count == 0 && !haveUsableWildPals)
        {
            var minSteps = settings.OwnedPals
                .Select(p => settings.BreedingDB.MinBreedingSteps[p.Pal][target.Pal])
                .Min();

            add(new(SolverDiagnosticCode.NoPalsWithinBreedingSteps, SolverDiagnosticSeverity.Blocking)
            {
                Pal = target.Pal,
                Value = minSteps,
            });
            return [];
        }

        var usable = withinBreedingSteps
            .Where(p => p.PassiveSkills.Except(target.DesiredPassives).Count() <= settings.MaxInputIrrelevantPassives)
            .ToList();

        if (withinBreedingSteps.Count > 0 && usable.Count == 0 && !haveUsableWildPals)
            add(new(SolverDiagnosticCode.AllOwnedPalsFilteredByIrrelevantPassives, SolverDiagnosticSeverity.Blocking)
            {
                Value = settings.MaxInputIrrelevantPassives,
            });

        return usable;
    }

    private static void CheckActiveSkills(
        PalDB db,
        GameSettings gameSettings,
        PalSpecifier target,
        IReadOnlySet<Pal> reachableSpecies,
        IReadOnlySet<Pal> obtainableSpecies,
        IReadOnlyList<PalInstance> usableOwned,
        bool wildPalsAllowed,
        Action<SolverDiagnostic> add
    )
    {
        if (target.TargetActiveSkills.Count == 0) return;

        var palsByName = db.Pals.ToLookup(p => p.Name);
        var maxLevel = gameSettings.MaxPalLevel;
        var newPalLevel = gameSettings.NewPalSkillLevel;

        var ownedLevelByPalName = usableOwned
            .GroupBy(p => p.Pal.Name)
            .ToDictionary(g => g.Key, g => g.Max(p => Math.Min(p.Level, maxLevel)));

        foreach (var skill in target.TargetActiveSkills)
        {
            if (!db.BreedingSkills.TryGetValue(skill.Name, out var learnable) || learnable.Count == 0)
            {
                add(new(SolverDiagnosticCode.ActiveSkillNotBreedable, SolverDiagnosticSeverity.Blocking)
                {
                    ActiveSkill = skill,
                });
                continue;
            }

            var withinCap = learnable.Where(ls => ls.Level <= maxLevel).ToList();
            if (withinCap.Count == 0)
            {
                add(new(SolverDiagnosticCode.ActiveSkillAboveLevelCap, SolverDiagnosticSeverity.Blocking)
                {
                    ActiveSkill = skill,
                    Value = learnable.Min(ls => ls.Level),
                    SuggestedPals = PalsOf(palsByName, learnable.OrderBy(ls => ls.Level)),
                });
                continue;
            }

            // a pal you already own counts at the level it's at now; anything bred or caught arrives unleveled
            var withSkill = withinCap
                .Where(ls =>
                    ls.Level <= newPalLevel ||
                    (ownedLevelByPalName.TryGetValue(ls.PalName, out var ownedLevel) && ls.Level <= ownedLevel)
                )
                .ToList();

            if (withSkill.Count == 0)
            {
                add(new(SolverDiagnosticCode.ActiveSkillNeedsLevelling, SolverDiagnosticSeverity.Blocking)
                {
                    ActiveSkill = skill,
                    Value = withinCap.Min(ls => ls.Level),
                    SuggestedPals = PalsOf(palsByName, withinCap.OrderBy(ls => ls.Level)),
                });
                continue;
            }

            var sources = PalsOf(palsByName, withSkill, limit: int.MaxValue);

            if (!sources.Any(reachableSpecies.Contains))
            {
                add(new(SolverDiagnosticCode.ActiveSkillNoAvailableSource, SolverDiagnosticSeverity.Blocking)
                {
                    ActiveSkill = skill,
                    SuggestedPals = sources.Take(MaxSuggestedPals).ToList(),
                });
            }
            else if (!wildPalsAllowed && !sources.Any(obtainableSpecies.Contains))
            {
                add(new(SolverDiagnosticCode.ActiveSkillNeedsWildPals, SolverDiagnosticSeverity.Hint)
                {
                    ActiveSkill = skill,
                    SuggestedPals = sources.Where(reachableSpecies.Contains).Take(MaxSuggestedPals).ToList(),
                });
            }
        }
    }

    private static void CheckRequiredPassives(
        PalDB db,
        BreedingSolverSettings settings,
        PalSpecifier target,
        IReadOnlyList<PalInstance> usableOwned,
        IReadOnlyList<Pal> wildPals,
        Action<SolverDiagnostic> add
    )
    {
        foreach (var passive in target.RequiredPassives)
        {
            if (usableOwned.Any(p => p.PassiveSkills.Contains(passive)))
                continue;

            // a wild catch's random passives are modelled as unnamed placeholders, so only
            // species-guaranteed passives count as a real source
            if (wildPals.Any(p => p.GuaranteedPassiveSkills(db).Contains(passive)))
                continue;

            if (!passive.SupportsSurgery)
            {
                add(new(SolverDiagnosticCode.RequiredPassiveUnavailable, SolverDiagnosticSeverity.Blocking)
                {
                    PassiveSkill = passive,
                });
            }
            else if (!settings.SurgeryPassives.Contains(passive))
            {
                add(new(SolverDiagnosticCode.RequiredPassiveNeedsSurgery, SolverDiagnosticSeverity.Blocking)
                {
                    PassiveSkill = passive,
                });
            }
            else if (passive.SurgeryCost > settings.MaxSurgeryCost)
            {
                add(new(SolverDiagnosticCode.SurgeryCostTooLow, SolverDiagnosticSeverity.Blocking)
                {
                    PassiveSkill = passive,
                    Value = passive.SurgeryCost,
                });
            }
        }
    }

    private static List<Pal> PalsOf(
        ILookup<string, Pal> palsByName,
        IEnumerable<LearnableActiveSkill> learnable,
        int limit = MaxSuggestedPals
    ) =>
        learnable
            .SelectMany(ls => palsByName[ls.PalName])
            .Distinct()
            .Take(limit)
            .ToList();
}
