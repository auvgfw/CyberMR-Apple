using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

namespace MVXUnity
{
    /// <summary> Player of standard Windows PCM format audio. </summary>
    /// <remarks> 1-byte per sample -> unsigned; 2-bytes per sample -> signed, 4-bytes per sample -> signed</remarks>
    [AddComponentMenu("Mvx2/Data Processors/Audio Async Player")]
    public class MvxAudioAsyncPlayer : MvxAsyncDataProcessor
    {
        #region data 

        private const int AUDIO_CHUNKS_BUFFER_SIZE = 10;
        private MvxAudioPlayer m_audioPlayer = new MvxAudioPlayer(AUDIO_CHUNKS_BUFFER_SIZE);

        private int m_outputSampleRate;

        #endregion

        #region MonoBehaviour

        public void Awake()
        {
            m_audioPlayer.onAudioChunkDiscarded.AddListener(OnAudioChunkDiscarded);

            gameObject.AddComponent<AudioSource>();
            // AudioSettings.outputSampleRate can not be called from audio thread
            m_outputSampleRate = AudioSettings.outputSampleRate;
        }

        public override void OnDestroy()
        {
            StopAudioDataProcessingThread();
            m_audioPlayer.Reset();
            m_audioPlayer.onAudioChunkDiscarded.RemoveListener(OnAudioChunkDiscarded);
        }

        public void OnEnable()
        {
            if (Application.isPlaying && m_framesQueue.Count > 0)
                StartAudioDataProcessingThread();
        }

        public void OnDisable()
        {
            StopAudioDataProcessingThread();
        }

        public override void Update()
        {
            m_outputSampleRate = AudioSettings.outputSampleRate;

            base.Update();
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            lock(m_audioPlayer)
                m_audioPlayer.DequeueAudioData(data, channels, m_outputSampleRate);
        }

        #endregion

        #region process frame

        protected override bool CanProcessStream(MVGraphAPI.SourceInfo sourceInfo)
        {
            bool streamSupported = sourceInfo.ContainsDataLayer(MVGraphAPI.SimpleDataLayersGuids.AUDIO_DATA_LAYER);
            Debug.LogFormat("Mvx2: AudioAsyncPlayer {0} the new mvx stream", streamSupported ? "supports" : "does not support");
            return streamSupported;
        }

        protected override void ResetProcessedData()
        {
            StopAudioDataProcessingThread();
            m_audioPlayer.Reset();
        }

        protected override void ProcessNextFrame(MVGraphAPI.Frame frame)
        {
            if (!frame.StreamContainsDataLayer(MVGraphAPI.SimpleDataLayersGuids.AUDIO_DATA_LAYER))
                return;

            lock (m_framesQueue)
                m_framesQueue.Enqueue(frame);

            StartAudioDataProcessingThread();
        }

        private Queue<MVGraphAPI.Frame> m_framesQueue = new Queue<MVGraphAPI.Frame>();

        private bool m_stopProcessingAudioData = false;
        /// <summary> A thread for processing audio data of MVX frames. </summary>
        /// <remarks> The thread is necessary to relieve the execution time of Update(). </remarks>
        private Thread m_audioDataProcessingThread;

        private void StartAudioDataProcessingThread()
        {
            if (m_audioDataProcessingThread == null || !m_audioDataProcessingThread.IsAlive)
            {
                m_stopProcessingAudioData = false;
                m_audioDataProcessingThread = new Thread(new ThreadStart(ProcessAudioData));
                m_audioDataProcessingThread.Start();
            }
        }

        private void StopAudioDataProcessingThread()
        {
            if (m_audioDataProcessingThread != null)
            {
                m_stopProcessingAudioData = true;
                m_audioDataProcessingThread.Join();
                m_audioDataProcessingThread = null;
            }
        }

        private void ProcessAudioData()
        {
            while (true)
            {
                if (m_stopProcessingAudioData)
                    return;

                MVGraphAPI.Frame frame = null;

                lock(m_framesQueue)
                {
                    if (m_framesQueue.Count == 0)
                        return;

                    frame = m_framesQueue.Dequeue();
                }

                UInt32 framePCMDataSize = MVGraphAPI.FrameAudioExtractor.GetPCMDataSize(frame);
                if (framePCMDataSize == 0)
                    continue;

                UInt32 frameChannelsCount;
                UInt32 frameBitsPerSample;
                UInt32 frameSampleRate;
                MVGraphAPI.FrameAudioExtractor.GetAudioSamplingInfo(frame, out frameChannelsCount, out frameBitsPerSample, out frameSampleRate);
                if (frameBitsPerSample != 8 && frameBitsPerSample != 16 && frameBitsPerSample != 32)
                {
                    Debug.LogErrorFormat("Unsupported 'bits per sample' value {0}", frameBitsPerSample);
                    continue;
                }
                UInt32 frameBytesPerSample = frameBitsPerSample / 8;

                byte[] frameAudioBytes = new byte[framePCMDataSize];
                MVGraphAPI.FrameAudioExtractor.CopyPCMData(frame, frameAudioBytes);

                MvxAudioChunk newAudioChunk = MvxAudioChunksPool.instance.AllocateAudioChunk(frameAudioBytes, frameBytesPerSample, frameChannelsCount, frameSampleRate);
                lock (m_audioPlayer)
                    m_audioPlayer.EnqueueAudioChunk(newAudioChunk);
            }
        }

        private void OnAudioChunkDiscarded(MvxAudioChunk discardedAudioChunk)
        {
            MvxAudioChunksPool.instance.ReturnAudioChunk(discardedAudioChunk);
        }

        #endregion
    }
}