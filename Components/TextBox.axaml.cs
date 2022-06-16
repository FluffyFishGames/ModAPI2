using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModAPI.Components
{
    public partial class TextBox : UserControl
    {
        public string Label { get { return GetValue(LabelProperty); } set { SetValue(LabelProperty, value); } }
        public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<DirectoryInput, string>(nameof(Label));

        public string Value { get { return GetValue(ValueProperty); } set { SetValue(ValueProperty, value); } }
        public static readonly StyledProperty<string> ValueProperty = AvaloniaProperty.Register<DirectoryInput, string>(nameof(Value));

        public int InputWidth { get { return GetValue(InputWidthProperty); } set { SetValue(InputWidthProperty, value); } }
        public static readonly StyledProperty<int> InputWidthProperty = AvaloniaProperty.Register<DirectoryInput, int>(nameof(InputWidth), 0);

        public TextBox()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
