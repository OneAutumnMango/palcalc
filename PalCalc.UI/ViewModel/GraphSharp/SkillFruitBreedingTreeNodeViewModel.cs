using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PalCalc.Model;
using PalCalc.Solver.PalReference;
using PalCalc.Solver.Tree;
using PalCalc.UI.ViewModel.Mapped;
using System.Collections.Generic;
using System.Linq;

namespace PalCalc.UI.ViewModel.GraphSharp
{
    public class SkillFruitOperationViewModel
    {
        public SkillFruitOperationViewModel(ActiveSkill skill, string fruitName)
        {
            Skill = ActiveSkillViewModel.Make(skill);
            FruitName = fruitName;
        }

        public ActiveSkillViewModel Skill { get; }
        public string FruitName { get; }
    }

    // (note: this node just represents the act of feeding the fruits, the result pal is a separate (parent) node)
    public partial class SkillFruitBreedingTreeNodeViewModel : ObservableObject, IBreedingTreeNodeViewModel
    {
        public SkillFruitBreedingTreeNodeViewModel(SkillFruitOperationNode node)
        {
            var pref = node.PalRef as SkillFruitPalReference;
            var db = PalDB.LoadEmbedded();

            Value = node;
            Operations = pref.TaughtSkills
                .Select(s => new SkillFruitOperationViewModel(
                    s,
                    db.SkillFruits.FirstOrDefault(f => f.SkillName == s.Name)?.FruitName ?? s.Name
                ))
                .ToList();

            ToggleCheckedCommand = new RelayCommand(() => IsChecked = !IsChecked);
        }

        [NotifyPropertyChangedFor(nameof(IsComplete))]
        [ObservableProperty]
        private bool isChecked;

        public IRelayCommand ToggleCheckedCommand { get; }

        public bool IsCheckable => false;

        public bool IsRequiredPal => false;

        private IBreedingTreeNodeViewModel consumer;

        public bool IsComplete => IsChecked || (consumer?.IsComplete ?? false);

        public void SetConsumer(IBreedingTreeNodeViewModel node)
        {
            consumer = node;
            if (consumer is ObservableObject obs)
            {
                obs.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(IsComplete))
                        OnPropertyChanged(nameof(IsComplete));
                };
            }
        }

        public IBreedingTreeNode Value { get; }

        public List<SkillFruitOperationViewModel> Operations { get; }
    }
}
