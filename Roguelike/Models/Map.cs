using System;
using System.Collections.Generic;

namespace Roguelike.Models
{
    public class Map
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public TileType[,] Grid { get; set; }

        public int PlayerStartX { get; private set; }
        public int PlayerStartY { get; private set; }
        public int ExitX { get; private set; }
        public int ExitY { get; private set; }

        // Исправлено: теперь это инициализированный список
        private List<(int x, int y, int w, int h)> _rooms;

        public Map(int width, int height)
        {
            Width = width;
            Height = height;
            Grid = new TileType[width, height];
            _rooms = new List<(int x, int y, int w, int h)>(); // ИНИЦИАЛИЗАЦИЯ
        }

        public void GenerateLevel()
        {
            _rooms.Clear(); // Очищаем старые комнаты при новом уровне

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Grid[x, y] = TileType.Wall;
                }
            }

            int numberOfRooms = 5;
            (int x, int y, int w, int h)[] rooms = new (int, int, int, int)[numberOfRooms];
            Random random = new Random();

            for (int i = 0; i < numberOfRooms; i++)
            {
                int roomWidth = random.Next(4, 8);
                int roomHeight = random.Next(4, 8);
                int roomX = random.Next(1, Width - roomWidth - 1);
                int roomY = random.Next(1, Height - roomHeight - 1);

                rooms[i] = (roomX, roomY, roomWidth, roomHeight);
                _rooms.Add(rooms[i]); // ИСПРАВЛЕНО: Добавляем комнату в память
                CreateRoom(roomX, roomY, roomWidth, roomHeight);
            }

            for (int i = 0; i < numberOfRooms - 1; i++)
            {
                var room1Center = GetCenter(rooms[i]);
                var room2Center = GetCenter(rooms[i + 1]);

                if (random.Next(2) == 0)
                {
                    CreateHorizontalCorridor(room1Center.x, room2Center.x, room1Center.y);
                    CreateVerticalCorridor(room1Center.y, room2Center.y, room2Center.x);
                }
                else
                {
                    CreateVerticalCorridor(room1Center.y, room2Center.y, room1Center.x);
                    CreateHorizontalCorridor(room1Center.x, room2Center.x, room2Center.y);
                }
            }

            var firstRoomCenter = GetCenter(rooms[0]);
            PlayerStartX = firstRoomCenter.x;
            PlayerStartY = firstRoomCenter.y;

            var lastRoomCenter = GetCenter(rooms[numberOfRooms - 1]);
            ExitX = lastRoomCenter.x;
            ExitY = lastRoomCenter.y;
            Grid[ExitX, ExitY] = TileType.Exit;
        }

        public bool IsInSameRoom(int x, int y, int refX, int refY)
        {
            foreach (var room in _rooms)
            {
                if (refX >= room.x && refX < room.x + room.w &&
                    refY >= room.y && refY < room.y + room.h)
                {
                    return x >= room.x && x < room.x + room.w &&
                           y >= room.y && y < room.y + room.h;
                }
            }
            return false;
        }

        public bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return false;
            return Grid[x, y] != TileType.Wall;
        }

        private void CreateRoom(int x, int y, int width, int height)
        {
            for (int i = x; i < x + width; i++)
            {
                for (int j = y; j < y + height; j++)
                {
                    Grid[i, j] = TileType.Floor;
                }
            }
        }

        private void CreateHorizontalCorridor(int x1, int x2, int y)
        {
            int start = Math.Min(x1, x2);
            int end = Math.Max(x1, x2);
            for (int x = start; x <= end; x++)
            {
                Grid[x, y] = TileType.Floor;
            }
        }

        private void CreateVerticalCorridor(int y1, int y2, int x)
        {
            int start = Math.Min(y1, y2);
            int end = Math.Max(y1, y2);
            for (int y = start; y <= end; y++)
            {
                Grid[x, y] = TileType.Floor;
            }
        }

        private (int x, int y) GetCenter((int x, int y, int w, int h) room)
        {
            return (room.x + room.w / 2, room.y + room.h / 2);
        }
    }
}