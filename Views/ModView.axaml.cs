using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModAPI.Views
{
    public partial class ModView : UserControl
    {
        public ModView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
