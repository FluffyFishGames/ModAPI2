using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;

namespace ModAPI.Components
{
    public partial class Button : UserControl
    {
        public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
            RoutedEvent.Register<Button, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

        // Provide CLR accessors for the event
        public event EventHandler<RoutedEventArgs> Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }
        public Button()
        {
            InitializeComponent();
        }

        protected override void OnPointerEnter(PointerEventArgs e)
        {
            base.OnPointerEnter(e);
            this.Classes.Add("hover");
            this.Classes.Remove("normal");
        }

        protected override void OnPointerLeave(PointerEventArgs e)
        {
            base.OnPointerLeave(e);
            this.Classes.Remove("hover");
            this.Classes.Add("normal");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            var button = this.FindControl<Avalonia.Controls.Button>("Button");
            button.Click += Button_Click;
        }

        private void Button_Click(object? sender, RoutedEventArgs e)
        {
            this.RaiseEvent(new RoutedEventArgs() { Source = this, RoutedEvent = ClickEvent });
        }
    }
}
