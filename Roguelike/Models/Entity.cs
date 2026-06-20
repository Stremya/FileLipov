using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roguelike.Models
{
    public class Entity : INotifyPropertyChanged
    {
        // Событие для обновления UI (WPF магия)
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int _x;
        public int X
        {
            get => _x;
            set { _x = value; OnPropertyChanged(nameof(X)); }
        }

        private int _y;
        public int Y
        {
            get => _y;
            set { _y = value; OnPropertyChanged(nameof(Y)); }
        }

        private int _hp;
        public int HP
        {
            get => _hp;
            set { _hp = value; OnPropertyChanged(nameof(HP)); }
        }

        public int MaxHP { get; set; }

        public Entity(int x, int y, int maxHp)
        {
            X = x;
            Y = y;
            MaxHP = maxHp;
            HP = maxHp;
        }

        public void Move(int dx, int dy)
        {
            X += dx;
            Y += dy;
        }

        public void TakeDamage(int amount)
        {
            HP -= amount;
            if (HP < 0) HP = 0; // Здоровье не может быть меньше нуля
        }
    }
}
