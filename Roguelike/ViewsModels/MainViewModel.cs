using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading; 
using Roguelike.Models;
using System.Windows.Media; 

namespace Roguelike.ViewModels
{
    public enum AppState { MainMenu, Playing, GameOver }

    public class MapCell : BaseViewModel
    {
        private string _cellColor;
        public string CellColor
        {
            get => _cellColor;
            set => SetProperty(ref _cellColor, value);
        }
    }

    public class MainViewModel : BaseViewModel
    {
        private GameEngine _engine;
        private DispatcherTimer _enemyTimer;
        private MediaPlayer _bgMusic;

        public ObservableCollection<MapCell> MapCells { get; set; }

        public ICommand MovementCommand { get; }
        public ICommand StartGameCommand { get; }
        public ICommand GoToMenuCommand { get; }

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
            }
        }

        public bool IsMainMenuVisible => CurrentState == AppState.MainMenu;
        public bool IsPlayingVisible => CurrentState == AppState.Playing;
        public bool IsGameOverVisible => CurrentState == AppState.GameOver;

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

            // --- НАСТРОЙКА ТАЙМЕРА ВРАГОВ ---
            _enemyTimer = new DispatcherTimer();
            _enemyTimer.Interval = TimeSpan.FromSeconds(0.8); // Скорость врагов (0.8 секунды на шаг)
            _enemyTimer.Tick += EnemyTimer_Tick;

            MovementCommand = new RelayCommand(ExecuteMovement);
            StartGameCommand = new RelayCommand((_) => StartNewGame());
            GoToMenuCommand = new RelayCommand((_) => GoToMenu());

            InitializeMapCells();
            _bgMusic = new MediaPlayer();
            // Указываем путь к нашему файлу
            _bgMusic.Open(new Uri("Sounds/bgm.mp3", UriKind.Relative));
            _bgMusic.Volume = 0.3; // Громкость (от 0.0 до 1.0, 0.3 - чтобы не оглохнуть)

            // Зацикливаем музыку (когда трек кончится, он начнется заново)
            _bgMusic.MediaEnded += (sender, args) =>
            {
                _bgMusic.Position = TimeSpan.Zero;
                _bgMusic.Play();
            };

            // Врубаем музон прямо со старта (будет играть и в меню, и в игре)
            _bgMusic.Play();
        }

        // Событие, которое срабатывает каждый "тик" таймера
        private void EnemyTimer_Tick(object sender, EventArgs e)
        {
            // Заставляем врагов ходить, передавая нули (игрок стоит на месте)
            if (CurrentState == AppState.Playing)
            {
                _engine.ProcessTurn(0, 0);
            }
        }

        private void StartNewGame()
        {
            _engine.StartNewGame();
            CurrentState = AppState.Playing;
            _enemyTimer.Start(); // ЗАПУСКАЕМ ВРАГОВ
            UpdateView();
        }

        private void HandleGameOver()
        {
            _enemyTimer.Stop(); // ОСТАНАВЛИВАЕМ ВРАГОВ
            CurrentState = AppState.GameOver;
        }

        private void GoToMenu()
        {
            _enemyTimer.Stop(); // ОСТАНАВЛИВАЕМ ВРАГОВ
            CurrentState = AppState.MainMenu;
        }

        private void InitializeMapCells()
        {
            MapCells.Clear();
            int totalCells = MapWidth * MapHeight;
            for (int i = 0; i < totalCells; i++)
            {
                MapCells.Add(new MapCell { CellColor = "Black" });
            }
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

                    if (_engine.CurrentMap.Grid[x, y] == TileType.Wall) color = "DarkGray";
                    else if (_engine.CurrentMap.Grid[x, y] == TileType.Floor) color = "LightGray";
                    else if (_engine.CurrentMap.Grid[x, y] == TileType.Exit) color = "Gold";

                    if (_engine.Enemies.Any(e => e.X == x && e.Y == y)) color = "Red";

                    if (_engine.CurrentPlayer != null && _engine.CurrentPlayer.X == x && _engine.CurrentPlayer.Y == y) color = "Blue";

                    if (MapCells[index].CellColor != color)
                    {
                        MapCells[index].CellColor = color;
                    }
                }
            }

            OnPropertyChanged(nameof(PlayerHP));
            OnPropertyChanged(nameof(Score));
            OnPropertyChanged(nameof(CurrentLevel));
        }

        private void ExecuteMovement(object parameter)
        {
            if (CurrentState != AppState.Playing) return;

            if (parameter is string direction)
            {
                int dx = 0, dy = 0;
                if (direction == "Up") dy = -1;
                else if (direction == "Down") dy = 1;
                else if (direction == "Left") dx = -1;
                else if (direction == "Right") dx = 1;

                _engine.ProcessTurn(dx, dy);

                // Сбрасываем таймер при нашем шаге. 
                // Это нужно, чтобы враг не сделал 2 шага подряд (один от нашего удара, второй от таймера)
                _enemyTimer.Stop();
                _enemyTimer.Start();
            }
        }
    }
}