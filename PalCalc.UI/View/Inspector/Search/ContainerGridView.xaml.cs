using PalCalc.UI.ViewModel.Inspector;
using PalCalc.UI.ViewModel.Inspector.Search;
using PalCalc.UI.ViewModel.Inspector.Search.Grid;
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

namespace PalCalc.UI.View.Inspector.Search
{
    /// <summary>
    /// Interaction logic for ContainerGridView.xaml
    /// </summary>
    public partial class ContainerGridView : UserControl
    {
        public ContainerGridView()
        {
            InitializeComponent();
        }

        private void Slot_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var inspector = Window.GetWindow(this)?.DataContext as SaveInspectorWindowViewModel;
            inspector?.ToggleRequiredPalCommand.Execute((sender as FrameworkElement)?.DataContext);
        }

        private void RemoveRequiredPal_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;
            var slotVm = menuItem.DataContext;
            var inspector = Window.GetWindow(this)?.DataContext as SaveInspectorWindowViewModel;
            inspector?.ToggleRequiredPalCommand.Execute(slotVm);
        }

        private void Slot_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                if ((sender as FrameworkElement)?.DataContext is ContainerGridPalSlotViewModel palSlot && palSlot.IsRequired)
                {
                    var inspector = Window.GetWindow(this)?.DataContext as SaveInspectorWindowViewModel;
                    inspector?.ToggleRequiredPalCommand.Execute(palSlot);
                    e.Handled = true;
                }
            }
        }
    }
}
