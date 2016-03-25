namespace ExFateBot
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using ff14bot;
	using ff14bot.Helpers;
	using ff14bot.Managers;
	using ff14bot.NeoProfiles;
	using ff14bot.Objects;

	public struct BattleCharacterWeight
	{
		public BattleCharacter BattleCharacter;

		public double Weight;
	}

	public class ExFateCombatTargetingProvider : ITargetingProvider
	{
		public HashSet<uint> IgnoreNpcIds;

		private BattleCharacter[] attackers;

		public ExFateCombatTargetingProvider()
		{
			this.IgnoreNpcIds = new HashSet<uint> { 1201 };
		}

		public List<BattleCharacter> GetObjectsByWeight()
		{
			var allBattleCharacters = GameObjectManager.GetObjectsOfType<BattleCharacter>().ToArray<BattleCharacter>();
			attackers = GameObjectManager.Attackers.ToArray();
			var inCombat = Core.Player.InCombat;

			var battleChars  = allBattleCharacters.Where(bc => Filter(inCombat, bc)).OrderByDescending(GetWeight).ToList();

			return battleChars;
		}

		private bool Filter(bool inCombat, BattleCharacter battleCharacter)
		{
			if (!battleCharacter.IsValid || battleCharacter.IsDead || !battleCharacter.IsVisible || battleCharacter.CurrentHealthPercent <= 0f)
			{
				return false;
			}

			if (!battleCharacter.CanAttack)
			{
				return false;
			}

			if (this.IgnoreNpcIds.Contains(battleCharacter.NpcId))
			{
				return false;
			}

			if (Blacklist.Contains(battleCharacter.ObjectId, BlacklistFlags.Combat))
			{
				return false;
			}

			if (battleCharacter.IsFateGone)
			{
				return false;
			}

			if (Core.Player.IsLevelSynced && IsOutOfRangeOfLevelSyncedFate(battleCharacter))
			{
				return false;
			}

			if (this.attackers.Contains(battleCharacter))
			{
				return true;
			}

			if (!battleCharacter.IsFate && ExFateBot.FateData != null)
			{
				return false;
			}

			if (ExFateBot.FateData != null && battleCharacter.FateId != ExFateBot.FateData.Id)
			{
				return false;
			}

			if ((ExFateBot.FateData == null || !ExFateBot.FateData.IsValid))
			{
				return false;
			}

			return !inCombat;
		}

		private bool IsOutOfRangeOfLevelSyncedFate(BattleCharacter battleCharacter)
		{
			if (ExFateBot.FateData == null)
			{
				return false;
			}

			// return true if distance from middle of fate to enemy is greater than
			var isOutOfRange = ExFateBot.FateData.Location.Distance(battleCharacter.Location) 
			> (ExFateBot.FateData.Radius * 0.8f) // 80% of the fate radius plus
			+ RoutineManager.Current.PullRange // player pull range from combat routine
			+ battleCharacter.CombatReach; // distance(radius) from enemy in which your abilities calculate their distance.

			return isOutOfRange;
		}

		private double GetWeight(BattleCharacter battleCharacter)
		{
			// Math.Max(battleCharacter.Distance() - battleCharacter.CombatReach, 0f) prevents enemies from walking on top of us becoming a lot more important than just using Distance();
			// Weight by distance [0 = 2000, 50 = 0];
			var weight = Math.Max(battleCharacter.Distance() - battleCharacter.CombatReach, 0f)  * -40 + 2000;

			// we are targetting the enemy, add 150
			if (battleCharacter.Pointer == Core.Player.PrimaryTargetPtr)
			{
				weight += 150;
			}

			// the enemy is targetting us, add 50, add (100 - %hp) times 2
			if (battleCharacter.HasTarget && battleCharacter.CurrentTargetId == Core.Player.ObjectId)
			{
				weight += 50 + (100f - battleCharacter.CurrentHealthPercent) * 2;
			}

			// enemy is part of our fate, add 190
			if (ExFateBot.FateData != null && battleCharacter.FateId == ExFateBot.FateData.Id)
			{
				weight += 190;
			}

			// enemy is on our attackers list, add 60
			if (this.attackers.Contains(battleCharacter))
			{
				weight += 60;
			}

			// enemy is not in combat, subtract 80
			if (!battleCharacter.InCombat)
			{
				weight -= 80;
			}

			return weight;
		}

	}
}
