using Timberborn.Buildings;
using Timberborn.EntitySystem;
using Timberborn.Reproduction;
using Timberborn.TimeSystem;
using UnityEngine;

namespace Mods.Headcount.Scripts {
  public class BreedingPodSurvey {

    private const float UptimeTimeConstantDays = 3f;
    private const float MaxSampleGapDays = 1f;

    private readonly EntityComponentRegistry _entityComponentRegistry;
    private readonly IDayNightCycle _dayNightCycle;
    private float _lastSampleDay = -1f;

    public int RunningPods { get; private set; }
    public int IdlePods { get; private set; }
    public int PausedPods { get; private set; }
    public int AdultPods { get; private set; }
    public int TotalPods => RunningPods + IdlePods + PausedPods;

    // Births/day if every unpaused pod stays supplied.
    public float PotentialKitBirthsPerDay { get; private set; }
    public float PotentialAdultBirthsPerDay { get; private set; }

    public float KitUptime { get; private set; } = 1f;
    public float AdultUptime { get; private set; } = 1f;
    public float ObservedKitBirthsPerDay => PotentialKitBirthsPerDay * KitUptime;
    public float ObservedAdultBirthsPerDay => PotentialAdultBirthsPerDay * AdultUptime;

    public BreedingPodSurvey(EntityComponentRegistry entityComponentRegistry,
                             IDayNightCycle dayNightCycle) {
      _entityComponentRegistry = entityComponentRegistry;
      _dayNightCycle = dayNightCycle;
    }

    public void Sample() {
      RunningPods = IdlePods = PausedPods = AdultPods = 0;
      float potentialKits = 0f, potentialAdults = 0f, runningKits = 0f, runningAdults = 0f;

      foreach (var pod in _entityComponentRegistry.GetAll<BreedingPod>()) {
        if (!pod.Enabled) {
          continue;
        }
        var spec = pod._breedingPodSpec;
        var daysPerBirth = spec.CycleLengthInDays * spec.CyclesUntilFullyGrown;
        if (daysPerBirth <= 0f) {
          continue;
        }
        var birthsPerDay = 1f / daysPerBirth;
        if (spec.SpawnAdults) {
          AdultPods++;
        }

        var pausable = pod.GetComponent<PausableBuilding>();
        if (pausable && pausable.Paused) {
          PausedPods++;
          continue;
        }
        var running = pod._blockableObject.IsUnblocked && pod._timeTrigger.InProgress;
        if (running) {
          RunningPods++;
        } else {
          IdlePods++;
        }
        if (spec.SpawnAdults) {
          potentialAdults += birthsPerDay;
          runningAdults += running ? birthsPerDay : 0f;
        } else {
          potentialKits += birthsPerDay;
          runningKits += running ? birthsPerDay : 0f;
        }
      }

      PotentialKitBirthsPerDay = potentialKits;
      PotentialAdultBirthsPerDay = potentialAdults;
      UpdateUptime(potentialKits, runningKits, potentialAdults, runningAdults);
    }

    // Uptime is smoothed over game time so short gaps between nutrient deliveries
    // don't make the projection flicker; it pauses with the game.
    private void UpdateUptime(float potentialKits, float runningKits,
                              float potentialAdults, float runningAdults) {
      var today = _dayNightCycle.PartialDayNumber;
      var elapsed = today - _lastSampleDay;
      var reset = _lastSampleDay < 0f || elapsed < 0f || elapsed > MaxSampleGapDays;
      if (!reset && elapsed == 0f) {
        return;
      }
      var weight = reset ? 1f : 1f - Mathf.Exp(-elapsed / UptimeTimeConstantDays);
      KitUptime = Blend(KitUptime, potentialKits, runningKits, weight);
      AdultUptime = Blend(AdultUptime, potentialAdults, runningAdults, weight);
      _lastSampleDay = today;
    }

    private static float Blend(float uptime, float potential, float running, float weight) {
      if (potential <= 0f) {
        return uptime;
      }
      return Mathf.Lerp(uptime, running / potential, weight);
    }

  }
}
