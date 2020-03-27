using System;
using UnityEngine;

namespace MVXUnity
{
    [AddComponentMenu("Mvx2/Data Processors/Mesh Textured Renderer")]
    public class MvxMeshTexturedRenderer : MvxMeshRenderer
    {
        #region data

        protected override void CreateMaterialInstances()
        {
            base.CreateMaterialInstances();
            if (materialInstances == null || materialInstances.Length == 0)
                return;

            foreach (Material materialInstance in materialInstances)
                materialInstance.SetTexture(TEXTURE_SHADER_PROPERTY_NAME, null);
        }

        #endregion

        #region process frame

        public static bool SupportsStreamRendering(MVGraphAPI.SourceInfo sourceInfo)
        {
            bool streamSupported =
                sourceInfo.ContainsDataLayer(MVGraphAPI.SimpleDataLayersGuids.VERTEX_POSITIONS_DATA_LAYER)
                && sourceInfo.ContainsDataLayer(MVGraphAPI.SimpleDataLayersGuids.VERTEX_INDICES_DATA_LAYER)
                && sourceInfo.ContainsDataLayer(MVGraphAPI.SimpleDataLayersGuids.VERTEX_UVS_DATA_LAYER)
                && SourceInfoContainsTexture(sourceInfo);
            return streamSupported;
        }

        private static bool SourceInfoContainsTexture(MVGraphAPI.SourceInfo sourceInfo)
        {
            return sourceInfo.ContainsDataLayer(MVGraphAPI.SimpleDataLayersGuids.ASTC_TEXTURE_DATA_LAYER)
                || sourceInfo.ContainsDataLayer(MVGraphAPI.SimpleDataLayersGuids.DXT1_TEXTURE_DATA_LAYER)
                || sourceInfo.ContainsDataLayer(MVGraphAPI.SimpleDataLayersGuids.ETC2_TEXTURE_DATA_LAYER)
                || sourceInfo.ContainsDataLayer(MVGraphAPI.SimpleDataLayersGuids.NVX_TEXTURE_DATA_LAYER)
                || sourceInfo.ContainsDataLayer(MVGraphAPI.SimpleDataLayersGuids.RGB_TEXTURE_DATA_LAYER);
        }

        protected override bool CanProcessStream(MVGraphAPI.SourceInfo sourceInfo)
        {
            bool streamSupported = SupportsStreamRendering(sourceInfo);
            Debug.LogFormat("Mvx2: MeshTextured renderer {0} rendering of the new mvx stream", streamSupported ? "supports" : "does not support");
            return streamSupported;
        }

        protected override void ProcessNextFrame(MVGraphAPI.Frame frame)
        {
            base.ProcessNextFrame(frame);
            CollectTextureDataFromFrame(frame);
        }

        protected override bool IgnoreColors()
        {
            return true;
        }

        #endregion

        #region texture

        private static readonly string TEXTURE_SHADER_PROPERTY_NAME = "_MainTex";
        private static readonly string TEXTURE_TYPE_SHADER_PROPERTY_NAME = "_TextureType";

        private enum TextureTypeCodes
        {
            TTC_ASTC = 4,
            TTC_DXT1 = 3,
            TTC_ETC2 = 2,
            TTC_NVX = 0,
            TTC_RGB = 1
        };

        // an array of textures - they are switched between updates to improve performance -> textures double-buffering
        private Texture2D[] m_textures = new Texture2D[2];
        private int m_activeTextureIndex = -1;

