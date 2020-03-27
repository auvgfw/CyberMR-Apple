using System;
using UnityEngine;
using UnityEngine.UI;

namespace MVXUnity
{
    [AddComponentMenu("Mvx2/Data Processors/Texture Extractor")]
    public class MvxTextureExtractor : MvxAsyncDataProcessor
    {
        #region data 

        public enum MvxTextureFormat
        {
            DEPTH_MAP,
            ASTC,
            DXT1,
            ETC2,
            RGB,
            IR
        }

        [SerializeField] private MvxTextureFormat m_textureFormat = MvxTextureFormat.RGB;
        [SerializeField] private RawImage m_rawImageUI = null;
        [SerializeField] private float m_depthMultiplier = 1f;

        #endregion

        #region stream handlers

        protected override bool CanProcessStream(MVGraphAPI.SourceInfo sourceInfo)
        {
            return true;
        }

        protected override void ResetProcessedData()
        {
        }

        protected override void ProcessNextFrame(MVGraphAPI.Frame frame)
        {
            if (m_rawImageUI == null)
                return;

            if (CollectTextureDataFromFrame(frame))
                m_rawImageUI.texture = m_textures[m_activeTextureIndex];
            else
                m_rawImageUI.texture = m_blackTexture;
        }

        #endregion

        #region texture

        // an array of textures - they are switched between updates to improve performance -> textures double-buffering
        private Texture2D[] m_textures = new Texture2D[2];
        private int m_activeTextureIndex = -1;
        private Texture2D m_blackTexture = null;

        private bool CollectTextureDataFromFrame(MVGraphAPI.Frame frame)
        {
            TextureFormat textureFormat;
            ushort textureWidth, textureHeight;
            UInt32 textureSizeInBytes;

            if (m_textureFormat == MvxTextureFormat.DEPTH_MAP && frame.StreamContainsDataLayer(MVGraphAPI.SimpleDataLayersGuids.DEPTHMAP_TEXTURE_DATA_LAYER))
            {
                textureFormat = TextureFormat.R8;
                MVGraphAPI.FrameTextureExtractor.GetTextureResolution(frame, MVGraphAPI.FrameTextureExtractor.TextureType.TT_DEPTH, out textureWidth, out textureHeight);
                textureSizeInBytes = MVGraphAPI.FrameTextureExtractor.GetTextureDataSizeInBytes(frame, MVGraphAPI.FrameTextureExtractor.TextureType.TT_DEPTH) / 2;
            }
            else if (m_textureFormat == MvxTextureFormat.ASTC && frame.StreamContainsDataLayer(MVGraphAPI.SimpleDataLayersGuids.ASTC_TEXTURE_DATA_LAYER))
            {
                textureFormat = TextureFormat.ASTC_8x8;
                MVGraphAPI.FrameTextureExtractor.GetTextureResolution(frame, MVGraphAPI.FrameTextureExtractor.TextureType.TT_ASTC, out textureWidth, out textureHeight);
                textureSizeInBytes = MVGraphAPI.FrameTextureExtractor.GetTextureDataSizeInBytes(frame, MVGraphAPI.FrameTextureExtractor.TextureType.TT_ASTC);
            }
            else if (m_textureFormat == MvxTextureFormat.DXT1 && frame.StreamContainsDataLayer(MVGraphAPI.SimpleDataLayersGuids.DXT1_TEXTURE_DATA_LAYER))
            {
                textureFormat = TextureFormat.DXT1;
                MVGraphAPI.FrameTextureExtractor.GetTextureResolution(frame, MVGraphAPI.FrameTextureExtractor.TextureType.TT_DXT1, out textureWidth, out textureHeight);
                textureSizeInBytes = MVGraphAPI.FrameTextureExtractor.GetTextureDataSizeInBytes(frame, MVGraphAPI.FrameTextureExtractor.TextureType.TT_DXT1);
            }
            else if (m_textureFormat == MvxTextureFormat.ETC2 && frame.StreamContainsDataLayer(MVGraphAPI.SimpleDataLayersGuids.ETC2_TEXTURE_DATA_LAYER))
            {
                textureFormat = TextureFormat.ETC2_RGB;
                MVGraphAPI.FrameTextureExtractor.GetTextureResolution(frame, MVGraphAPI.FrameTextureExtractor.TextureType.TT_ETC2, out textureWidth, out textureHeight);
                textureSizeInBytes = MVGraphAPI.FrameTextureExtractor.GetTextureDataSizeInBytes(frame, MVGraphAPI.FrameTextureExtractor.TextureType.TT_ETC2);
            }
            else if (m_textureFormat == MvxTextureFormat.RGB && frame.StreamContainsDataLayer(MVGraphAPI.SimpleDataLayersGuids.RGB_TEXTURE_DATA_LAYER))
            {
                textureFormat = TextureFormat.RGB24;
                MVGraphAPI.FrameTextureExtractor.GetTextureResolution(frame, MVGraphAPI.FrameTextureExtractor.TextureType.TT_RGB, out textureWidth, out textureHeight);
                textureSizeInBytes = MVGraphAPI.FrameTextureExtractor.GetTextureDataSizeInBytes(frame, MVGraphAPI.FrameTextureExtractor.TextureType.TT_RGB);
            }
            else if (m_textureFormat == MvxTextureFormat.IR && frame.StreamContainsDataLayer(MVGraphAPI.SimpleDataLayersGuids.IR_TEXTURE_DATA_LAYER))
            {
                textureFormat = TextureFormat.R8;
                MVGraphAPI.FrameTextureExtractor.GetTextureResolution(frame, MVGraphAPI.FrameTextureExtractor.TextureType.TT_IR, out textureWidth, out textureHeight);
                textureSizeInBytes = MVGraphAPI.FrameTextureExtractor.GetTextureDataSizeInBytes(frame, MVGraphAPI.FrameTextureExtractor.TextureType.TT_IR);
            }
            else
            {
                return false;
            }

            m_activeTextureIndex = (m_activeTextureIndex + 1) % m_textures.Length;
            Texture2D newActiveTexture = m_textures[m_activeTextureIndex];
            EnsureTextureProperties(ref newActiveTexture, textureFormat, textureWidth, textureHeight);
            m_textures[m_activeTextureIndex] = newActiveTexture;

            LoadTexture(frame, newActiveTexture, m_textureFormat, textureWidth, textureHeight, textureSizeInBytes);

            return true;
        }

