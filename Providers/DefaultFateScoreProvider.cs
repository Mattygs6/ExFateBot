namespace ExFateBot
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using ff14bot;
	using ff14bot.Managers;

	public class DefaultFateScoreProvider 
	{
		private static readonly Dictionary<FateIconType, float> BaseFateScores = new Dictionary<FateIconType, float>
		{
			{FateIconType.Boss, 0.5f},
			{FateIconType.Battle, 1.0f},
			{FateIconType.KillHandIn, 10.0f}, // until I figure out picking up / handing in
			{FateIconType.ProtectNPC, 1.5f},
			{FateIconType.ProtectNPC2, 1.5f}
		};

		public IList<FateData> GetObjectsByWeight(IEnumerable<FateData> fates)
		{
			var fateData = fates.OrderByDescending(GetWeight).ToList();

			return fateData;
		}

		private static double GetWeight(FateData fateData)
		{
			float baseScore;
			if (!BaseFateScores.TryGetValue(fateData.Icon, out baseScore))
			{
				baseScore = 1.0f;
			}

			var power = WorldManager.CanFly ? 2 : 3;
			var score = 100000
						- baseScore * (Math.Pow(fateData.Progress, power) + fateData.Location.Distance(Core.Me.Location) * 150);

			return score;
		}
	}
}
