using CommunityToolkit.Mvvm.ComponentModel;
using PalCalc.Model;
using PalCalc.Solver;
using PalCalc.UI.ViewModel.Mapped;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PalCalc.UI.ViewModel.Solver
{
    // Owned pals which must appear somewhere in the breeding tree. Belongs to a single target profile.
    public partial class RequiredPalsViewModel : ObservableObject
    {
        private List<PalInstance> available = [];

        public RequiredPalsViewModel(IEnumerable<string> instanceIds)
        {
            InstanceIds = new ObservableCollection<string>(instanceIds ?? []);
            InstanceIds.CollectionChanged += (_, _) =>
            {
                if (ReferenceEquals(this, active)) RefreshActiveIds();
                Refresh();
            };
        }

        public ObservableCollection<string> InstanceIds { get; }

        public ObservableCollection<PalInstanceViewModel> Pals { get; } = [];

        public bool IsEmpty => Pals.Count == 0;

        public void RefreshWith(IEnumerable<PalInstance> availablePals)
        {
            available = availablePals?.ToList() ?? [];
            Refresh();
        }

        private void Refresh()
        {
            Pals.Clear();
            foreach (var id in InstanceIds)
            {
                var instance = available.FirstOrDefault(p => p.InstanceId == id);
                if (instance != null)
                    Pals.Add(new PalInstanceViewModel(instance));
            }

            OnPropertyChanged(nameof(IsEmpty));
        }

        public bool IsRequired(string instanceId) =>
            instanceId != null && InstanceIds.Contains(instanceId);

        public void Toggle(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return;

            if (!InstanceIds.Remove(instanceId))
            {
                if (InstanceIds.Count >= RequiredPalSet.MaxRequiredPals) return;
                InstanceIds.Add(instanceId);
            }
        }

        // The profile currently being edited. Membership is read from hot paths (every breeding-tree
        // node, every inspector grid slot), so it's kept as a plain snapshot set.
        private static volatile HashSet<string> activeIds = [];
        private static RequiredPalsViewModel active;

        public static RequiredPalsViewModel Active
        {
            get => active;
            set
            {
                active = value;
                RefreshActiveIds();
            }
        }

        private static void RefreshActiveIds() => activeIds = active?.InstanceIds.ToHashSet() ?? [];

        public static bool IsPalRequired(string instanceId) =>
            instanceId != null && activeIds.Contains(instanceId);
    }
}
