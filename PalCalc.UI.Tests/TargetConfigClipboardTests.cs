using PalCalc.Model;
using PalCalc.Solver;
using PalCalc.UI.Localization;
using PalCalc.UI.Model;
using PalCalc.UI.View.Main;
using PalCalc.UI.ViewModel;
using PalCalc.UI.ViewModel.Mapped;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PalCalc.UI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class TargetConfigClipboardTests
    {
        private static Dispatcher? uiDispatcher;

        private static Dispatcher UIDispatcher()
        {
            if (uiDispatcher != null) return uiDispatcher;

            using var ready = new ManualResetEventSlim();
            var thread = new Thread(() =>
            {
                new App().InitializeComponent();
                Translator.Init();
                uiDispatcher = Dispatcher.CurrentDispatcher;
                ready.Set();
                Dispatcher.Run();
            })
            { IsBackground = true };

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            ready.Wait();

            return uiDispatcher!;
        }

        private static void RunSta(Action action) => UIDispatcher().Invoke(action);

        private static void Pump()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }

        private static PalSpecifierViewModel MakeSpec(string id, Pal pal, params PassiveSkill[] requiredPassives) =>
            new(id, new PalSpecifier()
            {
                Pal = pal,
                RequiredPassives = requiredPassives.ToList(),
                OptionalPassives = [],
                TargetActiveSkills = [],
                RequiredGender = PalGender.FEMALE,
                IV_HP = 50,
            });

        private static (PalTargetListView, Window) MakeView(PalTargetListViewModel vm)
        {
            var view = new PalTargetListView() { DataContext = vm };
            var window = new Window() { Content = view, Width = 400, Height = 400 };

            window.Show();
            view.UpdateLayout();
            Pump();

            return (view, window);
        }

        private static MenuItem MenuItemFor(PalTargetListView view, PalSpecifierViewModel spec, int index)
        {
            var container = view.ItemContainerGenerator.ContainerFromItem(spec) as ListBoxItem;
            Assert.IsNotNull(container, "no container generated for spec");

            var menu = container.ContextMenu;
            Assert.IsNotNull(menu, "no context menu on container");

            menu.PlacementTarget = container;
            menu.IsOpen = true;
            Pump();

            return menu.Items.OfType<MenuItem>().ElementAt(index);
        }

        [TestMethod]
        public void CopyConfigMenuItem_PutsConfigOnClipboard()
        {
            RunSta(() =>
            {
                var db = PalDB.LoadEmbedded();
                var spec = MakeSpec("spec", db.Pals.First(), db.StandardPassiveSkills.First());

                var (view, window) = MakeView(new PalTargetListViewModel(null!, [spec]));

                Clipboard.Clear();
                MenuItemFor(view, spec, 0).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Pump();

                var clipboard = Clipboard.ContainsText() ? Clipboard.GetText() : null;
                window.Close();

                Assert.IsNotNull(clipboard, "nothing was copied to the clipboard");

                var config = PalTargetConfig.FromJson(clipboard);
                Assert.IsNotNull(config, "copied content was not a valid target config");
                Assert.AreEqual(spec.TargetPal!.ModelObject.InternalName, config.Pal);
                Assert.AreEqual(db.StandardPassiveSkills.First().InternalName, config.RequiredPassives.Single());
                Assert.AreEqual(PalGender.FEMALE, config.RequiredGender);
                Assert.AreEqual(50, config.MinIV_HP);
            });
        }

        [TestMethod]
        public void PasteConfigMenuItem_AppliesClipboardConfig()
        {
            RunSta(() =>
            {
                var db = PalDB.LoadEmbedded();
                var source = MakeSpec("source", db.Pals.First(), db.StandardPassiveSkills.First());
                var dest = MakeSpec("dest", db.Pals.Last());

                var vm = new PalTargetListViewModel(null!, [source, dest]);

                PalTargetConfig? pasted = null;
                vm.ConfigPasted += c => pasted = c;

                var (view, window) = MakeView(vm);

                Clipboard.Clear();
                Clipboard.SetText(vm.ExportConfigJson(source));

                MenuItemFor(view, dest, 1).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Pump();

                window.Close();

                Assert.IsNotNull(pasted, "paste didn't produce a config");
                Assert.AreEqual(source.TargetPal!.ModelObject.InternalName, pasted!.Pal);
                Assert.AreSame(dest, vm.SelectedTarget, "paste targeted the wrong specifier");

                pasted.ApplyTo(dest);
                Assert.AreEqual(source.TargetPal.ModelObject, dest.TargetPal!.ModelObject);
                Assert.AreEqual(db.StandardPassiveSkills.First(), dest.RequiredPassives.AsModelEnumerable().Single());
                Assert.AreEqual(PalGender.FEMALE, dest.RequiredGender.Value);
                Assert.AreEqual(50, dest.MinIv_HP);
            });
        }
    }
}
