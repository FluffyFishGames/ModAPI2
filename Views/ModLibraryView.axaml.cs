using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ModAPI.ViewModels;

namespace ModAPI.Views
{
    public partial class ModLibraryView : UserControl
    {
        public ModLibraryView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            this.FindControl<Button>("CreateModLibrary").Click += ModLibraryView_CreateClick;
            this.FindControl<Button>("RecreateModLibrary").Click += ModLibraryView_CreateClick;
            this.FindControl<Button>("DeleteModLibrary").Click += ModLibraryView_DeleteClick;
        }

        private void ModLibraryView_CreateClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (this.DataContext is ModLibrary viewModel)
            {
                var progressHandler = new ProgressHandler();
                var taskViewModel = new Task(progressHandler);
                taskViewModel.Name = "Creating mod library...";
                viewModel.Create(progressHandler);
                AppViewModel.Instance.CurrentPopup = taskViewModel;
            }
        }
        private void ModLibraryView_DeleteClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
        }
    }
}
