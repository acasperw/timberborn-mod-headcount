using Timberborn.Beavers;
using Timberborn.BonusSystem;
using Timberborn.Common;
using Timberborn.LifeSystem;

namespace Mods.Headcount.Scripts {
  public readonly struct ColonyLife {

    public float AverageLifespanDays { get; }
    public float DaysOfChildhood { get; }
    public float MeanLongevity { get; }
    public float AdultLifeMultiplier { get; }
    public float KitLifeMultiplier { get; }
    public float KitGrowthMultiplier { get; }

    public ColonyLife(float averageLifespanDays, float daysOfChildhood, float meanLongevity,
                      float adultLifeMultiplier, float kitLifeMultiplier, float kitGrowthMultiplier) {
      AverageLifespanDays = averageLifespanDays;
      DaysOfChildhood = daysOfChildhood;
      MeanLongevity = meanLongevity;
      AdultLifeMultiplier = adultLifeMultiplier;
      KitLifeMultiplier = kitLifeMultiplier;
      KitGrowthMultiplier = kitGrowthMultiplier;
    }

  }

  // Averages the bonus multipliers the game actually applies to each living beaver, so
  // well-being tiers, difficulty and other mods are all reflected without hardcoding tiers.
  public class ColonyLifeSurvey {

    private static readonly float MeanLongevity =
        (BeaverLongevity.MinExpectedLongevity + BeaverLongevity.MaxExpectedLongevity) / 2f;

    private readonly LifeService _lifeService;
    private readonly BeaverPopulation _beaverPopulation;

    public ColonyLifeSurvey(LifeService lifeService, BeaverPopulation beaverPopulation) {
      _lifeService = lifeService;
      _beaverPopulation = beaverPopulation;
    }

    public ColonyLife Survey(bool includeKitBonuses = true) {
      var beavers = _beaverPopulation._beaverCollection;
      var (kitLifeMultiplier, kitGrowthMultiplier) = includeKitBonuses
          ? AverageKitMultipliers(beavers.Children)
          : (1f, 1f);
      return new ColonyLife(_lifeService._averageLifespan,
                            _lifeService._daysOfChildhood,
                            MeanLongevity,
                            AverageMultiplier(beavers.Adults, LifeProgressor.LifeExpectancyBonusId),
                            kitLifeMultiplier,
                            kitGrowthMultiplier);
    }

    private static (float life, float growth) AverageKitMultipliers(ReadOnlyList<Beaver> kits) {
      var lifeSum = 0f;
      var growthSum = 0f;
      var lifeCount = 0;
      var growthCount = 0;
      foreach (var kit in kits) {
        var bonusManager = kit.GetComponent<BonusManager>();
        if (!bonusManager) {
          continue;
        }
        if (bonusManager._bonuses.TryGetValue(LifeProgressor.LifeExpectancyBonusId, out var lifeBonus)) {
          lifeSum += lifeBonus.ClampedValue;
          lifeCount++;
        }
        if (bonusManager._bonuses.TryGetValue(Child.BonusId, out var growthBonus)) {
          growthSum += growthBonus.ClampedValue;
          growthCount++;
        }
      }
      return (lifeCount > 0 ? lifeSum / lifeCount : 1f,
              growthCount > 0 ? growthSum / growthCount : 1f);
    }

    private static float AverageMultiplier(ReadOnlyList<Beaver> beavers, string bonusId) {
      var sum = 0f;
      var count = 0;
      foreach (var beaver in beavers) {
        var bonusManager = beaver.GetComponent<BonusManager>();
        if (bonusManager && bonusManager._bonuses.TryGetValue(bonusId, out var bonus)) {
          sum += bonus.ClampedValue;
          count++;
        }
      }
      return count > 0 ? sum / count : 1f;
    }

  }
}
