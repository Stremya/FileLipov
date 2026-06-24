using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Models
{
    public class GameEngine
    {
        // === СВОЙСТВА ===
        public Player CurrentPlayer { get; private set; }
        public List<Enemy> Enemies { get; private set; }
        public Map CurrentMap { get; private set; }
        public int CurrentLevel { get; private set; }
        public bool IsGameOver { get; private set; }

        // === СОБЫТИЯ ДЛЯ UI ===
        // Вызывается после каждого хода - UI должен обновиться
        public event Action GameStateChanged;

        // Вызывается при переходе на новый уровень
        public event Action LevelChanged;

        // Вызывается при поражении
        public event Action GameOver;

        // === КОНСТАНТЫ ===
        private const int MapWidth = 30;
        private const int MapHeight = 20;
        private const int EnemiesPerLevel = 2; // Врагов на каждом уровне

        // === КОНСТРУКТОР ===
        public GameEngine()
        {
            Enemies = new List<Enemy>();
            CurrentLevel = 1;
            IsGameOver = false;
        }

        // === ЗАПУСК НОВОЙ ИГРЫ ===
        public void StartNewGame()
        {
            CurrentLevel = 1;
            IsGameOver = false;
            Enemies.Clear();

            // ДОБАВИТЬ ЭТУ СТРОЧКУ:
            CurrentPlayer = null;

            GenerateLevel();
        }

        // === ГЕНЕРАЦИЯ УРОВНЯ ===
        private void GenerateLevel()
        {
            System.Diagnostics.Debug.WriteLine("=== НАЧАЛО ГЕНЕРАЦИИ УРОВНЯ ===");

            // Создаем карту и генерируем уровень
            CurrentMap = new Map(MapWidth, MapHeight);
            CurrentMap.GenerateLevel();
            System.Diagnostics.Debug.WriteLine($"Карта создана: {MapWidth}x{MapHeight}");

            // Создаем игрока на стартовой позиции
            if (CurrentPlayer == null)
            {
                // Если это первый этаж, создаем игрока с нуля
                CurrentPlayer = new Player(
                    CurrentMap.PlayerStartX,
                    CurrentMap.PlayerStartY,
                    maxHp: 100,
                    attackPower: 20
                );
                System.Diagnostics.Debug.WriteLine($"Игрок создан на ({CurrentPlayer.X}, {CurrentPlayer.Y})");
            }
            else
            {
                // Если мы перешли на новый этаж, просто меняем координаты старого игрока (очки и ХП сохраняются!)
                CurrentPlayer.X = CurrentMap.PlayerStartX;
                CurrentPlayer.Y = CurrentMap.PlayerStartY;
                System.Diagnostics.Debug.WriteLine($"Игрок перенесен на ({CurrentPlayer.X}, {CurrentPlayer.Y})");
            }
            System.Diagnostics.Debug.WriteLine("Начинаем спавн врагов...");
            SpawnEnemies();
            System.Diagnostics.Debug.WriteLine($"Врагов заспавнено: {Enemies.Count}");

            // Уведомляем UI
            GameStateChanged?.Invoke();

            System.Diagnostics.Debug.WriteLine("=== КОНЕЦ ГЕНЕРАЦИИ УРОВНЯ ===");
        }

        // === ОСНОВНОЙ МЕТОД: ОБРАБОТКА ХОДА ===
        public void ProcessTurn(int dx, int dy)
        {
            // Если игра окончена - ничего не делаем
            if (IsGameOver) return;

            // 1. Ход игрока
            ProcessPlayerMove(dx, dy);

            // 2. Если игрок не умер - ходят враги
            if (!IsGameOver)
            {
                ProcessEnemiesTurn();
            }

            // 3. Уведомляем UI об изменениях
            GameStateChanged?.Invoke();
        }

        // === ДВИЖЕНИЕ ИГРОКА ===
        private void ProcessPlayerMove(int dx, int dy)
        {
            // Если игрок не двигается (dx=0, dy=0) - пропускаем ход
            if (dx == 0 && dy == 0) return;

            int newX = CurrentPlayer.X + dx;
            int newY = CurrentPlayer.Y + dy;

            // Проверяем, есть ли враг на целевой клетке (bump-to-attack)
            Enemy targetEnemy = Enemies.FirstOrDefault(e => e.X == newX && e.Y == newY);

            if (targetEnemy != null)
            {
                // Атакуем врага!
                AttackEnemy(targetEnemy);
            }
            else if (CurrentMap.IsWalkable(newX, newY))
            {
                // Двигаемся, если клетка проходима
                CurrentPlayer.Move(dx, dy);

                // Проверяем, дошел ли игрок до выхода
                if (newX == CurrentMap.ExitX && newY == CurrentMap.ExitY)
                {
                    NextLevel();
                }
            }
            // Иначе - игрок упирается в стену, ход теряется
        }

        // === АТАКА ВРАГА (bump-to-attack) ===
        private void AttackEnemy(Enemy enemy)
        {
            enemy.TakeDamage(CurrentPlayer.AttackPower);

            // Если враг умер - убираем его и начисляем очки
            if (enemy.HP <= 0)
            {
                Enemies.Remove(enemy);
                CurrentPlayer.Score += 50; // Очки за убийство врага
            }
        }

        // === ХОД ВРАГОВ ===
        private void ProcessEnemiesTurn()
        {
            foreach (var enemy in Enemies.ToList()) // ToList() чтобы безопасно удалять во время цикла
            {
                // Проверяем, соседствует ли враг с игроком (атака)
                int distanceX = Math.Abs(enemy.X - CurrentPlayer.X);
                int distanceY = Math.Abs(enemy.Y - CurrentPlayer.Y);

                if (distanceX + distanceY == 1) // Враг рядом с игроком (по вертикали или горизонтали)
                {
                    // Враг атакует игрока
                    EnemyAttackPlayer(enemy);
                }
                else
                {
                    // Враг двигается к игроку
                    int oldX = enemy.X;
                    int oldY = enemy.Y;

                    enemy.MoveTowards(CurrentPlayer);

                    // Проверяем, не врезался ли враг в стену или другого врага
                    if (!CurrentMap.IsWalkable(enemy.X, enemy.Y) ||
                        Enemies.Any(e => e != enemy && e.X == enemy.X && e.Y == enemy.Y))
                    {
                        // Возвращаем на старую позицию
                        enemy.X = oldX;
                        enemy.Y = oldY;
                    }
                }
            }
        }

        // === АТАКА ВРАГА ПО ИГРОКУ ===
        private void EnemyAttackPlayer(Enemy enemy)
        {
            CurrentPlayer.TakeDamage(enemy.AttackPower);

            // Проверяем, не умер ли игрок
            if (CurrentPlayer.HP <= 0)
            {
                IsGameOver = true;
                GameOver?.Invoke();
            }
        }

        // === ПЕРЕХОД НА СЛЕДУЮЩИЙ УРОВЕНЬ ===
        public void NextLevel()
        {
            CurrentLevel++;

            // Лечим игрока немного при переходе на новый уровень
            CurrentPlayer.Heal(20);

            // Генерируем новый уровень
            GenerateLevel();

            // Уведомляем UI
            LevelChanged?.Invoke();
        }

        // === СПАВН ВРАГОВ ===
        private void SpawnEnemies()
        {
            System.Diagnostics.Debug.WriteLine($"[SpawnEnemies] Начало. Нужно заспавнить: {EnemiesPerLevel}");

            Random random = new Random();
            int enemiesSpawned = 0;
            int attempts = 0;

            while (enemiesSpawned < EnemiesPerLevel && attempts < 100)
            {
                int x = random.Next(0, MapWidth);
                int y = random.Next(0, MapHeight);

                if (CurrentMap.IsWalkable(x, y) &&
                    !(x == CurrentPlayer.X && y == CurrentPlayer.Y) &&
                    !(x == CurrentMap.ExitX && y == CurrentMap.ExitY) &&
                    !Enemies.Any(e => e.X == x && e.Y == y))
                {
                    Enemy enemy = new Enemy(x, y, maxHp: 30 + (CurrentLevel * 5), attackPower: 10 + (CurrentLevel * 2));
                    Enemies.Add(enemy);
                    enemiesSpawned++;
                    System.Diagnostics.Debug.WriteLine($"[SpawnEnemies] ✅ Враг #{enemiesSpawned} создан на ({x}, {y})");
                }

                attempts++;
            }

            System.Diagnostics.Debug.WriteLine($"[SpawnEnemies] Итого: {Enemies.Count} врагов за {attempts} попыток");

        }
    }
}