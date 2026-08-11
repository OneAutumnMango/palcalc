using PalCalc.Solver;
using PalCalc.UI.Localization;
using PalCalc.UI.ViewModel.Mapped;
using System.Collections.Generic;
using System.Linq;

namespace PalCalc.UI.ViewModel.Solver
{
    public class SolverDiagnosticViewModel
    {
        public SolverDiagnosticViewModel(SolverDiagnostic diagnostic)
        {
            IsBlocking = diagnostic.Severity == SolverDiagnosticSeverity.Blocking;
            Description = Describe(diagnostic);
        }

        public bool IsBlocking { get; }
        public ILocalizedText Description { get; }

        public static List<SolverDiagnosticViewModel> MakeAll(IEnumerable<SolverDiagnostic> diagnostics) =>
            diagnostics
                .Select(d => new SolverDiagnosticViewModel(d))
                .OrderByDescending(d => d.IsBlocking)
                .ToList();

        private static ILocalizedText Describe(SolverDiagnostic d)
        {
            ILocalizedText PalName() => PalViewModel.Make(d.Pal).Name;
            ILocalizedText SkillName() => ActiveSkillViewModel.Make(d.ActiveSkill).Name;
            ILocalizedText PassiveName() => PassiveSkillViewModel.Make(d.PassiveSkill).Name;
            ILocalizedText PalNames() => Translator.Join.Bind(d.SuggestedPals.Select(p => PalViewModel.Make(p).Name));

            return d.Code switch
            {
                SolverDiagnosticCode.NoOwnedPals =>
                    LocalizationCodes.LC_DIAGNOSTICS_NO_OWNED_PALS.Bind(),

                SolverDiagnosticCode.NoPalsWithinBreedingSteps =>
                    LocalizationCodes.LC_DIAGNOSTICS_NO_PALS_WITHIN_STEPS.Bind(
                        new { PalName = PalName(), MinSteps = d.Value }
                    ),

                SolverDiagnosticCode.AllOwnedPalsFilteredByIrrelevantPassives =>
                    LocalizationCodes.LC_DIAGNOSTICS_IRRELEVANT_PASSIVES.Bind(d.Value),

                SolverDiagnosticCode.TargetPalBanned =>
                    LocalizationCodes.LC_DIAGNOSTICS_TARGET_BANNED.Bind(PalName()),

                SolverDiagnosticCode.ActiveSkillNotBreedable =>
                    LocalizationCodes.LC_DIAGNOSTICS_SKILL_NOT_BREEDABLE.Bind(SkillName()),

                SolverDiagnosticCode.ActiveSkillAboveLevelCap =>
                    LocalizationCodes.LC_DIAGNOSTICS_SKILL_ABOVE_LEVEL_CAP.Bind(
                        new { SkillName = SkillName(), Level = d.Value, PalNames = PalNames() }
                    ),

                SolverDiagnosticCode.ActiveSkillNeedsLevelling =>
                    LocalizationCodes.LC_DIAGNOSTICS_SKILL_NEEDS_LEVELLING.Bind(
                        new { SkillName = SkillName(), Level = d.Value, PalNames = PalNames() }
                    ),

                SolverDiagnosticCode.ActiveSkillNoAvailableSource =>
                    LocalizationCodes.LC_DIAGNOSTICS_SKILL_NO_SOURCE.Bind(
                        new { SkillName = SkillName(), PalNames = PalNames() }
                    ),

                SolverDiagnosticCode.ActiveSkillNeedsWildPals =>
                    LocalizationCodes.LC_DIAGNOSTICS_SKILL_NEEDS_WILD.Bind(
                        new { SkillName = SkillName(), PalNames = PalNames() }
                    ),

                SolverDiagnosticCode.RequiredPassiveUnavailable =>
                    LocalizationCodes.LC_DIAGNOSTICS_PASSIVE_UNAVAILABLE.Bind(PassiveName()),

                SolverDiagnosticCode.RequiredPassiveNeedsSurgery =>
                    LocalizationCodes.LC_DIAGNOSTICS_PASSIVE_NEEDS_SURGERY.Bind(PassiveName()),

                SolverDiagnosticCode.SurgeryCostTooLow =>
                    LocalizationCodes.LC_DIAGNOSTICS_SURGERY_COST_TOO_LOW.Bind(
                        new { PassiveName = PassiveName(), Cost = d.Value }
                    ),

                _ => LocalizationCodes.LC_DIAGNOSTICS_NO_OBVIOUS_CAUSE.Bind(),
            };
        }
    }
}
