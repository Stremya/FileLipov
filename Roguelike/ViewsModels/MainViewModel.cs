/*
связующее звено между View и Model:
Свойства для отображения: PlayerHP, CurrentLevel, Score
Команды для управления (MovementCommand)
Ссылка на GameEngine
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Roguelike.Models; // Подключаем модели друга

namespace Roguelike.ViewModels
{
    // Класс для отдельной клетки на экране
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
        // --- ССЫЛКИ НА МОДЕЛИ ДРУГА ---
        private Map _currentMap;
        private Player _player;
        private List<Enemy> _enemies;

        // --- СВОЙСТВА ДЛЯ ИНТЕРФЕЙСА ---
        public ObservableCollection<MapCell> MapCells { get; set; }
        public ICommand MovementCommand { get; }

        // Динамические размеры поля (берутся из сгенерированной карты)
        public int MapWidth => _currentMap?.Width ?? 20;
        public int MapHeight => _currentMap?.Height ?? 20;

        // Данные игрока
        public int PlayerHP => _player?.HP ?? 0;
        public int Score => _player?.Score ?? 0;

        private int _currentLevel = 1;
        public int CurrentLevel
        {
            get => _currentLevel;
            set => SetProperty(ref _currentLevel, value);
        }

        public MainViewModel()
        {
            MapCells = new ObservableCollection<MapCell>();
            _enemies = new List<Enemy>();
            MovementCommand = new RelayCommand(ExecuteMovement);

            StartNewLevel();
        }

        private void StartNewLevel()
        {
            // 1. Создаем карту (например, 25x25) и генерируем комнаты (метод друга)
            _currentMap = new Map(25, 25);
            _currentMap.GenerateLevel();

            // 2. Создаем игрока или перемещаем существующего на старт
            if (_player == null)
            {
                // Игрок: HP = 100, Урон = 10 (по конструктору друга)
                _player = new Player(_currentMap.PlayerStartX, _currentMap.PlayerStartY, 100, 10);
            }
            else
            {
                _player.X = _currentMap.PlayerStartX;
                _player.Y = _currentMap.PlayerStartY;
            }

            // Уведомляем XAML, что размеры карты и статы могли измениться
            OnPropertyChanged(nameof(MapWidth));
            OnPropertyChanged(nameof(MapHeight));
            OnPropertyChanged(nameof(PlayerHP));
            OnPropertyChanged(nameof(Score));

            // Перерисовываем интерфейс
            InitializeMapCells();
            UpdateMapRendering();
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

        // Метод, который считывает карту друга и красит наши квадратики
        private void UpdateMapRendering()
        {
            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    int index = y * MapWidth + x;
                    string color = "Black";

                    // Проверяем тип клетки по Enum'у друга
                    if (_currentMap.Grid[x, y] == TileType.Wall) color = "DarkGray"; // Стена
                    else if (_currentMap.Grid[x, y] == TileType.Floor) color = "LightGray"; // Пол
                    else if (_currentMap.Grid[x, y] == TileType.Exit) color = "Gold"; // Выход

                    // Рисуем врагов (если друг потом добавит их спавн)
                    if (_enemies.Any(e => e.X == x && e.Y == y)) color = "Red";

                    // Игрок рисуется поверх всего
                    if (_player.X == x && _player.Y == y) color = "Blue";

                    // Обновляем только если цвет изменился
                    if (MapCells[index].CellColor != color)
                    {
                        MapCells[index].CellColor = color;
                    }
                }
            }
        }

        private void ExecuteMovement(object parameter)
        {
            if (parameter is string direction)
            {
                int dx = 0, dy = 0;
                if (direction == "Up") dy = -1;
                else if (direction == "Down") dy = 1;
                else if (direction == "Left") dx = -1;
                else if (direction == "Right") dx = 1;

                int newX = _player.X + dx;
                int newY = _player.Y + dy;

                // Используем проверку друга на проходимость (IsWalkable)
                if (_currentMap.IsWalkable(newX, newY))
                {
                    _player.Move(dx, dy);

                    // Проверяем, наступил ли игрок на выход (Exit)
                    if (_currentMap.Grid[_player.X, _player.Y] == TileType.Exit)
                    {
                        CurrentLevel++;
                        _player.Score += 50; // Начисляем очки за прохождение
                        StartNewLevel(); // Генерируем новый лабиринт
                        return; // Прерываем этот ход, так как загрузилась новая карта
                    }
                }

                // Перерисовываем графику после хода
                UpdateMapRendering();
                OnPropertyChanged(nameof(PlayerHP));
                OnPropertyChanged(nameof(Score));
            }
        }
    }
}