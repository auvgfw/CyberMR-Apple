using UnityEngine;
using UnityEngine.Events;

namespace MVXUnity
{
    [AddComponentMenu("Mvx2/Targets/Frame Access Target")]
    public class MvxFrameAccessTarget : MvxTarget
    {
        #region data

        [System.Serializable] public class NextFrameReceivedEvent : UnityEvent<MVCommon.SharedRef<MVGraphAPI.Frame>> { }
        [SerializeField] public NextFrameReceivedEvent onNextFrameReceived = new NextFrameReceivedEvent();

        private MVGraphAPI.AsyncFrameAccessGraphNode m_asyncFrameAccessGraphTarget = null;

        private void OnMvxFrame(MVGraphAPI.Frame frame)
        {
            onNextFrameReceived.Invoke(new MVCommon.SharedRef<MVGraphAPI.Frame>(frame));
        }

        #endregion

        #region graph targets

        public override MVGraphAPI.GraphNode GetGraphNode()
        {
            if (m_asyncFrameAccessGraphTarget == null)
            {
                m_asyncFrameAccessGraphTarget = new MVGraphAPI.AsyncFrameAccessGraphNode(new MVGraphAPI.DelegatedFrameListener(OnMvxFrame));
            }

            return m_asyncFrameAccessGraphTarget;
        }

        #endregion
    }
}
