using UnityEngine;

namespace MVXUnity
{
    public abstract class MvxTarget : MonoBehaviour
    {
        #region graph targets

        public abstract MVGraphAPI.GraphNode GetGraphNode();

        public static MVGraphAPI.GraphNode[] TransformMvxTargetsToGraphNodes(MvxTarget[] targets)
        {
            if (targets == null)
                return null;

            MVGraphAPI.GraphNode[] graphTargets = new MVGraphAPI.GraphNode[targets.Length];
            for (int i = 0; i < targets.Length; i++)
                graphTargets[i] = targets[i].GetGraphNode();

            return graphTargets;
        }

        #endregion
    }
}
