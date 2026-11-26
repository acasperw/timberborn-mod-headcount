using System.Collections.Generic;
using Timberborn.Buildings;
using Timberborn.EntitySystem;
using Timberborn.GameDistricts;
using Timberborn.Reproduction;
using Timberborn.TimeSystem;
using UnityEngine;

namespace Mods.Headcount.Scripts {
  public class BreedingPodSurvey {

    private const float UptimeTimeConstantDays = 3f;
    private const float MaxSampleGapDays = 1f;

    private readonly EntityComponentRegistry _entityComponentRegistry;
    private readonly IDayNightCycle _dayNightCycle;
    private readonly UptimeState _colonyUptime = new();
    private readonly Dictionary<DistrictCenter, UptimeState> _districtUptimes = new();
    private UptimeState _uptime;

    private class UptimeState {
      public float KitUptime = 1f;
      public float AdultUptime = 1f;
      public float LastSampleDay = -1f;
    }

    public int RunningPods { get; private set; }
    public int IdlePods { get; private set; }
    public int PausedPods { get; private set; }
    public int AdultPods { get; private set; }
    public int TotalPods => RunningPods + IdlePods + PausedPods;
    public bool ColonyHasPods { get; private set; }

    // Births/day from unpaused pods if they stay supplied; paused pods never count.
    public float PotentialKitBirthsPerDay { get; private set; }
    public float PotentialAdultBirthsPerDay { get; private set; }

    public float KitUptime => _uptime?.KitUptime ?? 1f;
    public float AdultUptime => _uptime?.AdultUptime ?? 1f;
    public float ObservedKitBirthsPerDay => PotentialKitBirthsPerDay * KitUptime;
    public float ObservedAdultBirthsPerDay => PotentialAdultBirthsPerDay * AdultUptime;

    public BreedingPodSurvey(EntityComponentRegistry entityComponentRegistry,
                             IDayNightCycle dayNightCycle) {
      _entityComponentRegistry = entityComponentRegistry;
      _dayNightCycle = dayNightCycle;
    }

    public void Sample(DistrictCenter district = null) {
      RunningPods = IdlePods = PausedPods = AdultPods = 0;
      ColonyHasPods = false;
      float potentialKits = 0f, potentialAdults = 0f, runningKits = 0f, runningAdults = 0f;

      foreach (var pod in _entityComponentRegistry.GetAll<BreedingPod>()) {
        if (!pod.Enabled) {
          continue;
        }
        ColonyHasPods = true;
        if (district && !IsInDistrict(pod, district)) {
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
      _uptime = GetUptimeState(district);
      UpdateUptime(potentialKits, runningKits, potentialAdults, runningAdults);
    }

    private static bool IsInDistrict(BreedingPod pod, DistrictCenter district) {
      var districtBuilding = pod.GetComponent<DistrictBuilding>();
      return districtBuilding && districtBuilding.District == district;
    }

    private UptimeState GetUptimeState(DistrictCenter district) {
      if (!district) {
        return _colonyUptime;
      }
      if (!_districtUptimes.TryGetValue(district, out var state)) {
        state = new UptimeState();
        _districtUptimes[district] = state;
      }
      return state;
    }

    // Uptime is smoothed over game time so short gaps between nutrient deliveries
    // don't make the projection flicker; it pauses with the game.
    private void UpdateUptime(float potentialKits, float runningKits,
                              float potentialAdults, float runningAdults) {
      var today = _dayNightCycle.PartialDayNumber;
      var elapsed = today - _uptime.LastSampleDay;
      var reset = _uptime.LastSampleDay < 0f || elapsed < 0f || elapsed > MaxSampleGapDays;
      if (!reset && elapsed == 0f) {
        return;
      }
      var weight = reset ? 1f : 1f - Mathf.Exp(-elapsed / UptimeTimeConstantDays);
      _uptime.KitUptime = Blend(_uptime.KitUptime, potentialKits, runningKits, weight);
      _uptime.AdultUptime = Blend(_uptime.AdultUptime, potentialAdults, runningAdults, weight);
      _uptime.LastSampleDay = today;
    }

    private static float Blend(float uptime, float potential, float running, float weight) {
      if (potential <= 0f) {
        return uptime;
      }
      return Mathf.Lerp(uptime, running / potential, weight);
    }

  }
}
