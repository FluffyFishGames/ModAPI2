using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace ModAPI.ViewModels
{
    public class AppViewModel : ViewModelBase
    {
        public static AppViewModel Instance;
        public AppViewModel()
        {
            Instance = this;
        }

        public void BrowseTo(ViewModelBase page)
        {
            foreach (var _game in _Games)
            {
                if (_game != page)
                    _game.IsActive = false;
            }
            if (page != this)
                this.IsActive = false;
            Page = page;
        }

        private ViewModelBase _Page;
        public ViewModelBase Page { get => _Page; set => this.RaiseAndSetIfChanged<AppViewModel, ViewModelBase>(ref _Page, value, "Page"); }

        private ObservableCollection<Game> _Games;
        public ObservableCollection<Game> Games 
        {
            get => _Games;
            set => this.RaiseAndSetIfChanged<AppViewModel, ObservableCollection<Game>>(ref _Games, value, "Games");
        }

        public Popup _CurrentPopup;
        public Popup CurrentPopup { 
            get => _CurrentPopup; 
            set 
            {
                if (_CurrentPopup != value)
                {
                    _CurrentPopup = value;
                    if (value != null && value is Popup popup)
                    {
                        popup.OnClose += () =>
                        {
                            CurrentPopup = null;
                        };
                    }
                    this.RaisePropertyChanged<AppViewModel>("CurrentPopup");
                }
            } 
        }
    }
}
