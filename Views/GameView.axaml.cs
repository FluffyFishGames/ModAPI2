using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Linq;

namespace ModAPI.Views
{
    public partial class GameView : UserControl
    {
        public GameView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            this.FindControl<Avalonia.Controls.Button>("PatchGameButton").Click += GameView_Click;
        }

        private void GameView_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (this.DataContext is ViewModels.Game game)
            {
                Utils.ModApplier.Execute(new Utils.ModApplier.Context() { Game = game, Mods = game.Mods.ToList(), ProgressHandler = new ViewModels.ProgressHandler() });
            }
        }
    }
}
