using System.Collections.Generic;
using Timberborn.Beavers;
using Timberborn.CoreUI;
using Timberborn.GameDistricts;
using Timberborn.Localization;
using Timberborn.Population;
using Timberborn.PopulationUI;
using Timberborn.Reproduction;
using Timberborn.SingletonSystem;
using Timberborn.TooltipSystem;
using Timberborn.UILayoutSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mods.Headcount.Scripts {
  // Adds the expected population to the end of the game's top-left population row:
  // a steady-state estimate for breeding pods, or the bed count for housing breeders.
  public class PopulationPanelProjection : ILoadableSingleton, IUpdatableSingleton {

    private const float RefreshIntervalSeconds = 1f;

    private readonly EventBus _eventBus;
    private readonly PopulationPanel _populationPanel;
    private readonly DistrictContextService _districtContextService;
    private readonly BeaverPopulation _beaverPopulation;
    private readonly BreedingPodSurvey _podSurvey;
    private readonly ColonyLifeSurvey _lifeSurvey;
    private readonly ITooltipRegistrar _tooltipRegistrar;
    private readonly ILoc _loc;
    private Label _projectionLabel;
    private SteadyStatePopulation _atCurrentUptime;
    private float _nextRefreshTime;
    private Mode _mode;
    private bool _visible;
    private bool _tooltipDirty = true;
    private string _tooltip;
    private int _tooltipAdults;
    private int _tooltipChildren;
    private BedData _tooltipBeds;

    private enum Mode { None, BreedingPods, Housing }

    public PopulationPanelProjection(EventBus eventBus,
                                     PopulationPanel populationPanel,
                                     DistrictContextService districtContextService,
                                     BeaverPopulation beaverPopulation,
                                     BreedingPodSurvey podSurvey,
                                     ColonyLifeSurvey lifeSurvey,
                                     ITooltipRegistrar tooltipRegistrar,
                                     ILoc loc) {
      _eventBus = eventBus;
      _populationPanel = populationPanel;
      _districtContextService = districtContextService;
      _beaverPopulation = beaverPopulation;
      _podSurvey = podSurvey;
      _lifeSurvey = lifeSurvey;
      _tooltipRegistrar = tooltipRegistrar;
      _loc = loc;
    }

    public void Load() {
      _projectionLabel = new Label { name = "HeadcountProjection" };
      _projectionLabel.style.marginLeft = 6;
      _projectionLabel.ToggleDisplayStyle(false);
      _tooltipRegistrar.RegisterUpdatable(_projectionLabel, GetTooltip);
      _eventBus.Register(this);
    }

    [OnEvent]
    public void OnShowPrimaryUI(ShowPrimaryUIEvent showPrimaryUIEvent) {
      if (_projectionLabel.parent == null) {
        AttachToPopulationRow();
      }
      Refresh();
    }

    public void UpdateSingleton() {
      if (Time.unscaledTime < _nextRefreshTime) {
        return;
      }
      _nextRefreshTime = Time.unscaledTime + RefreshIntervalSeconds;
      Refresh();
    }

    private void AttachToPopulationRow() {
      var row = _populationPanel._populationDataRow;
      var host = row?._contaminatedCount?.parent
                 ?? _populationPanel._root?.Q<Button>("PopulationData");
      if (host == null) {
        Debug.LogWarning("[Headcount] Population panel not found; projection hidden.");
        return;
      }
      if (row?._adultCount != null) {
        foreach (var styleClass in row._adultCount.GetClasses()) {
          _projectionLabel.AddToClassList(styleClass);
        }
      }
      host.Add(_projectionLabel);
    }

    private void Refresh() {
      if (_projectionLabel.parent == null) {
        return;
      }
      _tooltipDirty = true;
      _podSurvey.Sample();
      _mode = _podSurvey.TotalPods > 0 ? Mode.BreedingPods
          : BreedsInHouses() ? Mode.Housing
          : Mode.None;
      // Pod projections are colony-wide, so they hide while the panel shows a district.
      var visible = _mode == Mode.Housing
                    || (_mode == Mode.BreedingPods && !_districtContextService.SelectedDistrict);
      if (_visible != visible) {
        _projectionLabel.ToggleDisplayStyle(visible);
        _visible = visible;
      }
      if (!visible) {
        return;
      }
      if (_mode == Mode.Housing) {
        _projectionLabel.text = _loc.T("Headcount.InlineHousing", TotalBeds(CurrentBedData()));
        return;
      }
      var hasKitBirths = _podSurvey.PotentialKitBirthsPerDay > 0f;
      if (hasKitBirths || _podSurvey.PotentialAdultBirthsPerDay > 0f) {
        var life = _lifeSurvey.Survey(includeKitBonuses: hasKitBirths);
        _atCurrentUptime = SteadyStatePopulation.Calculate(_podSurvey.ObservedKitBirthsPerDay,
                                                           _podSurvey.ObservedAdultBirthsPerDay, life);
      } else {
        _atCurrentUptime = default;
      }
      _projectionLabel.text = _loc.T("Headcount.Inline", Round(_atCurrentUptime.Total));
    }

    // Housing breeders (Folktails) have Procreator on adults; pod breeders don't.
    private bool BreedsInHouses() {
      var adults = _beaverPopulation._beaverCollection.Adults;
      return adults.Count > 0 && adults[0].HasComponent<Procreator>();
    }

    private BedData CurrentBedData() => _populationPanel.GetContextualPopulationData().BedData;

    private static int TotalBeds(BedData beds) => beds.OccupiedBeds + beds.FreeBeds;

    private string GetTooltip() {
      if (_mode == Mode.Housing) {
        var data = _populationPanel.GetContextualPopulationData();
        return GetCachedTooltip(data.NumberOfAdults, data.NumberOfChildren, data.BedData);
      }
      if (_mode != Mode.BreedingPods) {
        return "";
      }
      return GetCachedTooltip(_beaverPopulation.NumberOfAdults, _beaverPopulation.NumberOfChildren, default);
    }

    private string GetCachedTooltip(int adults, int children, BedData beds) {
      if (_tooltipDirty || _tooltipAdults != adults || _tooltipChildren != children
          || (_mode == Mode.Housing && !_tooltipBeds.Equals(beds))) {
        _tooltip = _mode == Mode.Housing
            ? GetHousingTooltip(adults, children, beds)
            : GetPodTooltip(adults, children);
        _tooltipAdults = adults;
        _tooltipChildren = children;
        _tooltipBeds = beds;
        _tooltipDirty = false;
      }
      return _tooltip;
    }

    private string GetPodTooltip(int adults, int children) {
      var lines = new List<string> {
          _loc.T("Headcount.TooltipTitle", Round(_atCurrentUptime.Total)),
          _loc.T("Headcount.TooltipSplit", Round(_atCurrentUptime.Adults), Round(_atCurrentUptime.Kits)),
          _loc.T("Headcount.TooltipNow", adults, children),
          "",
          _loc.T("Headcount.TooltipPods", _podSurvey.RunningPods, _podSurvey.IdlePods,
                 _podSurvey.PausedPods)
      };
      if (_podSurvey.AdultPods > 0) {
        lines.Add(_loc.T("Headcount.TooltipAdultPods", _podSurvey.AdultPods));
      }
      return string.Join("\n", lines);
    }

    private string GetHousingTooltip(int adults, int children, BedData beds) {
      var lines = new List<string> {
          _loc.T("Headcount.TooltipHousingTitle", TotalBeds(beds)),
          _loc.T("Headcount.TooltipNow", adults, children),
          _loc.T("Headcount.TooltipBeds", beds.OccupiedBeds, beds.FreeBeds)
      };
      if (beds.Homeless > 0) {
        lines.Add(_loc.T("Headcount.TooltipHomeless", beds.Homeless));
      }
      lines.Add("");
      lines.Add(_loc.T("Headcount.TooltipHousingRule"));
      return string.Join("\n", lines);
    }

    private static int Round(float value) => Mathf.RoundToInt(value);

  }
}
