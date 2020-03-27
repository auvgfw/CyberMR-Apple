using UnityEngine;

namespace MVXUnity
{
    [AddComponentMenu("Mvx2/Data Streams/Simple Data Stream")]
    public class MvxSimpleDataStream : MvxDataReaderStream
    {
        #region data

        [SerializeField, HideInInspector] private MVGraphAPI.RunnerPlaybackMode m_playbackMode;
        public MVGraphAPI.RunnerPlaybackMode playbackMode
        {
            get { return m_playbackMode; }
            set
            {
                if (m_playbackMode == value)
                    return;
                m_playbackMode = value;

                if (Application.isPlaying && isActiveAndEnabled && isOpen)
                {
                    m_mvxRunner.Stop();
                    m_mvxRunner.Play(m_playbackMode);
                }
            }
        }

        [SerializeField, HideInInspector] private bool m_followStreamFPS = true;
        public bool followStreamFPS
        {
            get { return m_followStreamFPS; }
            set
            {
                if (m_followStreamFPS == value)
                    return;
                m_followStreamFPS = value;

                if (Application.isPlaying && isActiveAndEnabled && isOpen)
                    m_fpsBlocker.SetFPS(m_followStreamFPS ? MVGraphAPI.BlockFPSGraphNode.FPS_FROM_SOURCE : MVGraphAPI.BlockFPSGraphNode.FPS_MAX);
            }
        }

        #endregion

        #region reader

        [System.NonSerialized] private MVGraphAPI.AutoSequentialGraphRunner m_mvxRunner = null;
        [System.NonSerialized] private MVGraphAPI.BlockFPSGraphNode m_fpsBlocker = null;
        [System.NonSerialized] private MVGraphAPI.AsyncFrameAccessGraphNode m_frameAccess = null;

        protected override MVGraphAPI.GraphRunner mvxRunner
        {
            get { return m_mvxRunner; }
        }

        protected override bool OpenReader()
        {
            lastReceivedFrame = null;
            m_paused = false;

            try
            {
                m_frameAccess = new MVGraphAPI.AsyncFrameAccessGraphNode(new MVGraphAPI.DelegatedFrameListener(HandleNextFrame));
                m_fpsBlocker = new MVGraphAPI.BlockFPSGraphNode(3, m_followStreamFPS ? MVGraphAPI.BlockFPSGraphNode.FPS_FROM_SOURCE : MVGraphAPI.BlockFPSGraphNode.FPS_MAX, MVGraphAPI.BlockGraphNode.FullBehaviour.FB_BLOCK_FRAMES);

                MVGraphAPI.ManualGraphBuilder graphBuilder = new MVGraphAPI.ManualGraphBuilder();
                graphBuilder = graphBuilder
                    + dataStreamDefinition.GetSourceGraphNode()
                    + new MVGraphAPI.AutoDecompressorGraphNode()
                    + m_fpsBlocker
                    + m_frameAccess;
                AddAdditionalGraphTargetsToGraph(graphBuilder);

                m_mvxRunner = new MVGraphAPI.AutoSequentialGraphRunner(graphBuilder.CompileGraphAndReset());

                if (!m_mvxRunner.Play(m_playbackMode))
                {
                    Debug.LogError("Mvx2: Failed to play source");
                    return false;
                }

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

            m_mvxRunner.Stop();
            m_mvxRunner.Dispose();
            m_mvxRunner = null;
        }

        public override void SeekFrame(uint frameID)
        {
            if (!isOpen)
                return;

            m_mvxRunner.SeekFrame(frameID);
        }

        private bool m_paused = false;

        public override void Pause()
        {
            if (!isOpen)
                return;

            m_paused = true;
            m_mvxRunner.Pause();
        }

        public override void Resume()
        {
            if (!isOpen)
                return;

            m_paused = false;
            if (Application.isPlaying && isActiveAndEnabled)
                m_mvxRunner.Resume();
        }

        #endregion

        #region frames handling

        protected void HandleNextFrame(MVGraphAPI.Frame nextFrame)
        {
            lastReceivedFrame = new MVCommon.SharedRef<MVGraphAPI.Frame>(nextFrame);
            onNextFrameReceived.Invoke(lastReceivedFrame);
        }

        #endregion

        #region MonoBehaviour

        public void OnEnable()
        {
            if (Application.isPlaying && isActiveAndEnabled && isOpen && !m_paused)
                m_mvxRunner.Resume();
        }

        public void OnDisable()
        {
            if (Application.isPlaying && isOpen)
                m_mvxRunner.Pause();
        }

        #endregion
    }
}