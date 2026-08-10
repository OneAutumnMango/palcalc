using AdonisUI.Controls;
using PalCalc.UI.ViewModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace PalCalc.UI.View
{
    /// <summary>
    /// Interaction logic for SkillFruitsCheckListWindow.xaml
    /// </summary>
    public partial class SkillFruitsCheckListWindow : AdonisWindow
    {
        public SkillFruitsCheckListWindow()
        {
            InitializeComponent();
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var senderElem = sender as FrameworkElement;
            var senderVm = senderElem.DataContext as SkillFruitsCheckListEntryViewModel;

            senderVm.IsEnabled = !senderVm.IsEnabled;
        }

        private void m_ListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                var items = m_ListView.SelectedItems.OfType<SkillFruitsCheckListEntryViewModel>();
                var shouldEnable = items.Any(i => !i.IsEnabled);

                foreach (var item in items)
                    item.IsEnabled = shouldEnable;

                e.Handled = true;
            }
        }
    }
}
