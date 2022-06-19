using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ModAPI.Utils;
using ModAPI.ViewModels;
using ModAPI.ViewModels.ModProject;

namespace ModAPI.Views.ModProject
{
    public partial class ModProjectView : UserControl
    {
        private ViewModelBase LastViewModel;
        //private bool _GameIsSetup;
        public ModProjectView()
        {
            InitializeComponent();
        }
        /*
        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (this.DataContext is ViewModelBase b)
            {
                if (LastViewModel != null)
                    LastViewModel.PropertyChanged -= ViewModelPropertyChanged;
                if (b != null)
                    b.PropertyChanged += ViewModelPropertyChanged;
                LastViewModel = b;
            }
        }

        private void ViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (this.DataContext is ModAPI.ViewModels.ModProject.ModProject project)
            {

            }
        }*/

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            this.FindControl<Avalonia.Controls.Button>("AddButtonButton").Click += AddButton;
            this.FindControl<Avalonia.Controls.Button>("BuildModButton").Click += BuildMod;

        }

        private void BuildMod(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (this.DataContext is ViewModels.ModProject.ModProject modProject)
            {
                var creator = new ModCreator(modProject, new ProgressHandler());
                creator.Execute();
            }
        }

        private void AddButton(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (this.DataContext is ViewModels.ModProject.ModProject modProject)
            {
                var n = 0;
                var id = "";
                while (true)
                {
                    id = "Button" + n;
                    bool isNew = true;
                    foreach (var b in modProject.Configuration.Buttons)
                    {
                        if (b.ID == id)
                            isNew = false;
                    }
                    if (isNew)
                        break;
                    n++;
                }
                modProject.Configuration.Buttons.Add(new ModButton(modProject.Configuration) { ID = id, Name = "", Description = "", StandardMapping = new ButtonMapping() });
            }
        }
    }
}
