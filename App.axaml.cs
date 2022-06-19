using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ModAPI.ViewModels;
using ModAPI.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ModAPI
{
    public class App : Application
    {
        public static App Instance;
        public Dictionary<string, Game> Games = new Dictionary<string, Game>();
        public override void Initialize()
        {
            Instance = this;
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                foreach (var g in Configuration.Games)
                    Games.Add(g.Key, new UnityMonoGame(g.Value));
                var context = new AppViewModel();
                context.Games = new ObservableCollection<Game>();
                foreach (var g in Games)
                    context.Games.Add(g.Value);
                desktop.MainWindow = new Views.MainWindow
                {
                    DataContext = context
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
