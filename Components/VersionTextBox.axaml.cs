using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ModAPI.ViewModels;

namespace ModAPI.Components
{
    public partial class VersionTextBox : UserControl
    {
        public string Label { get { return GetValue(LabelProperty); } set { SetValue(LabelProperty, value); } }
        public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<DirectoryInput, string>(nameof(Label));

        public ModVersion Value { get { return GetValue(ValueProperty); } set { SetValue(ValueProperty, value); } }
        public static readonly StyledProperty<ModVersion> ValueProperty = AvaloniaProperty.Register<DirectoryInput, ModVersion>(nameof(Value));

        public VersionTextBox()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
