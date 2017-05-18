using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeaponGenerator.Game
{
	/// <summary>
	/// This is the final class that this program is trying to achieve: a weapon with all its properties set.
	/// </summary>
	public class GameWeapon
	{
		public string Name;
		public WeaponType Type;
		public double Length;
		public int Attack;
		public int Defense;
		public WieldType WieldType;
		public int Price;

		public override string ToString()
		{
			return $"[{Type}] {Name}, {WieldType}, L({Length}), A({Attack}), D({Defense})";
		}
	}
}
