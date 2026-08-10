using CommunityToolkit.Mvvm.ComponentModel;
using PalCalc.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PalCalc.UI.ViewModel.Mapped
{
    /// <summary>
    /// ViewModel for managing up to 6 target active skills for breeding.
    /// </summary>
    public partial class PalSpecifierActiveSkillCollectionViewModel : ObservableObject
    {
        public PalSpecifierActiveSkillCollectionViewModel()
        {
        }

        public PalSpecifierActiveSkillCollectionViewModel(IEnumerable<ActiveSkill> modelActiveSkills)
        {
            ActiveSkill1 = ActiveSkillViewModel.Make(modelActiveSkills.Skip(0).FirstOrDefault());
            ActiveSkill2 = ActiveSkillViewModel.Make(modelActiveSkills.Skip(1).FirstOrDefault());
            ActiveSkill3 = ActiveSkillViewModel.Make(modelActiveSkills.Skip(2).FirstOrDefault());
            ActiveSkill4 = ActiveSkillViewModel.Make(modelActiveSkills.Skip(3).FirstOrDefault());
            ActiveSkill5 = ActiveSkillViewModel.Make(modelActiveSkills.Skip(4).FirstOrDefault());
            ActiveSkill6 = ActiveSkillViewModel.Make(modelActiveSkills.Skip(5).FirstOrDefault());
        }

        [NotifyPropertyChangedFor(nameof(HasItems))]
        [ObservableProperty]
        private ActiveSkillViewModel activeSkill1;

        [NotifyPropertyChangedFor(nameof(HasItems))]
        [ObservableProperty]
        private ActiveSkillViewModel activeSkill2;

        [NotifyPropertyChangedFor(nameof(HasItems))]
        [ObservableProperty]
        private ActiveSkillViewModel activeSkill3;

        [NotifyPropertyChangedFor(nameof(HasItems))]
        [ObservableProperty]
        private ActiveSkillViewModel activeSkill4;

        [NotifyPropertyChangedFor(nameof(HasItems))]
        [ObservableProperty]
        private ActiveSkillViewModel activeSkill5;

        [NotifyPropertyChangedFor(nameof(HasItems))]
        [ObservableProperty]
        private ActiveSkillViewModel activeSkill6;

        public bool HasItems => AsEnumerable().Any();

        public IEnumerable<ActiveSkillViewModel> AsEnumerable() => 
            new List<ActiveSkillViewModel>() { ActiveSkill1, ActiveSkill2, ActiveSkill3, ActiveSkill4, ActiveSkill5, ActiveSkill6 }
            .Where(a => a != null);

        public IEnumerable<ActiveSkill> AsModelEnumerable() => AsEnumerable().Select(a => a.ModelObject).Distinct();

        public void CopyFrom(PalSpecifierActiveSkillCollectionViewModel other)
        {
            ActiveSkill1 = other.ActiveSkill1;
            ActiveSkill2 = other.ActiveSkill2;
            ActiveSkill3 = other.ActiveSkill3;
            ActiveSkill4 = other.ActiveSkill4;
            ActiveSkill5 = other.ActiveSkill5;
            ActiveSkill6 = other.ActiveSkill6;
        }
    }
}
