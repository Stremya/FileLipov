using System;
using System.Collections.Generic;
using System.Linq;

namespace Roguelike.Models
{
    public class GameEngine
    {
        public Player CurrentPlayer { get; private set; }
        public List<Enemy> Enemies { get; private set; }
        public Map CurrentMap { get; private set; }
        public int CurrentLevel { get; private set; }
        public bool IsGameOver { get; private set; }

        public bool IsPlayerTurn { get; private set; } = true;

        public event Action GameStateChanged;
        public event Action LevelChanged;
        public event Action GameOver;
        public event Action<string> StatusMessage;

        private const int MapWidth = 30;
        private const int MapHeight = 20;
        private const int EnemiesPerLevel = 2;
        private const int HealthPotionHealAmount = 30;

        public GameEngine()
        {
            Enemies = new List<Enemy>();
            CurrentLevel = 1;
            IsGameOver = false;
        }

        public void StartNewGame()
        {
            CurrentLevel = 1;
            IsGameOver = false;
            Enemies.Clear();
            CurrentPlayer = null;
            GenerateLevel();
        }

        private void GenerateLevel()
        {
            CurrentMap = new Map(MapWidth, MapHeight);
            CurrentMap.GenerateLevel();

            if (CurrentPlayer == null)
            {
                CurrentPlayer = new Player(CurrentMap.PlayerStartX, CurrentMap.PlayerStartY, maxHp: 100, attackPower: 20);
            }
            else
            {
                CurrentPlayer.X = CurrentMap.PlayerStartX;
                CurrentPlayer.Y = CurrentMap.PlayerStartY;
            }

            SpawnEnemies();
            SpawnHealthPotion(); 

            GameStateChanged?.Invoke();
        }

        public void ProcessTurn(int dx, int dy)
        {
            if (IsGameOver) return;

            ProcessPlayerMove(dx, dy);

            if (!IsGameOver)
            {
                ProcessEnemiesTurn();
            }

            GameStateChanged?.Invoke();
        }

        private void ProcessPlayerMove(int dx, int dy)
        {
            if (dx == 0 && dy == 0) return;

            int newX = CurrentPlayer.X + dx;
            int newY = CurrentPlayer.Y + dy;

            Enemy targetEnemy = Enemies.FirstOrDefault(e => e.X == newX && e.Y == newY);

            if (targetEnemy != null)
            {
                AttackEnemy(targetEnemy);
            }
            else if (CurrentMap.IsWalkable(newX, newY))
            {
                CurrentPlayer.Move(dx, dy);

                if (CurrentMap.Grid[newX, newY] == TileType.HealthPotion)
                {
                    CurrentPlayer.Heal(HealthPotionHealAmount);
                    CurrentMap.Grid[newX, newY] = TileType.Floor;
                    StatusMessage?.Invoke($"+{HealthPotionHealAmount} HP! Аптечка подобрана!");
                }

                if (newX == CurrentMap.ExitX && newY == CurrentMap.ExitY)
                {
                    NextLevel();
                }
            }
        }

        private void AttackEnemy(Enemy enemy)
        {
            enemy.TakeDamage(CurrentPlayer.AttackPower);

            if (enemy.HP <= 0)
            {
                Enemies.Remove(enemy);
                CurrentPlayer.Score += 50;
            }
        }

        private void ProcessEnemiesTurn()
        {
            foreach (var enemy in Enemies.ToList())
            {
                int distanceX = Math.Abs(enemy.X - CurrentPlayer.X);
                int distanceY = Math.Abs(enemy.Y - CurrentPlayer.Y);

                if (distanceX + distanceY == 1)
                {
                    EnemyAttackPlayer(enemy);
                }
                else
                {
                    int oldX = enemy.X;
                    int oldY = enemy.Y;

                    enemy.MoveTowards(CurrentPlayer);

                    if (!CurrentMap.IsWalkable(enemy.X, enemy.Y) ||
                        Enemies.Any(e => e != enemy && e.X == enemy.X && e.Y == enemy.Y))
                    {
                        enemy.X = oldX;
                        enemy.Y = oldY;
                    }
                }
            }
        }

        private void EnemyAttackPlayer(Enemy enemy)
        {
            CurrentPlayer.TakeDamage(enemy.AttackPower);

            if (CurrentPlayer.HP <= 0)
            {
                IsGameOver = true;
                GameOver?.Invoke();
            }
        }

        public void NextLevel()
        {
            CurrentLevel++;
            CurrentPlayer.Heal(20);
            GenerateLevel();
            LevelChanged?.Invoke();
        }

        private void SpawnEnemies()
        {
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
                    Enemy enemy = new Enemy(x, y, maxHp: 20 + (CurrentLevel * 5), attackPower: 3 + CurrentLevel);
                    Enemies.Add(enemy);
                    enemiesSpawned++;
                }

                attempts++;
            }
        }

        private void SpawnHealthPotion()
        {
            Random random = new Random();
            int potionsSpawned = 0; 
            int attempts = 0;

            while (potionsSpawned < 3 && attempts < 300)
            {
                int x = random.Next(1, MapWidth - 1);
                int y = random.Next(1, MapHeight - 1);

                if (CurrentMap.IsWalkable(x, y) &&
                    !(x == CurrentPlayer.X && y == CurrentPlayer.Y) &&
                    !(x == CurrentMap.ExitX && y == CurrentMap.ExitY) &&
                    !Enemies.Any(e => e.X == x && e.Y == y) &&
                    !CurrentMap.IsInSameRoom(x, y, CurrentPlayer.X, CurrentPlayer.Y) &&
                    !CurrentMap.IsInSameRoom(x, y, CurrentMap.ExitX, CurrentMap.ExitY))
                {
                    CurrentMap.Grid[x, y] = TileType.HealthPotion;
                    potionsSpawned++; 
                }
                attempts++;
            }
        }
    }
}