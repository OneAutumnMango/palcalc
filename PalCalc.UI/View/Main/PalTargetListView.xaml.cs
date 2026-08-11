using PalCalc.UI.Localization;
using PalCalc.UI.ViewModel;
using PalCalc.UI.ViewModel.Mapped;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AdonisMessageBox = AdonisUI.Controls.MessageBox;

namespace PalCalc.UI.View.Main
{
    /// <summary>
    /// Interaction logic for PalTargetView.xaml
    /// </summary>
    public partial class PalTargetListView : ListBox
    {
        public PalTargetListView()
        {
            InitializeComponent();

            SetResourceReference(StyleProperty, typeof(ListBox));
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            ScrollIntoView(SelectedItem);
        }

        private void ListBoxItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item) item.IsSelected = true;
        }

        private static PalSpecifierViewModel SpecFor(object sender) =>
            ((sender as MenuItem)?.Parent as ContextMenu)?.PlacementTarget is FrameworkElement target
                ? target.DataContext as PalSpecifierViewModel
                : null;

        private void CopyConfig_Click(object sender, RoutedEventArgs e)
        {
            var json = (DataContext as PalTargetListViewModel)?.ExportConfigJson(SpecFor(sender));
            if (json != null) Clipboard.SetText(json);
        }

        private void PasteConfig_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as PalTargetListViewModel;
            var spec = SpecFor(sender);
            if (vm == null || spec == null) return;

            if (!vm.ImportConfigJson(spec, Clipboard.ContainsText() ? Clipboard.GetText() : null))
            {
                AdonisMessageBox.Show(
                    App.Current.MainWindow,
                    LocalizationCodes.LC_TARGET_PASTE_CONFIG_ERROR_MSG.Bind().Value,
                    LocalizationCodes.LC_TARGET_PASTE_CONFIG_ERROR_TITLE.Bind().Value
                );
            }
        }
    }
}
