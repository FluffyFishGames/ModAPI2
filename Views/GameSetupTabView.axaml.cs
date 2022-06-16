using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModAPI.Views
{
    public partial class GameSetupTabView : UserControl
    {
        public GameSetupTabView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
