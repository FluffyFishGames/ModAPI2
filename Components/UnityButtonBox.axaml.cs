using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ModAPI.ViewModels;

namespace ModAPI.Components
{
    public partial class UnityButtonBox : UserControl
    {
        public string Label { get { return GetValue(LabelProperty); } set { SetValue(LabelProperty, value); } }
        public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<DirectoryInput, string>(nameof(Label));
        public bool IsAssigning { get { return GetValue(IsAssigningProperty); } set { SetValue(IsAssigningProperty, value); } }
        public static readonly StyledProperty<bool> IsAssigningProperty = AvaloniaProperty.Register<DirectoryInput, bool>(nameof(IsAssigning));
        public bool ShowAssigning { get { return GetValue(ShowAssigningProperty); } set { SetValue(ShowAssigningProperty, value); } }
        public static readonly StyledProperty<bool> ShowAssigningProperty = AvaloniaProperty.Register<DirectoryInput, bool>(nameof(ShowAssigning));
        public bool LeftShift { get { return GetValue(LeftShiftProperty); } set { SetValue(LeftShiftProperty, value); } }
        public static readonly StyledProperty<bool> LeftShiftProperty = AvaloniaProperty.Register<DirectoryInput, bool>(nameof(LeftShift));
        public bool LeftControl { get { return GetValue(LeftControlProperty); } set { SetValue(LeftControlProperty, value); } }
        public static readonly StyledProperty<bool> LeftControlProperty = AvaloniaProperty.Register<DirectoryInput, bool>(nameof(LeftControl));
        public bool LeftAlt { get { return GetValue(LeftAltProperty); } set { SetValue(LeftAltProperty, value); } }
        public static readonly StyledProperty<bool> LeftAltProperty = AvaloniaProperty.Register<DirectoryInput, bool>(nameof(LeftAlt));
        public bool RightShift { get { return GetValue(RightShiftProperty); } set { SetValue(RightShiftProperty, value); } }
        public static readonly StyledProperty<bool> RightShiftProperty = AvaloniaProperty.Register<DirectoryInput, bool>(nameof(RightShift));
        public bool RightControl { get { return GetValue(RightControlProperty); } set { SetValue(RightControlProperty, value); } }
        public static readonly StyledProperty<bool> RightControlProperty = AvaloniaProperty.Register<DirectoryInput, bool>(nameof(RightControl));
        public bool RightAlt { get { return GetValue(RightAltProperty); } set { SetValue(RightAltProperty, value); } }
        public static readonly StyledProperty<bool> RightAltProperty = AvaloniaProperty.Register<DirectoryInput, bool>(nameof(RightAlt));

        public ButtonMapping Value { get { return GetValue(ValueProperty); } set { SetValue(ValueProperty, value); } }
        public static readonly StyledProperty<ButtonMapping> ValueProperty = AvaloniaProperty.Register<DirectoryInput, ButtonMapping>(nameof(Value));

        public UnityButtonBox()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            var button = this.FindControl<Avalonia.Controls.Button>("Button");
            button.Click += Click;
            button.LostFocus += LostFocus;
            button.KeyDown += KeyDown;
            button.KeyUp += KeyUp;
        }

        private void KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (IsAssigning)
            {
                if (e.Key == Avalonia.Input.Key.LeftShift)
                {
                    LeftShift = true;
                    ShowAssigning = false;
                }
                else if (e.Key == Avalonia.Input.Key.LeftCtrl)
                { 
                    LeftControl = true;
                    ShowAssigning = false;
                }
                else if (e.Key == Avalonia.Input.Key.LeftAlt)
                { 
                    LeftAlt = true;
                    ShowAssigning = false;
                }
                else if (e.Key == Avalonia.Input.Key.RightShift)
                { 
                    RightShift = true;
                    ShowAssigning = false;
                }
                else if (e.Key == Avalonia.Input.Key.RightCtrl)
                { 
                    RightControl = true;
                    ShowAssigning = false;
                }
                else if (e.Key == Avalonia.Input.Key.RightAlt)
                { 
                    RightAlt = true;
                    ShowAssigning = false;
                }
                else
                {
                    Value.LeftAlt = LeftAlt;
                    Value.LeftShift = LeftShift;
                    Value.LeftControl = LeftControl;
                    Value.RightAlt = RightAlt;
                    Value.RightShift = RightShift;
                    Value.RightControl = RightControl;
                    LeftAlt = false;
                    LeftShift = false;
                    LeftControl = false;
                    RightAlt = false;
                    RightShift = false;
                    RightControl = false;

                    var unityButton = Utils.Button.GetByKey(e.Key);
                    System.Diagnostics.Debug.WriteLine(unityButton.ToString());
                    Value.Button = unityButton;
                    Classes.Remove("Assigning");
                    IsAssigning = false;
                    ShowAssigning = false;
                }
            }
        }

        private void KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (IsAssigning)
            {
                var unityButton = UnityButton.None;
                if (LeftShift && e.Key == Avalonia.Input.Key.LeftShift)
                    unityButton = Utils.Button.GetByKey(Avalonia.Input.Key.LeftShift);
                else if (LeftControl && e.Key == Avalonia.Input.Key.LeftCtrl)
                    unityButton = Utils.Button.GetByKey(Avalonia.Input.Key.LeftCtrl);
                else if (LeftAlt && e.Key == Avalonia.Input.Key.LeftAlt)
                    unityButton = Utils.Button.GetByKey(Avalonia.Input.Key.LeftAlt);
                else if (RightShift && e.Key == Avalonia.Input.Key.RightShift)
                    unityButton = Utils.Button.GetByKey(Avalonia.Input.Key.RightShift);
                else if (RightControl && e.Key == Avalonia.Input.Key.RightCtrl)
                    unityButton = Utils.Button.GetByKey(Avalonia.Input.Key.RightCtrl);
                else if (RightAlt && e.Key == Avalonia.Input.Key.RightAlt)
                    unityButton = Utils.Button.GetByKey(Avalonia.Input.Key.RightAlt);

                if (unityButton != UnityButton.None)
                {
                    LeftAlt = false;
                    LeftShift = false;
                    LeftControl = false;
                    RightAlt = false;
                    RightShift = false;
                    RightControl = false;

                    Value.LeftAlt = false;
                    Value.LeftControl = false;
                    Value.LeftShift = false;
                    Value.RightAlt = false;
                    Value.RightControl = false;
                    Value.RightShift = false;
                    Value.Button = unityButton;

                    Classes.Remove("Assigning");
                    IsAssigning = false;
                    ShowAssigning = false;
                }
            }
        }

        private void LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (IsAssigning)
            {
                LeftAlt = false;
                LeftShift = false;
                LeftControl = false;
                RightAlt = false;
                RightShift = false;
                RightControl = false;
                Classes.Remove("Assigning");
                IsAssigning = false;
                ShowAssigning = false;
            }
        }

        private void Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Classes.Add("Assigning");
            IsAssigning = true;
            ShowAssigning = true;
        }
    }
}
