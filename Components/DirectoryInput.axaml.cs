using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Runtime.InteropServices;
using System;
using ReactiveUI;
using System.Reactive;

namespace ModAPI.Components
{
    public partial class DirectoryInput : UserControl
    {
        public const string DLL = "tinyfiledialogs64.dll";
        [DllImport(DLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tinyfd_selectFolderDialog(string aTitle, string aDefaultPathAndFile);

        public string Label { get { return GetValue(LabelProperty); } set { SetValue(LabelProperty, value); } }
        public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<DirectoryInput, string>(nameof(Label));

        public string Value { get { return GetValue(ValueProperty); } set { SetValue(ValueProperty, value); } }
        public static readonly StyledProperty<string> ValueProperty = AvaloniaProperty.Register<DirectoryInput, string>(nameof(Value));

        public int InputWidth { get { return GetValue(InputWidthProperty); } set { SetValue(InputWidthProperty, value); } }
        public static readonly StyledProperty<int> InputWidthProperty = AvaloniaProperty.Register<DirectoryInput, int>(nameof(InputWidth), 400);

        public DirectoryInput()
        {
            InitializeComponent();
//            OpenFolderDialog = ReactiveCommand.Create(OpenDialog);
        }
        //public ReactiveCommand<Unit, Unit> OpenFolderDialog { get; }

        private static string StringFromANSI(IntPtr ptr) // for UTF-8/char
        {
            return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(ptr);
        }

        private void OpenDialog()
        {
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            this.FindControl<Avalonia.Controls.Button>("Button").Click += DirectoryInput_Click;
        }

        private void DirectoryInput_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var ptr = tinyfd_selectFolderDialog("Please select game path", this.Value);
            var newValue = StringFromANSI(ptr);
            if (newValue != null)
                Value = newValue;
        }
    }
}