        private void EnsureTextureProperties(ref Texture2D texture, TextureFormat targetFormat, ushort targetWidth, ushort targetHeight)
        {
            if (texture == null
                || texture.format != targetFormat
                || texture.width != targetWidth || texture.height != targetHeight)
                texture = new Texture2D(targetWidth, targetHeight, targetFormat, false);
        }
        
        private unsafe void LoadTexture(MVGraphAPI.Frame frame, Texture2D targetTexture, MvxTextureFormat textureFormat, ushort width, ushort height, UInt32 sizeInBytes)
        {
            switch (textureFormat)
            {
                case MvxTextureFormat.DEPTH_MAP:
                    {
                        byte[] depthMapData = new byte[width * height * 2];
                        MVGraphAPI.FrameTextureExtractor.CopyTextureData(frame, MVGraphAPI.FrameTextureExtractor.TextureType.TT_DEPTH, depthMapData);
                        byte[] convertedDepthMapData = new byte[width * height];
                        for (int i = 0; i < convertedDepthMapData.Length; i++)
                        {
                            ushort depthMapDataElement = (ushort)(depthMapData[2 * i] + depthMapData[2 * i + 1] * 256);
                            convertedDepthMapData[i] = (byte)Mathf.Clamp(depthMapDataElement / 256 * m_depthMultiplier, 0, 256);
                        }
                            
                        fixed (byte* convertedDepthMapDataPtr = convertedDepthMapData)
                            targetTexture.LoadRawTextureData((IntPtr)convertedDepthMapDataPtr, (Int32)(sizeInBytes));
                    }
                    break;
                case MvxTextureFormat.ASTC:
                    {
                        IntPtr textureData = MVGraphAPI.FrameTextureExtractor.GetTextureData(frame, MVGraphAPI.FrameTextureExtractor.TextureType.TT_ASTC);
                        targetTexture.LoadRawTextureData(textureData, (Int32)sizeInBytes);
                    }
                    break;
                case MvxTextureFormat.DXT1:
                    {
                        IntPtr textureData = MVGraphAPI.FrameTextureExtractor.GetTextureData(frame, MVGraphAPI.FrameTextureExtractor.TextureType.TT_DXT1);
                        targetTexture.LoadRawTextureData(textureData, (Int32)sizeInBytes);
                    }
                    break;
                case MvxTextureFormat.ETC2:
                    {
                        IntPtr textureData = MVGraphAPI.FrameTextureExtractor.GetTextureData(frame, MVGraphAPI.FrameTextureExtractor.TextureType.TT_ETC2);
                        targetTexture.LoadRawTextureData(textureData, (Int32)sizeInBytes);
                    }
                    break;
                case MvxTextureFormat.RGB:
                    {
                        IntPtr textureData = MVGraphAPI.FrameTextureExtractor.GetTextureData(frame, MVGraphAPI.FrameTextureExtractor.TextureType.TT_RGB);
                        targetTexture.LoadRawTextureData(textureData, (Int32)sizeInBytes);
                    }
                    break;
                case MvxTextureFormat.IR:
                    {
                        IntPtr textureData = MVGraphAPI.FrameTextureExtractor.GetTextureData(frame, MVGraphAPI.FrameTextureExtractor.TextureType.TT_IR);
                        targetTexture.LoadRawTextureData(textureData, (Int32)sizeInBytes);
                    }
                    break;
                default:
                    return;
            }

            targetTexture.Apply(true, false);
        }


        #endregion

        #region MonoBehaviour

        public void Awake()
        {
            m_blackTexture = new Texture2D(1, 1, TextureFormat.R8, false);

            byte[] blackColorArray = new byte[1] { 0 };
            m_blackTexture.LoadRawTextureData(blackColorArray);
            m_blackTexture.Apply(false, false);
        }

        #endregion
    }
}