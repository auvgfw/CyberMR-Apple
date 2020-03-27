using UnityEngine;

namespace MVXUnity
{
    [AddComponentMenu("Mvx2/Data Streams/Data Random-access Stream")]
    public class MvxDataRandomAccessStream : MvxDataReaderStream
    {
        #region data

        [SerializeField, HideInInspector] protected uint m_frameId = 0;
        public virtual uint frameId
        {
            get
            {
                return m_frameId;
            }
            set
            {
                if (m_frameId == value)
                    return;

                m_frameId = value;
                if (Application.isPlaying && isActiveAndEnabled)
                    ReadFrame();
            }
        }

        #endregion

        #region stream

        public override void InitializeStream()
        {
            base.InitializeStream();
            if (isOpen)
                ReadFrame();
        }

        #endregion

        #region reader

        [System.NonSerialized] private MVGraphAPI.RandomAccessGraphRunner m_mvxRunner = null;
        [System.NonSerialized] private MVGraphAPI.FrameAccessGraphNode m_frameAccess = null;
        protected override MVGraphAPI.GraphRunner mvxRunner
        {
            get { return m_mvxRunner; }
        }

        protected override bool OpenReader()
        {
            lastReceivedFrame = null;

            try
            {
                m_frameAccess = new MVGraphAPI.FrameAccessGraphNode();

                MVGraphAPI.ManualGraphBuilder graphBuilder = new MVGraphAPI.ManualGraphBuilder();
                graphBuilder = graphBuilder + dataStreamDefinition.GetSourceGraphNode() + new MVGraphAPI.AutoDecompressorGraphNode() + m_frameAccess;
                AddAdditionalGraphTargetsToGraph(graphBuilder);

                m_mvxRunner = new MVGraphAPI.RandomAccessGraphRunner(graphBuilder.CompileGraphAndReset());
                Debug.Log("Mvx2: The stream is open and playing");
                return true;
            }
            catch (System.Exception exception)
            {
                Debug.LogErrorFormat("Failed to create the graph: {0}", exception.Message);
                m_mvxRunner = null;
                return false;
            }
        }

        protected override void DisposeReader()
        {
            if (m_frameAccess != null)
            {
                m_frameAccess.Dispose();
                m_frameAccess = null;
            }

            if (m_mvxRunner == null)
                return;

            m_mvxRunner.Dispose();
            m_mvxRunner = null;
        }

        public override void SeekFrame(uint frameID)
        {
            if (!isOpen)
                return;

            this.frameId = frameID;
        }

        public override void Pause()
        {
            // nothing to do here
        }

        public override void Resume()
        {
            // nothing to do here
        }

        #endregion

        #region frames reading

        protected void ReadFrame()
        {
            if (!m_mvxRunner.ProcessFrame(frameId))
                return;

            lastReceivedFrame = new MVCommon.SharedRef<MVGraphAPI.Frame>(m_frameAccess.GetRecentProcessedFrame());
            if (lastReceivedFrame.sharedObj != null)
                onNextFrameReceived.Invoke(lastReceivedFrame);
        }

        #endregion
    }
}
