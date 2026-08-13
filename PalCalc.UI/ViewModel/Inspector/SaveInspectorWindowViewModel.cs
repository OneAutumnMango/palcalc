using PalCalc.Model;
using PalCalc.SaveReader;
using PalCalc.UI.Localization;
using PalCalc.UI.Model;
using PalCalc.UI.ViewModel.Inspector.Search.Grid;
using PalCalc.UI.ViewModel.Mapped;
using PalCalc.UI.ViewModel.Mapped.Saves;
using PalCalc.UI.ViewModel.SaveSelection;
using PalCalc.UI.ViewModel.Solver;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.UI.ViewModel.Inspector
{
    public partial class SaveInspectorWindowViewModel
    {
        private static SaveInspectorWindowViewModel designerInstance = null;
        public static SaveInspectorWindowViewModel DesignerInstance => designerInstance ??= new SaveInspectorWindowViewModel(
            null,
            SaveGameViewModel.DesignerInstance,
            CachedSaveGame.SampleForDesignerView,
            GameSettings.Defaults
        );

        public SaveGameViewModel DisplayedSave { get; }

        public SearchViewModel Search { get; }
        public SaveDetailsViewModel Details { get; }

        public ILocalizedText WindowTitle { get; }

        public SaveInspectorWindowViewModel(
            SavesCollectionViewModel slvm,
            SaveGameViewModel sgvm,
            CachedSaveGame cachedSave,
            GameSettings settings
        )
        {
            DisplayedSave = sgvm;

            Search = new SearchViewModel(sgvm, cachedSave, settings);
            Details = new SaveDetailsViewModel(slvm.SourceLocation, cachedSave);

            WindowTitle = LocalizationCodes.LC_SAVEWINDOW_TITLE.Bind(sgvm.CombinedLabel);
        }

        public SaveCustomizationsViewModel Customizations => DisplayedSave.Customizations;

        [RelayCommand]
        private void ToggleRequiredPal(object slot)
        {
            if (slot is not ContainerGridPalSlotViewModel palSlot) return;

            var target = RequiredPalsViewModel.Active;
            if (target == null) return;

            var instanceId = palSlot.PalInstance?.ModelObject?.InstanceId;
            target.Toggle(instanceId);
            palSlot.IsRequired = target.IsRequired(instanceId);
        }
    }
}
