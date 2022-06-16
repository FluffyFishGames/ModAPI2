using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ModAPI.ViewModels;

namespace ModAPI.Views
{
    public partial class TaskView : UserControl
    {
        public TaskView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            this.FindControl<Avalonia.Controls.Button>("CloseButton").Click += TaskView_Click;
        }

        private void TaskView_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (this.DataContext is Task task)
            {
                if (task.OnClose != null)
                    task.OnClose();
            }
        }
    }
}
