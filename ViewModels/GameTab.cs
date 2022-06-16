using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;

namespace ModAPI.ViewModels
{
    public class GameTab : ViewModelBase
    {
        public GameTab(Game viewModel, string displayName, Material.Icons.MaterialIconKind materialIcon)
        {
            _Data = viewModel;
            _MaterialIcon = materialIcon;
            _DisplayName = displayName;
            this.PropertyChanged += GameTabViewModel_PropertyChanged;
        }

        public GameTab(Game viewModel, string displayName, string imageIcon)
        {
            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            var bitmap = new Bitmap(assets.Open(new Uri(imageIcon)));
            _ImageIcon = bitmap;
            _Data = viewModel;
            _DisplayName = displayName;
            this.PropertyChanged += GameTabViewModel_PropertyChanged;
        }

        public GameTab(Game viewModel, string displayName)
        {
            _Data = viewModel;
            _DisplayName = displayName;
            this.PropertyChanged += GameTabViewModel_PropertyChanged;
        }

        private void GameTabViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsActive" && IsActive == true)
                _Data.BrowseTo(this);
        }

        private Game _Data;
        public Game Data { get => _Data; set => this.RaiseAndSetIfChanged<GameTab, Game>(ref _Data, value, "Data"); }
        
        private IImage _ImageIcon;
        public IImage ImageIcon { get => _ImageIcon; set => this.RaiseAndSetIfChanged<GameTab, IImage>(ref _ImageIcon, value, "ImageIcon"); }

        private Material.Icons.MaterialIconKind? _MaterialIcon;
        public Material.Icons.MaterialIconKind? MaterialIcon { get => _MaterialIcon; set => this.RaiseAndSetIfChanged<GameTab, Material.Icons.MaterialIconKind?>(ref _MaterialIcon, value, "MaterialIcon"); }

        private string _DisplayName;
        public string DisplayName { get => _DisplayName; set => this.RaiseAndSetIfChanged<GameTab, string>(ref _DisplayName, value, "DisplayName"); }

    }
}
