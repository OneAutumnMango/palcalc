using CommunityToolkit.Mvvm.ComponentModel;
using PalCalc.Model;
using PalCalc.UI.Localization;
using PalCalc.UI.Model;
using PalCalc.UI.View.Utils;
using PalCalc.UI.ViewModel.Mapped;
using System.Linq;

namespace PalCalc.UI.ViewModel.Presets
{
    public partial class ActiveSkillsPresetViewModel : ObservableObject, IEditableListItem
    {
        public ActiveSkillsPresetViewModel(ActiveSkillsPreset content)
        {
            ModelObject = content;
            Label = new HardCodedText(content.Name);

            var db = PalDB.LoadEmbedded();
            ActiveSkillViewModel Resolve(string internalName) =>
                ActiveSkillViewModel.Make(
                    internalName == null ? null : db.ActiveSkills.FirstOrDefault(s => s.InternalName == internalName)
                );

            TargetActiveSkills = new PalSpecifierActiveSkillCollectionViewModel()
            {
                ActiveSkill1 = Resolve(content.ActiveSkill1InternalName),
                ActiveSkill2 = Resolve(content.ActiveSkill2InternalName),
                ActiveSkill3 = Resolve(content.ActiveSkill3InternalName),
                ActiveSkill4 = Resolve(content.ActiveSkill4InternalName),
                ActiveSkill5 = Resolve(content.ActiveSkill5InternalName),
                ActiveSkill6 = Resolve(content.ActiveSkill6InternalName),
            };
        }

        public ActiveSkillsPreset ModelObject { get; }

        public PalSpecifierActiveSkillCollectionViewModel TargetActiveSkills { get; }

        [ObservableProperty]
        private ILocalizedText label;

        public string Name => Label.Value;
        public string DeleteConfirmTitle => LocalizationCodes.LC_TRAITS_PRESETS_DELETE_TITLE.Bind().Value;
        public string DeleteConfirmMessage => LocalizationCodes.LC_TRAITS_PRESETS_DELETE_MSG.Bind(Label).Value;
        public string OverwriteConfirmTitle => LocalizationCodes.LC_TRAITS_PRESETS_OVERWRITE_TITLE.Bind().Value;
        public string OverwriteConfirmMessage => LocalizationCodes.LC_ACTIVE_SKILLS_PRESETS_OVERWRITE_MSG.Bind(Label).Value;
        public string RenamePopupTitle => LocalizationCodes.LC_TRAITS_PRESETS_RENAME_TITLE.Bind(Label).Value;
        public string RenamePopupInputLabel => LocalizationCodes.LC_TRAITS_PRESETS_NAME.Bind().Value;

        public void ApplyTo(PalSpecifierViewModel spec) => spec.TargetActiveSkills.CopyFrom(TargetActiveSkills);
    }
}
