
using System.Collections.Generic;

namespace DiseaseCore
{
    public class PipelineReturnData
    {
        internal List<EntityOnMap<HealthyEntity>> newHealthy = new List<EntityOnMap<HealthyEntity>>();
        internal List<EntityOnMap<SickEntity>> newSick = new List<EntityOnMap<SickEntity>>();
    }
    public interface Pipeline
    {
        void updateTimeScale(float timeScale);
        PipelineReturnData pushThrough(
            List<EntityOnMap<SickEntity>> currentSick,
            List<EntityOnMap<HealthyEntity>> currentHealthy,
            ulong timeDeltaMs
        );
    }
}
