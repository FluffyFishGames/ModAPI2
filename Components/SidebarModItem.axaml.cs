using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.Svg;
using ModAPI.ViewModels;
using System;

namespace ModAPI.Components
{
    public partial class SidebarModItem : UserControl
    {
        public bool IsHeader { get { return GetValue(IsHeaderProperty); } set { SetValue(IsHeaderProperty, value); } }
        public static readonly StyledProperty<bool> IsHeaderProperty = AvaloniaProperty.Register<SidebarModItem, bool>(nameof(IsHeader));
        public bool IsActive { get { return GetValue(IsActiveProperty); } set { SetValue(IsActiveProperty, value); } }
        public static readonly StyledProperty<bool> IsActiveProperty = AvaloniaProperty.Register<SidebarModItem, bool>(nameof(IsActive));

        private ViewModelBase CurrentContext;

        public SidebarModItem()
        {
            InitializeComponent();
        }

        protected override void OnPointerEnter(PointerEventArgs e)
        {
            base.OnPointerEnter(e);
            if (!IsHeader)
            {
                this.Classes.Remove("normal");
                this.Classes.Add("hover");
            }
        }
        protected override void OnPointerLeave(PointerEventArgs e)
        {
            base.OnPointerLeave(e);
            if (!IsHeader)
            {
                this.Classes.Remove("hover");
                this.Classes.Add("normal");
            }
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);
            if (this.IsInitialized)
            {
                if (change.Property.Name == "IsActive")
                {
                    if (IsActive && !this.Classes.Contains("active"))
                        this.Classes.Add("active");
                    else if (!IsActive && this.Classes.Contains("active"))
                        this.Classes.Remove("active");
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            var button = this.FindControl<Avalonia.Controls.Button>("Button");
            button.Click += Button_Click;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if (IsActive && !this.Classes.Contains("active"))
                this.Classes.Add("active");
            else if (!IsActive && this.Classes.Contains("active"))
                this.Classes.Remove("active");
        }
        private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (this.DataContext is ViewModelBase viewModelBase)
            {
                viewModelBase.IsActive = true;
            }
        }
    }
}
