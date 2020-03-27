using UnityEngine;

namespace MVXUnity
{
    [AddComponentMenu("Mvx2/Targets/Auto Compressor Target")]
    public class MvxAutoCompressorTarget : MvxTarget
    {
        #region data

        [SerializeField] public bool dropOriginalUncompressedData = true;

        #endregion

        #region graph targets

        public override MVGraphAPI.GraphNode GetGraphNode()
        {
            return new MVGraphAPI.AutoCompressorGraphNode(dropOriginalUncompressedData);
        }

        #endregion
    }
}
