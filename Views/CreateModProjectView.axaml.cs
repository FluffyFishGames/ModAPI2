using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ModAPI.ViewModels;

namespace ModAPI.Views
{
    public partial class CreateModProjectView : UserControl
    {
        public CreateModProjectView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            this.FindControl<Avalonia.Controls.Button>("CreateButton").Click += Create;
            this.FindControl<Avalonia.Controls.Button>("CancelButton").Click += Cancel;
        }

        private void Cancel(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (this.DataContext is CreateModProject d)
            {
                if (d.OnClose != null)
                    d.OnClose();
            }
        }

        private void Create(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (this.DataContext is CreateModProject d)
            {
                d.Game.ModProjects.Add(ViewModels.ModProject.ModProject.CreateProject(d.Game, d.Name));
                if (d.OnClose != null)
                    d.OnClose();
            }
        }
    }
}
