using PalCalc.Model;
using PalCalc.UI.Localization;
using PalCalc.UI.ViewModel.Mapped;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PalCalc.UI.ViewModel.PalDerived
{
    public class InheritedActiveSkillViewModel
    {
        private InheritedActiveSkillViewModel(ActiveSkillViewModel skill, int? level)
        {
            Skill = skill;
            Level = level;
            LevelDescription = level == null ? null : LocalizationCodes.LC_GRAPH_ACTIVE_SKILL_LEVEL.Bind(level.Value);
        }

        public ActiveSkillViewModel Skill { get; }
        public int? Level { get; }
        public ILocalizedText LevelDescription { get; }
        public Visibility LevelVisibility => Level == null ? Visibility.Collapsed : Visibility.Visible;

        // Level is only known when this pal learns the skill naturally; otherwise it was inherited from a parent.
        public static List<InheritedActiveSkillViewModel> MakeAll(Pal pal, IEnumerable<ActiveSkill> skills)
        {
            var db = PalDB.LoadEmbedded();

            return skills
                .Select(s =>
                {
                    var level = db.BreedingSkills
                        .GetValueOrDefault(s.Name)
                        ?.Where(ls => ls.PalName == pal.Name)
                        .Select(ls => (int?)ls.Level)
                        .Min();

                    return new InheritedActiveSkillViewModel(ActiveSkillViewModel.Make(s), level);
                })
                .OrderBy(vm => vm.Skill.Name.Value)
                .ToList();
        }
    }
}
