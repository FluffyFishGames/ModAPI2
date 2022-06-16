using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ModAPI.ViewModels;
using ModAPI.Views;
using System.Collections.ObjectModel;

namespace ModAPI
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var context = new AppViewModel();
                context.Games = new ObservableCollection<Game>();
                foreach (var g in Configuration.Games)
                    context.Games.Add(g);
                desktop.MainWindow = new Views.MainWindow
                {
                    DataContext = context,
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
