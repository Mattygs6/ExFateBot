namespace ExFateBot
{
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

		private double GetWeight(BattleCharacter battleCharacter)
		{
			var weight = battleCharacter.Distance() * -30 + 1800;

			if (battleCharacter.Pointer == Core.Player.PrimaryTargetPtr)
			{
				weight += 150;
			}

			if (battleCharacter.HasTarget && battleCharacter.CurrentTargetId == Core.Player.ObjectId)
			{
				weight += 50;
			}

			if (ExFateBot.FateData != null && battleCharacter.FateId == ExFateBot.FateData.Id)
			{
				weight += 210;
			}

			if (this.attackers.Contains(battleCharacter))
			{
				weight += 110;
			}

			if (battleCharacter.CurrentTargetId == Core.Player.ObjectId)
			{
				weight += (100f - battleCharacter.CurrentHealthPercent);
			}

			if (!battleCharacter.InCombat)
			{
				weight += 130;
			}

			return weight;
		}

	}
}
