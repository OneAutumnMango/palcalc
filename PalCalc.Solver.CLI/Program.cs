using PalCalc.Model;
using PalCalc.SaveReader;
using PalCalc.Solver;
using PalCalc.Solver.ResultPruning;
using System.Diagnostics;

internal class Program
{
    // scenario used for active-skill perf benchmarking
    private const string TargetPal = "Penking";
    private static readonly string[] TargetPassives = ["Immortality", "Diamond Body", "Demon God"];
    private static readonly string[] TargetSkills = [
        "Hydro Laser", "Hydro Slicer", "Splash", "Lightning Streak", "Dark Ball", "Dark Laser"
    ];

    static void Main(string[] args)
    {
        Logging.InitCommonFull();

        var repeats = int.TryParse(Environment.GetEnvironmentVariable("PALCALC_BENCH_REPEATS"), out var r) ? r : 3;
        var skillCounts = args.Length > 0
            ? args.Select(int.Parse).ToArray()
            : [0, 2, 4, 6];

        var db = PalDB.LoadEmbedded();
        var breedingDB = PalBreedingDB.LoadEmbedded(db);

        var explicitSave = Environment.GetEnvironmentVariable("PALCALC_SAVE");
        ISaveGame saveGame = explicitSave != null
            ? new StandardSaveGame(explicitSave)
            : DirectSavesLocation.AllLocal
                .SelectMany(l => l.ValidSaveGames)
                .MaxBy(g => g.LevelMeta.ReadGameOptions().PlayerLevel);

        Console.WriteLine("Using save {0}", saveGame);
        var ownedPals = saveGame.Level.ReadCharacterData(db, GameSettings.Defaults, [], null).Pals;
        Console.WriteLine("Loaded {0} owned pals\n", ownedPals.Count);

        foreach (var numSkills in skillCounts)
        {
            var timings = new List<long>();
            for (int i = 0; i < repeats; i++)
                timings.Add(RunScenario(db, breedingDB, ownedPals, numSkills));

            Console.WriteLine(
                "SUMMARY {0} skills -> min {1} ms, median {2} ms  [{3}]",
                numSkills,
                timings.Min(),
                timings.Order().ElementAt(timings.Count / 2),
                string.Join(", ", timings)
            );
        }
    }

    private static long RunScenario(PalDB db, PalBreedingDB breedingDB, List<PalInstance> ownedPals, int numSkills)
    {
        var settings = new BreedingSolverSettings(
            db: db,
            breedingDB: breedingDB,
            gameSettings: new GameSettings(),
            ownedPals: ownedPals,
            resultPruning: ResultPruningPolicy.Default,
            maxBreedingSteps: 10,
            maxSolverIterations: 20,
            maxWildPals: 1,
            allowedWildPals: db.Pals.ToList(),
            bannedBredPals: [],
            maxInputIrrelevantPassives: 3,
            maxBredIrrelevantPassives: 1,
            maxEffort: TimeSpan.MaxValue,
            maxThreads: 0,
            maxSurgeryCost: 0,
            allowedSurgeryPassives: db.SurgeryPassiveSkills.ToList(),
            useGenderReversers: false,
            useSkillFruits: false,
            allowedSkillFruitSkills: []
        );

        var target = new PalSpecifier
        {
            Pal = TargetPal.ToPal(db),
            RequiredPassives = TargetPassives.Select(p => p.ToStandardPassive(db)).ToList(),
            TargetActiveSkills = TargetSkills.Take(numSkills).Select(s => db.ActiveSkills.Single(a => a.Name == s)).ToList(),
        };

        var solver = new BreedingSolver();

        GC.Collect();
        GC.WaitForPendingFinalizers();

        var sw = Stopwatch.StartNew();
        var result = solver.Solve(new BreedingSolverRequest(target, settings), new SolverStateController());
        sw.Stop();

        Console.WriteLine(
            "{0} skills -> {1,6} ms, {2} results, best {3}",
            numSkills,
            sw.ElapsedMilliseconds,
            result.Results.Count,
            result.Results.Count == 0 ? "n/a" : result.Results.Min(r => r.BreedingEffort).ToString()
        );

        return sw.ElapsedMilliseconds;
    }
}
