using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PalCalc.Model;
using PalCalc.UI.Localization;
using PalCalc.UI.ViewModel.Mapped;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PalCalc.UI.ViewModel
{
    public partial class SkillFruitsCheckListEntryViewModel : ObservableObject
    {
        private readonly bool initialEnabled;

        public SkillFruitsCheckListEntryViewModel(SkillFruit fruit, bool initialEnabled)
        {
            Fruit = fruit;
            Skill = ActiveSkillViewModel.Make(fruit.Skill);
            this.initialEnabled = initialEnabled;

            isEnabled = initialEnabled;
        }

        public SkillFruit Fruit { get; }
        public ActiveSkillViewModel Skill { get; }
        public string FruitName => Fruit.FruitName;

        [NotifyPropertyChangedFor(nameof(HasChanges))]
        [ObservableProperty]
        private bool isEnabled;

        public bool HasChanges => IsEnabled != initialEnabled;
    }

    public partial class SkillFruitsCheckListViewModel : ObservableObject
    {
        public IRelayCommand<object> SaveCommand { get; }
        public IRelayCommand<object> CancelCommand { get; }

        public static SkillFruitsCheckListViewModel DesignerInstance { get; } =
            new SkillFruitsCheckListViewModel(
                null, null,
                PalDB.LoadEmbedded().SkillFruits.ToDictionary(f => f, f => true)
            );

        private readonly List<SkillFruitsCheckListEntryViewModel> allEntries;
        public IReadOnlyCollection<SkillFruitsCheckListEntryViewModel> AllEntries => allEntries;

        public SkillFruitsCheckListViewModel(Action onCancel, Action<Dictionary<SkillFruit, bool>> onSave, Dictionary<SkillFruit, bool> initialState)
        {
            allEntries = initialState
                .Select(kvp => new SkillFruitsCheckListEntryViewModel(kvp.Key, kvp.Value))
                .OrderBy(vm => vm.Skill.Name.Value)
                .ToList();

            foreach (var e in allEntries)
                e.PropertyChanged += EntryPropertyChanged;

            SaveCommand = new RelayCommand<object>(
                execute: (window) =>
                {
                    DetachEvents();
                    onSave?.Invoke(allEntries.ToDictionary(e => e.Fruit, e => e.IsEnabled));
                    (window as Window).Close();
                },
                canExecute: (_) => HasChanges
            );

            CancelCommand = new RelayCommand<object>(
                execute: (window) =>
                {
                    DetachEvents();
                    onCancel?.Invoke();
                    (window as Window).Close();
                },
                canExecute: (_) => true
            );
        }

        private void EntryPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SkillFruitsCheckListEntryViewModel.HasChanges))
                HasChanges = allEntries.Any(e => e.HasChanges);

            if (e.PropertyName == nameof(SkillFruitsCheckListEntryViewModel.IsEnabled))
                OnPropertyChanged(nameof(AllItemsEnabled));
        }

        private void DetachEvents()
        {
            foreach (var e in allEntries) e.PropertyChanged -= EntryPropertyChanged;
        }

        [ObservableProperty]
        private ILocalizedText title = new HardCodedText("Skill Fruits Checklist");

        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        [ObservableProperty]
        private bool hasChanges = false;

        public bool? AllItemsEnabled
        {
            get
            {
                if (allEntries.All(e => e.IsEnabled)) return true;
                if (allEntries.All(e => !e.IsEnabled)) return false;
                return null;
            }
            set
            {
                if (value == null) return;
                foreach (var e in allEntries) e.IsEnabled = value.Value;
                OnPropertyChanged();
            }
        }
    }
}