        private void CollectTextureDataFromFrame(MVGraphAPI.Frame frame)
        {
            if (materialInstances == null || materialInstances.Length == 0)
                return;

            int textureType;
            TextureFormat textureFormat;
            MVGraphAPI.FrameTextureExtractor.TextureType mvxTextureType;

            if (frame.StreamContainsDataLayer(MVGraphAPI.SimpleDataLayersGuids.ASTC_TEXTURE_DATA_LAYER))
            {
                textureType = (int) TextureTypeCodes.TTC_ASTC;
                textureFormat = TextureFormat.ASTC_8x8;
                mvxTextureType = MVGraphAPI.FrameTextureExtractor.TextureType.TT_ASTC;
            }
            else if (frame.StreamContainsDataLayer(MVGraphAPI.SimpleDataLayersGuids.DXT1_TEXTURE_DATA_LAYER))
            {
                textureType = (int) TextureTypeCodes.TTC_DXT1;
                textureFormat = TextureFormat.DXT1;
                mvxTextureType = MVGraphAPI.FrameTextureExtractor.TextureType.TT_DXT1;
            }
            else if (frame.StreamContainsDataLayer(MVGraphAPI.SimpleDataLayersGuids.ETC2_TEXTURE_DATA_LAYER))
            {
                textureType = (int) TextureTypeCodes.TTC_ETC2;
                textureFormat = TextureFormat.ETC2_RGB;
                mvxTextureType = MVGraphAPI.FrameTextureExtractor.TextureType.TT_ETC2;
            }
            else if (frame.StreamContainsDataLayer(MVGraphAPI.SimpleDataLayersGuids.NVX_TEXTURE_DATA_LAYER))
            {
                textureType = (int) TextureTypeCodes.TTC_NVX;
                textureFormat = TextureFormat.Alpha8;
                mvxTextureType = MVGraphAPI.FrameTextureExtractor.TextureType.TT_NVX;
            }
            else if (frame.StreamContainsDataLayer(MVGraphAPI.SimpleDataLayersGuids.RGB_TEXTURE_DATA_LAYER))
            {
                textureType = (int) TextureTypeCodes.TTC_RGB;
                textureFormat = TextureFormat.RGB24;
                mvxTextureType = MVGraphAPI.FrameTextureExtractor.TextureType.TT_RGB;
            }
            else
            {
                foreach (Material materialInstance in materialInstances)
                    materialInstance.SetTexture(TEXTURE_SHADER_PROPERTY_NAME, null);
                return;
            }

            ushort textureWidth, textureHeight;
            MVGraphAPI.FrameTextureExtractor.GetTextureResolution(frame, mvxTextureType, out textureWidth, out textureHeight);
            UInt32 textureSizeInBytes = MVGraphAPI.FrameTextureExtractor.GetTextureDataSizeInBytes(frame, mvxTextureType);
            IntPtr textureData = MVGraphAPI.FrameTextureExtractor.GetTextureData(frame, mvxTextureType);

            m_activeTextureIndex = (m_activeTextureIndex + 1) % m_textures.Length;
            Texture2D newActiveTexture = m_textures[m_activeTextureIndex];
            EnsureTextureProperties(ref newActiveTexture, textureFormat, textureWidth, textureHeight);
            m_textures[m_activeTextureIndex] = newActiveTexture;

            newActiveTexture.LoadRawTextureData(textureData, (Int32)textureSizeInBytes);
            newActiveTexture.Apply(true, false);

            foreach (Material materialInstance in materialInstances)
            {
                materialInstance.SetInt(TEXTURE_TYPE_SHADER_PROPERTY_NAME, textureType);
                materialInstance.SetTexture(TEXTURE_SHADER_PROPERTY_NAME, newActiveTexture);
            }
        }

        private void EnsureTextureProperties(ref Texture2D texture, TextureFormat targetFormat, ushort targetWidth, ushort targetHeight)
        {
            if (texture == null
                || texture.format != targetFormat
                || texture.width != targetWidth || texture.height != targetHeight)
                texture = new Texture2D(targetWidth, targetHeight, targetFormat, false);
        }

        #endregion

        #region MonoBehaviour

        public override void Reset()
        {
            base.Reset();
#if UNITY_EDITOR
            Material defaultMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Plugins/Mvx2/Materials/MeshTextured.mat");
            if (defaultMaterial != null)
                materialTemplates = new Material[] { defaultMaterial };
#endif
        }

        #endregion
    }
}