using UnityEngine;
using UnityEngine.Events;

namespace MVXUnity
{
    public abstract class MvxDataStream : MonoBehaviour
    {
        #region data

        [SerializeField, HideInInspector] private MvxDataStreamDefinition m_dataStreamDefinition;
        public virtual MvxDataStreamDefinition dataStreamDefinition
        {
            get { return m_dataStreamDefinition; }
            set
            {
                if (m_dataStreamDefinition == value)
                    return;

                if (m_dataStreamDefinition != null)
                    m_dataStreamDefinition.onDefinitionChanged.RemoveListener(TryRestartStream);

                m_dataStreamDefinition = value;

                if (m_dataStreamDefinition != null)
                    m_dataStreamDefinition.onDefinitionChanged.AddListener(TryRestartStream);

                if (Application.isPlaying && isActiveAndEnabled)
                    RestartStream();
            }
        }

        [SerializeField] public MvxTarget[] additionalTargets = null;
        protected void AddAdditionalGraphTargetsToGraph(MVGraphAPI.ManualGraphBuilder graphBuilder)
        {
            if (additionalTargets == null)
                return;

            foreach (MvxTarget additionalTarget in additionalTargets)
                graphBuilder = graphBuilder + additionalTarget.GetGraphNode();
        }

        public abstract MVGraphAPI.SourceInfo mvxSourceInfo
        {
            get;
        }

        public abstract bool isSingleFrameSource
        {
            get;
        }

        public abstract bool isOpen
        {
            get;
        }

        private MVCommon.SharedRef<MVGraphAPI.Frame> m_lastReceivedFrame;
        public MVCommon.SharedRef<MVGraphAPI.Frame> lastReceivedFrame
        {
            get { return m_lastReceivedFrame; }
            protected set
            {
                if (m_lastReceivedFrame != null)
                    m_lastReceivedFrame.Dispose();

                m_lastReceivedFrame = value;
            }
        }

        #endregion

        #region frames handling

        [System.Serializable] public class StreamOpenEvent : UnityEvent<MVGraphAPI.SourceInfo> { }
        [SerializeField, HideInInspector] public StreamOpenEvent onStreamOpen = new StreamOpenEvent();
        
        [System.Serializable] public class NextFrameReceivedEvent : UnityEvent<MVCommon.SharedRef<MVGraphAPI.Frame>> { }
        [SerializeField, HideInInspector] public NextFrameReceivedEvent onNextFrameReceived = new NextFrameReceivedEvent();

        #endregion

        #region stream

        private void TryRestartStream()
        {
            if (Application.isPlaying && isActiveAndEnabled)
                RestartStream();
        }

        public void RestartStream()
        {
            DisposeStream();
            InitializeStream();
        }

        public abstract void InitializeStream();
        public abstract void DisposeStream();

        public abstract void SeekFrame(uint frameID);

        public abstract void Pause();
        public abstract void Resume();

        #endregion

        #region MonoBehaviour

        public virtual void Awake()
        {
            MvxPluginsLoader.LoadPlugins();
        }

        public virtual void Start()
        {
            if (m_dataStreamDefinition != null)
                m_dataStreamDefinition.onDefinitionChanged.AddListener(RestartStream);

            InitializeStream();
        }

        public virtual void OnDestroy()
        {
            DisposeStream();
        }

        public virtual void Update()
        {
            if (!isOpen)
                return;

            if (isSingleFrameSource && lastReceivedFrame != null)
                DisposeStream();
        }

        #endregion
    }
}