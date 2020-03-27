using UnityEngine;

namespace MVXUnity
{
    [AddComponentMenu("Mvx2/Targets/File Target")]
    public class MvxFileTarget : MvxTarget
    {
        #region data

        [SerializeField] public string absoluteFilePath;

        #endregion

        #region graph targets

        public override MVGraphAPI.GraphNode GetGraphNode()
        {
            return new MVGraphAPI.Mvx2FileWriterGraphNode(absoluteFilePath);
        }

        #endregion
    }
}
