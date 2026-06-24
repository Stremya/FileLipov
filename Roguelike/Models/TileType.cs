using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roguelike.Models
{
    // перечисление с типом клеток
    public enum TileType
    {
        Wall,  // Стена (непроходимо)
        Floor, // Пол (проходимо)
        Exit,   // Выход на следующий уровень
        HealthPotion   // Аптечка (восстанавливает HP)
    }
}
