using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PalCalc.UI.Model;
using PalCalc.UI.View.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PalCalc.UI.ViewModel.Presets
{
    public partial class ActiveSkillsPresetCollectionViewModel : ObservableObject
    {
        // for outer element to respond and close popup (if needed)
        public event Action<ActiveSkillsPresetViewModel> PresetSelected;

        // contains the currently selected active skills from the outer element which would be used as new contents when saving/overwriting
        public PalTargetViewModel ActivePalTarget { get; set; }

        public IRelayCommand<EditableListMenu.CreateCommandArgs> CreatePresetCommand { get; }
        public IRelayCommand<EditableListMenu.SelectCommandArgs> SelectPresetCommand { get; }
        public IRelayCommand<EditableListMenu.DeleteCommandArgs> DeletePresetCommand { get; }
        public IRelayCommand<EditableListMenu.RenameCommandArgs> RenamePresetCommand { get; }
        public IRelayCommand<EditableListMenu.OverwriteCommandArgs> OverwritePresetCommand { get; }

        private ObservableCollection<ActiveSkillsPresetViewModel> options;
        public ReadOnlyObservableCollection<ActiveSkillsPresetViewModel> Options { get; }

        public static ActiveSkillsPresetCollectionViewModel DesignerInstance => new ActiveSkillsPresetCollectionViewModel([
            new ActiveSkillsPreset() { Name = "Test 1" },
            new ActiveSkillsPreset() { Name = "foo" },
        ]);

        public ActiveSkillsPresetCollectionViewModel(IEnumerable<ActiveSkillsPreset> initialOptions)
        {
            options = new ObservableCollection<ActiveSkillsPresetViewModel>(
                initialOptions.Select(o => new ActiveSkillsPresetViewModel(o)).OrderBy(o => o.Label)
            );

            Options = new ReadOnlyObservableCollection<ActiveSkillsPresetViewModel>(options);

            CreatePresetCommand = new RelayCommand<EditableListMenu.CreateCommandArgs>(
                (ev) =>
                {
                    var newPreset = ActivePalTarget?.CurrentPalSpecifier?.ToActiveSkillsPreset() ?? new ActiveSkillsPreset();
                    newPreset.Name = ev.NewName;

                    // insert alphabetically
                    var previous = Options.FirstOrDefault(o => o.Label.Value.CompareTo(ev.NewName) > 0);
                    options.Insert(
                        previous != null ? options.IndexOf(previous) : options.Count,
                        new ActiveSkillsPresetViewModel(newPreset)
                    );

                    AppSettings.Current.ActiveSkillsPresets.Add(newPreset);
                    Storage.SaveAppSettings(AppSettings.Current);
                }
            );

            SelectPresetCommand = new RelayCommand<EditableListMenu.SelectCommandArgs>(
                (ev) => PresetSelected?.Invoke(ev.Item as ActiveSkillsPresetViewModel)
            );

            DeletePresetCommand = new RelayCommand<EditableListMenu.DeleteCommandArgs>(
                (ev) =>
                {
                    var presetVm = ev.Item as ActiveSkillsPresetViewModel;

                    options.Remove(presetVm);
                    AppSettings.Current.ActiveSkillsPresets.Remove(presetVm.ModelObject);
                    Storage.SaveAppSettings(AppSettings.Current);
                }
            );

            RenamePresetCommand = new RelayCommand<EditableListMenu.RenameCommandArgs>(
                (ev) =>
                {
                    var presetVm = ev.Item as ActiveSkillsPresetViewModel;
                    var newName = ev.NewName;

                    // note: reference to original preset from AppSettings object is preserved, just need to update the VM
                    var preset = presetVm.ModelObject;
                    preset.Name = newName;

                    options.Remove(presetVm);
                    // insert alphabetically
                    var previous = Options.FirstOrDefault(o => o.Label.Value.CompareTo(newName) > 0);
                    options.Insert(
                        previous != null ? options.IndexOf(previous) : options.Count,
                        new ActiveSkillsPresetViewModel(preset)
                    );

                    Storage.SaveAppSettings(AppSettings.Current);
                }
            );

            OverwritePresetCommand = new RelayCommand<EditableListMenu.OverwriteCommandArgs>(
                (ev) =>
                {
                    var presetVm = ev.Item as ActiveSkillsPresetViewModel;

                    var oldPreset = presetVm.ModelObject;
                    var newPreset = ActivePalTarget?.CurrentPalSpecifier?.ToActiveSkillsPreset() ?? new ActiveSkillsPreset();
                    newPreset.Name = oldPreset.Name;

                    AppSettings.Current.ActiveSkillsPresets.Remove(oldPreset);
                    AppSettings.Current.ActiveSkillsPresets.Add(newPreset);

                    var idx = options.IndexOf(presetVm);
                    options.Remove(presetVm);
                    options.Insert(idx, new ActiveSkillsPresetViewModel(newPreset));

                    Storage.SaveAppSettings(AppSettings.Current);
                }
            );
        }
    }
}
