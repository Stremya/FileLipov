using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roguelike.Models
{
    public class Player : Entity
    {
        private int _score;
        public int Score
        {
            get => _score;
            set { _score = value; OnPropertyChanged(nameof(Score)); }
        }

        public int AttackPower { get; set; }

        public Player(int x, int y, int maxHp, int attackPower)
            : base(x, y, maxHp)
        {
            AttackPower = attackPower;
            Score = 0;
        }

        public void Heal(int amount)
        {
            HP += amount;
            if (HP > MaxHP) HP = MaxHP; // Здоровье не может превышать максимум
        }
    }
}
