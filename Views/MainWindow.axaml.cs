using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ModAPI.ViewModels;

namespace ModAPI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            var dragArea = this.FindControl<Grid>("DragArea");
            dragArea.PointerPressed += DragArea_PointerPressed;
            var closeButton = this.FindControl<Button>("CloseButton");
            var minimizeButton = this.FindControl<Button>("MinimizeButton");
            var maximizeButton = this.FindControl<Button>("MaximizeButton");
            var restoreButton = this.FindControl<Button>("RestoreButton");

            restoreButton.PointerEnter += Button_PointerEnter;
            restoreButton.PointerLeave += Button_PointerLeave;
            closeButton.PointerEnter += Button_PointerEnter;
            closeButton.PointerLeave += Button_PointerLeave;
            minimizeButton.PointerEnter += Button_PointerEnter;
            minimizeButton.PointerLeave += Button_PointerLeave;
            maximizeButton.PointerEnter += Button_PointerEnter;
            maximizeButton.PointerLeave += Button_PointerLeave;

            restoreButton.IsVisible = false;

            closeButton.Click += CloseButton_Click;
            maximizeButton.Click += MaximizeButton_Click;
            minimizeButton.Click += MinimizeButton_Click;
            restoreButton.Click += RestoreButton_Click;
        }

        private void RestoreButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
            var maximizeButton = this.FindControl<Button>("MaximizeButton");
            var restoreButton = this.FindControl<Button>("RestoreButton");

            maximizeButton.IsVisible = true;
            restoreButton.IsVisible = false;
        }

        private void MinimizeButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.WindowState = WindowState.Maximized;
            var maximizeButton = this.FindControl<Button>("MaximizeButton");
            var restoreButton = this.FindControl<Button>("RestoreButton");

            maximizeButton.IsVisible = false;
            restoreButton.IsVisible = true;
        }

        private void Button_PointerEnter(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (sender is Avalonia.StyledElement element)
            {
                element.Classes.Remove("normal");
                element.Classes.Add("hover");
            }
        }

        private void Button_PointerLeave(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (sender is Avalonia.StyledElement element)
            {
                element.Classes.Remove("hover");
                element.Classes.Add("normal");
            }
        }

        private void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.Close();
        }

        private void DragArea_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            BeginMoveDrag(e);
        }
    }
}
