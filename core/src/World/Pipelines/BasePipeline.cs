
using System.Collections.Generic;

namespace DiseaseCore
{
    class PipelineReturnData
    {
        internal List<EntityOnMap> newHealthy = new List<EntityOnMap>();
        internal List<EntityOnMap> newSick = new List<EntityOnMap>();
    }
    interface Pipeline
    {
        PipelineReturnData pushThrough(List<EntityOnMap> currentSick, List<EntityOnMap> currentHealthy);
    }
}
