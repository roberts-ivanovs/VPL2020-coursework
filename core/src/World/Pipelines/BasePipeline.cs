
using System.Collections.Generic;

namespace DiseaseCore
{
    public class PipelineReturnData
    {
        internal List<EntityOnMap> newHealthy = new List<EntityOnMap>();
        internal List<EntityOnMap> newSick = new List<EntityOnMap>();
    }
    public interface Pipeline
    {
        void updateTimeScale(float timeScale);
        PipelineReturnData pushThrough(List<EntityOnMap> currentSick, List<EntityOnMap> currentHealthy, ulong timeDeltaMs);
    }
}
