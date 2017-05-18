using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeaponGenerator.Game
{
	static class WeaponTypeExtensions
	{
		/// <summary>
		/// We accepted the fact that not all articles might have values that could be used to determine the range of the weapon. Therefore, we decided instead to impose an average length based on initial testing.
		/// <para>Values are in meters.</para>
		/// </summary>
		public static double GetAverageLength(this WeaponType type)
		{
			switch(type) {
				case WeaponType.Sword:
					return 1;
				case WeaponType.Dagger:
					return 0.5;
				case WeaponType.Axe:
					return 1;
				case WeaponType.Club:
					return 1;
				case WeaponType.Bow:
					return 20;
				case WeaponType.PoleWeapon:
					return 3;
				case WeaponType.Ranged:
					return 10;
				case WeaponType.Siege:
					return 50;
			}

			throw new NotImplementedException("Unsupported weapon type.");
		}

		/// <summary>
		/// Weapons that fit in a certain weapon type may override the default wield type according to the instances found in the article text.
		/// </summary>
		public static bool IsWieldTypeStatic(this WeaponType type)
		{
			switch(type) {
				case WeaponType.Dagger:
				case WeaponType.Bow:
				case WeaponType.PoleWeapon:
					return true;
				case WeaponType.Sword:
				case WeaponType.Axe:
				case WeaponType.Club:
				case WeaponType.Ranged:
				case WeaponType.Siege:
					return false;
			}

			throw new NotImplementedException("Unsupported weapon type.");
		}

		public static WieldType GetDefaultWieldType(this WeaponType type)
		{
			switch(type) {
				case WeaponType.Sword:
					return WieldType.OneHanded;
				case WeaponType.Dagger:
					return WieldType.OneHanded;
				case WeaponType.Axe:
					return WieldType.OneHanded;
				case WeaponType.Club:
					return WieldType.OneHanded;
				case WeaponType.Bow:
					return WieldType.TwoHanded;
				case WeaponType.PoleWeapon:
					return WieldType.TwoHanded;
				case WeaponType.Ranged:
					return WieldType.OneHanded;
				case WeaponType.Siege:
					return WieldType.OneHanded;
			}

			throw new NotImplementedException("Unsupported weapon type.");
		}
	}
}
