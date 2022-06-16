using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ModAPI.ViewModels;

namespace ModAPI.Views
{
    public partial class GameModProjectsTabView : UserControl
    {
        public GameModProjectsTabView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            this.FindControl<Avalonia.Controls.Button>("CreateProjectButton").Click += GameModProjectsTabView_Click;
        }

        private void GameModProjectsTabView_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(this.DataContext);
            if (this.DataContext is GameModProjectsTab tab)
            {
                AppViewModel.Instance.CurrentPopup = new CreateModProject(tab.Data);
            }
        }
    }
}
