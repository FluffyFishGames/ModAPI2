using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Svg;
using ModAPI.ViewModels;
using System;

namespace ModAPI.Components
{
    public partial class SidebarItem : UserControl
    {
        public IImage ImageIcon { get { return GetValue(ImageIconProperty); } set { SetValue(ImageIconProperty, value); } }
        public static readonly StyledProperty<IImage> ImageIconProperty = AvaloniaProperty.Register<SidebarItem, IImage>(nameof(ImageIcon));
        public Material.Icons.MaterialIconKind? MaterialIcon { get { return GetValue(MaterialIconProperty); } set { SetValue(MaterialIconProperty, value); } }
        public static readonly StyledProperty<Material.Icons.MaterialIconKind?> MaterialIconProperty = AvaloniaProperty.Register<SidebarItem, Material.Icons.MaterialIconKind?>(nameof(MaterialIcon));
        public string DisplayName { get { return GetValue(DisplayNameProperty); } set { SetValue(DisplayNameProperty, value); } }
        public static readonly StyledProperty<string> DisplayNameProperty = AvaloniaProperty.Register<SidebarItem, string>(nameof(DisplayName));
        public bool IsHeader { get { return GetValue(IsHeaderProperty); } set { SetValue(IsHeaderProperty, value); } }
        public static readonly StyledProperty<bool> IsHeaderProperty = AvaloniaProperty.Register<SidebarItem, bool>(nameof(IsHeader));
        public bool IsActive { get { return GetValue(IsActiveProperty); } set { SetValue(IsActiveProperty, value); } }
        public static readonly StyledProperty<bool> IsActiveProperty = AvaloniaProperty.Register<SidebarItem, bool>(nameof(IsActive));

        private ViewModelBase CurrentContext;

        public SidebarItem()
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
        /*
        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (this.CurrentContext != null)
                this.CurrentContext.PropertyChanged -= CurrentContext_PropertyChanged;
            if (this.DataContext is ViewModelBase vm)
            {
                this.CurrentContext = vm;
                if (this.DataContext != null)
                    this.CurrentContext.PropertyChanged += CurrentContext_PropertyChanged;
            }
            else
                this.CurrentContext = null;
        }

        private void CurrentContext_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsActive")
            {
                if (this.CurrentContext.IsActive && !this.Classes.Contains("active"))
                    this.Classes.Add("active");
                else if (!this.CurrentContext.IsActive && this.Classes.Contains("active"))
                    this.Classes.Remove("active");
            }
        }*/

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
                this.FindControl<Image>("ImageIcon").IsVisible = ImageIcon != null;
                this.FindControl<Material.Icons.Avalonia.MaterialIcon>("MaterialIcon").IsVisible = MaterialIcon.HasValue;
                if (change.Property.Name == "IsActive")
                {
                    if (IsActive && !this.Classes.Contains("active"))
                        this.Classes.Add("active");
                    else if (!IsActive && this.Classes.Contains("active"))
                        this.Classes.Remove("active");
                }
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if (IsActive && !this.Classes.Contains("active"))
                this.Classes.Add("active");
            else if (!IsActive && this.Classes.Contains("active"))
                this.Classes.Remove("active");
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            var button = this.FindControl<Avalonia.Controls.Button>("Button");
            button.Click += Button_Click;
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
