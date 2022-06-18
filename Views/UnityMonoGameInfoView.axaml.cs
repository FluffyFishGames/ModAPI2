using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModAPI.Views
{
    public partial class UnityMonoGameInfoView : UserControl
    {
        public UnityMonoGameInfoView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
