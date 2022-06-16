using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModAPI.Views
{
    public partial class GameInfoView : UserControl
    {
        public GameInfoView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
