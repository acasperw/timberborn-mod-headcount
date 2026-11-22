using UnityEngine;

namespace Mods.Headcount.Scripts {
  // Steady state: population = birth rate × time each beaver stays in that group.
  // Mirrors the game's life model: a kit's life clock runs during childhood, so kit-born
  // adults get the remainder; Advanced Breeding Pod adults start at life progress 0.
  public readonly struct SteadyStatePopulation {

    public float Kits { get; }
    public float Adults { get; }
    public float Total => Kits + Adults;

    private SteadyStatePopulation(float kits, float adults) {
      Kits = kits;
      Adults = adults;
    }

    public static SteadyStatePopulation Calculate(float kitBirthsPerDay, float adultBirthsPerDay,
                                                  ColonyLife life) {
      var fullLifeDays = life.MeanLongevity * life.AverageLifespanDays;
      var kitLifeDays = fullLifeDays * life.KitLifeMultiplier;
      var kitDays = Mathf.Min(life.DaysOfChildhood / Mathf.Max(life.KitGrowthMultiplier, 0.01f),
                              kitLifeDays);
      var lifeUsedAsKit = kitLifeDays > 0f ? kitDays / kitLifeDays : 1f;
      var adultLifespanDays = fullLifeDays * life.AdultLifeMultiplier;
      var kitBornAdultDays = Mathf.Max(0f, 1f - lifeUsedAsKit) * adultLifespanDays;

      var kits = kitBirthsPerDay * kitDays;
      var adults = kitBirthsPerDay * kitBornAdultDays + adultBirthsPerDay * adultLifespanDays;
      return new SteadyStatePopulation(kits, adults);
    }

  }
}
