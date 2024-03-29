﻿using System.Collections.ObjectModel;
using System.Windows.Input;
using WallpapersSlideshower.Models;
using WallpapersSlideshower.Commands;
using System.Collections.Specialized;
using System.Threading;
using System;
using System.Windows;

namespace WallpapersSlideshower.ViewModels
{
    [Serializable]
    public class MainWindowViewModel : ViewModelBase
    {
        private const int WIDTH_OF_ICON_RESOLUTION = 150;

        private readonly WallpapersSlideshow _wallpaperSlideshow;

        public ObservableCollection<WallpaperViewModel> WallpapersViewModels { get; set; }

        public string? PathToFolder { get => _pathToFolder; set => Set(ref _pathToFolder, value); }
        private string? _pathToFolder;

        public WallpaperViewModel? SelectedWallpaperViewModel { get => _selectedWallpaperViewModel; set => Set(ref _selectedWallpaperViewModel, value); }
        private WallpaperViewModel? _selectedWallpaperViewModel;

        public bool SlideshowIsEnabled { get => _slideshowIsEnabled; set => Set(ref _slideshowIsEnabled, value); }
        private bool _slideshowIsEnabled;

        public bool RandomIsEnabled { get => _randomIsEnabled; set => Set(ref _randomIsEnabled, value); }
        private bool _randomIsEnabled;

        public bool WindowVisibility { get => _windowVisility; set => Set(ref _windowVisility, value); }
        private bool _windowVisility = true;

        public WindowState WindowState { get => _windowState; set => Set(ref _windowState, value); }
        private WindowState _windowState;

        public bool AutorunIsEnabled { get => _autorunIsEnabled; set => Set(ref _autorunIsEnabled, value); }
        private bool _autorunIsEnabled;

        public bool Topmost { get => _topmost; set => Set(ref _topmost, value); }
        private bool _topmost;

        public WindowState WindowStateBeforeHide { get; set; }

        public ICommand SelectFolderCommand { get; }
        public ICommand ChangeSlideshowEnabledCommand { get; }
        public ICommand ChangeRandomEnabledCommand { get; }
        public ICommand ChangeWindowVisibilityCommand { get; }
        public ICommand OnWindowStateChangedCommand { get; }
        public ICommand ChangeProgramAutorunCommand { get; }

        public MainWindowViewModel(WallpapersSlideshow wallpaperSlideshow, bool randomIsEnabled, bool slideshowIsEnabled,
            bool autorunIsEnabled)
        {
            _wallpaperSlideshow = wallpaperSlideshow;
            _randomIsEnabled = randomIsEnabled;
            _slideshowIsEnabled = slideshowIsEnabled;
            _autorunIsEnabled = autorunIsEnabled;

            WallpapersViewModels = new ObservableCollection<WallpaperViewModel>();
            WallpapersViewModels.CollectionChanged += OnWallpapersViewModelsChanged;

            PathToFolder = wallpaperSlideshow.PathToWallpapersFolder;

            SelectFolderCommand = new SelectFolderCommand(this, _wallpaperSlideshow);
            ChangeSlideshowEnabledCommand = new ChangeSlideshowEnabledCommand(this, _wallpaperSlideshow);
            ChangeRandomEnabledCommand = new ChangeWallpapersRandomEnabledCommand(this, _wallpaperSlideshow);
            ChangeWindowVisibilityCommand = new ChangeWindowVisibilityCommand(this);
            OnWindowStateChangedCommand = new OnWindowStateChangedCommand(this);
            ChangeProgramAutorunCommand = new ChangeProgramAutorunValueCommand(this);

            if (slideshowIsEnabled)
                ChangeSlideshowEnabledCommand.Execute(true);
            if (autorunIsEnabled)
                WindowVisibility = false;
            ChangeRandomEnabledCommand.Execute(randomIsEnabled);

            _wallpaperSlideshow.DesktopWallpaperChangedEvent += OnDesktopWallpaperChanged;

            UpdateWallpapersViewModels();
        }

        private void OnDesktopWallpaperChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        public void UpdateWallpapersViewModels()
        {
            WallpapersViewModels.CollectionChanged -= OnWallpapersViewModelsChanged;
            WallpapersViewModels.Clear();
            foreach (var wallpaper in _wallpaperSlideshow.ExistingWallpapers)
            {
                var wallpaperViewModel = new WallpaperViewModel(wallpaper.PathToImage, WIDTH_OF_ICON_RESOLUTION);
                WallpapersViewModels.Add(wallpaperViewModel);
            }

            WallpapersViewModels.CollectionChanged -= OnWallpapersViewModelsChanged;
            WallpapersViewModels.CollectionChanged += OnWallpapersViewModelsChanged;

            _currentLoadAllIconImagesCancellationTockenSource?.Cancel();
            _currentLoadAllIconImagesCancellationTockenSource = new CancellationTokenSource();
            LoadAllIconImagesAsync(_currentLoadAllIconImagesCancellationTockenSource);
        }

        private CancellationTokenSource? _currentLoadAllIconImagesCancellationTockenSource;
        private async void LoadAllIconImagesAsync(CancellationTokenSource cancellationTokenSource)
        {
            var wallpapersViewModelsCopy = new WallpaperViewModel[WallpapersViewModels.Count];
            WallpapersViewModels.CopyTo(wallpapersViewModelsCopy, 0);
            foreach (var wallpaperViewModel in wallpapersViewModelsCopy)
            {
                await wallpaperViewModel.LoadIconImageAsync();
                if (cancellationTokenSource.IsCancellationRequested)
                    return;
            }
        }

        private void OnWallpapersViewModelsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            _wallpaperSlideshow.ExistingWallpapers.Clear();
            foreach (var wallpaperViewModel in WallpapersViewModels)
                _wallpaperSlideshow.ExistingWallpapers.Add(new Wallpaper(wallpaperViewModel.PathToImage));
        }
    }
}
