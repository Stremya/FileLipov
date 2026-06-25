using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using Roguelike.Models;

namespace Roguelike.ViewModels
{
    // Все состояния игры на месте
    public enum AppState { MainMenu, Playing, GameOver, PauseMenu }

    public class MapCell : BaseViewModel
    {
        private string _cellColor;
        public string CellColor { get => _cellColor; set => SetProperty(ref _cellColor, value); }

        private string _cellText;
        public string CellText { get => _cellText; set => SetProperty(ref _cellText, value); }
    }

    public class MainViewModel : BaseViewModel
    {
        private GameEngine _engine;
        private DispatcherTimer _enemyTimer;
        private MediaPlayer _bgMusic;

        public ObservableCollection<MapCell> MapCells { get; set; }

        // --- ВСЕ КОМАНДЫ ДЛЯ КНОПОК ---
        public ICommand MovementCommand { get; }
        public ICommand StartGameCommand { get; }
        public ICommand GoToMenuCommand { get; }
        public ICommand ToggleSoundCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand PauseCommand { get; }

        private AppState _currentState = AppState.MainMenu;
        public AppState CurrentState
        {
            get => _currentState;
            set
            {
                SetProperty(ref _currentState, value);
                OnPropertyChanged(nameof(IsMainMenuVisible));
                OnPropertyChanged(nameof(IsPlayingVisible));
                OnPropertyChanged(nameof(IsGameOverVisible));
                // Теперь интерфейс точно знает, когда показывать паузу!
                OnPropertyChanged(nameof(IsPauseVisible));
            }
        }

        public bool IsMainMenuVisible => CurrentState == AppState.MainMenu;
        public bool IsPlayingVisible => CurrentState == AppState.Playing;
        public bool IsGameOverVisible => CurrentState == AppState.GameOver;
        public bool IsPauseVisible => CurrentState == AppState.PauseMenu;

        public int MapWidth => 30;
        public int MapHeight => 20;

        public int PlayerHP => _engine?.CurrentPlayer?.HP ?? 0;
        public int Score => _engine?.CurrentPlayer?.Score ?? 0;
        public int CurrentLevel => _engine?.CurrentLevel ?? 1;

        public MainViewModel()
        {
            MapCells = new ObservableCollection<MapCell>();
            _engine = new GameEngine();

            _engine.GameStateChanged += UpdateView;
            _engine.LevelChanged += UpdateView;
            _engine.GameOver += HandleGameOver;

            _enemyTimer = new DispatcherTimer();
            _enemyTimer.Interval = TimeSpan.FromSeconds(0.8);
            _enemyTimer.Tick += EnemyTimer_Tick;

            try
            {
                _bgMusic = new MediaPlayer();
                _bgMusic.Volume = 0.3;
                _bgMusic.MediaEnded += (s, e) => { _bgMusic.Position = TimeSpan.Zero; _bgMusic.Play(); };

                _bgMusic.MediaOpened += (s, e) =>
                {
                    _bgMusic.Play();
                    System.Diagnostics.Debug.WriteLine("✅ Музыка играет!");
                };

                _bgMusic.MediaFailed += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки музыки: {e.ErrorException?.Message}");
                };

                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", "bgm.mp3");
                _bgMusic.Open(new Uri(path));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Исключение: {ex.Message}");
            }

            // Инициализация всех кнопок
            MovementCommand = new RelayCommand(ExecuteMovement);
            StartGameCommand = new RelayCommand((_) => StartNewGame());
            GoToMenuCommand = new RelayCommand((_) => GoToMenu());
            ToggleSoundCommand = new RelayCommand((_) => _bgMusic.IsMuted = !_bgMusic.IsMuted);
            ExitCommand = new RelayCommand((_) => System.Windows.Application.Current.Shutdown());
            PauseCommand = new RelayCommand((_) => TogglePause());

            InitializeMapCells();
        }

        private void TogglePause()
        {
            if (CurrentState == AppState.Playing)
            {
                _enemyTimer.Stop();
                CurrentState = AppState.PauseMenu;
            }
            else if (CurrentState == AppState.PauseMenu)
            {
                _enemyTimer.Start();
                CurrentState = AppState.Playing;
                UpdateView();
            }
        }

        private void EnemyTimer_Tick(object sender, EventArgs e)
        {
            if (CurrentState == AppState.Playing) _engine.ProcessTurn(0, 0);
        }

        private void StartNewGame()
        {
            _engine.StartNewGame();
            CurrentState = AppState.Playing;
            _enemyTimer.Start();
            UpdateView();
        }

        private void HandleGameOver()
        {
            _enemyTimer.Stop();
            CurrentState = AppState.GameOver;
        }

        private void GoToMenu()
        {
            _enemyTimer.Stop();
            CurrentState = AppState.MainMenu;
        }

        private void InitializeMapCells()
        {
            MapCells.Clear();
            int totalCells = MapWidth * MapHeight;
            for (int i = 0; i < totalCells; i++) MapCells.Add(new MapCell { CellColor = "Black", CellText = "" });
        }

        private void UpdateView()
        {
            if (_engine.CurrentMap == null) return;

            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    int index = y * MapWidth + x;
                    string color = "Black";
                    string text = "";

                    if (_engine.CurrentMap.Grid[x, y] == TileType.Wall) color = "DarkGray";
                    else if (_engine.CurrentMap.Grid[x, y] == TileType.Floor) color = "LightGray";
                    else if (_engine.CurrentMap.Grid[x, y] == TileType.Exit) color = "Gold";
                    else if (_engine.CurrentMap.Grid[x, y] == TileType.HealthPotion)
                    {
                        color = "Lime"; // Зеленые аптечки
                        text = "✚";
                    }

                    var enemy = _engine.Enemies.FirstOrDefault(e => e.X == x && e.Y == y);
                    if (enemy != null)
                    {
                        color = "DarkRed";
                        text = enemy.HP.ToString(); // ХП врагов
                    }

                    if (_engine.CurrentPlayer != null && _engine.CurrentPlayer.X == x && _engine.CurrentPlayer.Y == y)
                    {
                        color = "Blue";
                        text = "☺";
                    }

                    if (MapCells[index].CellColor != color) MapCells[index].CellColor = color;
                    if (MapCells[index].CellText != text) MapCells[index].CellText = text;
                }
            }

            OnPropertyChanged(nameof(PlayerHP));
            OnPropertyChanged(nameof(Score));
            OnPropertyChanged(nameof(CurrentLevel));
        }

        private void ExecuteMovement(object parameter)
        {
            if (parameter is string direction)
            {
                if (direction == "Escape")
                {
                    TogglePause();
                    return;
                }

                if (CurrentState != AppState.Playing) return;

                int dx = 0, dy = 0;
                if (direction == "Up") dy = -1;
                else if (direction == "Down") dy = 1;
                else if (direction == "Left") dx = -1;
                else if (direction == "Right") dx = 1;

                _engine.ProcessTurn(dx, dy);

                _enemyTimer.Stop();
                _enemyTimer.Start();
            }
        }
    }
}