using Bindito.Core;

namespace Mods.Headcount.Scripts {
  [Context("Game")]
  public class HeadcountConfigurator : Configurator {

    protected override void Configure() {
      Bind<BreedingPodSurvey>().AsSingleton();
      Bind<ColonyLifeSurvey>().AsSingleton();
      Bind<PopulationPanelProjection>().AsSingleton();
    }

  }
}
