namespace Compass.Helpers;

public static class UssScoring
{
	public static decimal MapLikertToPercentage(int rating)
	{
		// (rating - 1) / 4 * 100
		return Math.Clamp((rating - 1) / 4m * 100m, 0m, 100m);
	}

	public static decimal ComputeUss(IDictionary<string, int?> ratingsByCode, IDictionary<string, int> weights)
	{
		decimal weightedSum = 0m;
		int totalWeights = 0;

		foreach (var kvp in weights)
		{
			var code = kvp.Key;
			var weight = kvp.Value;
			if (ratingsByCode.TryGetValue(code, out var ratingNullable) && ratingNullable.HasValue)
			{
				var pct = MapLikertToPercentage(ratingNullable.Value);
				weightedSum += pct * weight;
				totalWeights += weight;
			}
		}

		if (totalWeights == 0) return 0m;
		return Math.Round(weightedSum / totalWeights, 1, MidpointRounding.AwayFromZero);
	}

	public static string BandFromScore(decimal uss)
	{
		if (uss < 50m) return "Red";
		if (uss < 75m) return "Amber";
		return "Green";
	}
}


