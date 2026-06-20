using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roguelike.Models
{
    public class Enemy : Entity
    {
        public int AttackPower { get; set; }

        public Enemy(int x, int y, int maxHp, int attackPower)
            : base(x, y, maxHp)
        {
            AttackPower = attackPower;
        }

        public void MoveTowards(Player player)
        {
            int dx = 0;
            int dy = 0;

            // Простой ИИ: движение по той оси, где расстояние до игрока больше
            // Это обеспечивает движение по клеткам (4 направления), чтобы враги не "срезали углы" стен
            if (Math.Abs(player.X - X) > Math.Abs(player.Y - Y))
            {
                dx = Math.Sign(player.X - X);
            }
            else
            {
                dy = Math.Sign(player.Y - Y);
            }

            Move(dx, dy);
        }
    }
}
