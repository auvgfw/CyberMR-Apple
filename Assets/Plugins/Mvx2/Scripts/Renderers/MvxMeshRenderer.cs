using UnityEngine;
using System;

namespace MVXUnity
{
    [ExecuteInEditMode]
    public abstract class MvxMeshRenderer : MvxAsyncDataProcessor
    {
        #region data
        
        [SerializeField, HideInInspector] private Material[] m_materialTemplates = null;
        public Material[] materialTemplates
        {
            get { return m_materialTemplates == null ? null : (Material[]) m_materialTemplates.Clone(); }
            set
            {
                if (m_materialTemplates == null && value == null)
                    return;

                if (m_materialTemplates != null && value != null && m_materialTemplates.Length == value.Length)
                {
                    bool changed = false;
                    for (int i = 0; i < m_materialTemplates.Length; i++)
                    {
                        if (m_materialTemplates[i] != value[i])
                        {
                            changed = true;
                            break;
                        }

                    }

                    if (!changed)
                        return;
                }

                if (value == null)
                    m_materialTemplates = null;
                else
                    m_materialTemplates = (Material[]) value.Clone();

                if (isActiveAndEnabled)
                {
                    DestroyMaterialInstances();
                    CreateMaterialInstances();
                }
            }
        }

        protected virtual void DestroyMaterialInstances()
        {
            if (m_materialInstances == null)
                return;

            if (m_meshPart)
                m_meshPart.SetMaterials(null);

            foreach (Material materialInstance in m_materialInstances)
            {
                if (Application.isPlaying)
                    Destroy(materialInstance);
                else
                    DestroyImmediate(materialInstance);
            }
                
            m_materialInstances = null;
        }

        protected virtual void CreateMaterialInstances()
        {
            if (m_materialTemplates == null || m_materialTemplates.Length == 0)
                return;

            m_materialInstances = new Material[m_materialTemplates.Length];
            for (int i = 0; i < m_materialTemplates.Length; i++)
                m_materialInstances[i] = Instantiate<Material>(m_materialTemplates[i]);

            if (m_meshPart)
                m_meshPart.SetMaterials(m_materialInstances);
        }

        [SerializeField, HideInInspector] private Material[] m_materialInstances = null;
        protected Material[] materialInstances
        {
            get { return m_materialInstances; }
        }

        private MvxMeshPart m_meshPart = null;
        [SerializeField, HideInInspector] private GameObject m_transformFixGO = null;

        #endregion

        #region optimization data

        // temporary collections made into fields to avoid their constant reallocations

        private Vector3[] m_meshPartVertices = null;
        private Vector3[] m_meshPartNormals = null;
        private Color32[] m_meshPartColors = null;
        private Vector2[] m_meshPartUVs = null;
        private Int32[] m_meshPartIndices = null;

        #endregion

        #region process frame

        protected override void ResetProcessedData()
        {
            // disable mesh
            if (m_meshPart)
            {
                m_meshPart.ClearMesh();
                m_meshPart.gameObject.SetActive(false);
            }
        }

        protected override void ProcessNextFrame(MVGraphAPI.Frame frame)
        {
            UpdateMeshPart(MVGraphAPI.FrameMeshExtractor.GetMeshData(frame));
        }

        protected virtual bool IgnoreNormals()
        {
            return false;
        }

        protected virtual bool IgnoreColors()
        {
            return false;
        }

        protected virtual bool IgnoreUVs()
        {
            return false;
        }

        protected virtual UInt32 GetFrameMeshIndicesCount(MVGraphAPI.MeshData meshData)
        {
            return meshData.GetNumIndices();
        }

        unsafe protected virtual void CopyMeshIndicesToCollection(MVGraphAPI.MeshData meshData, Int32[] meshPartIndices)
        {
            fixed (Int32* indicesPtr = meshPartIndices)
                meshData.CopyIndicesRaw((IntPtr)indicesPtr);
        }

        protected virtual MVGraphAPI.MeshIndicesMode GetFrameIndicesMode(MVGraphAPI.MeshData meshData)
        {
            return MVGraphAPI.MeshIndicesMode.MIM_TriangleList;
        }

        unsafe private void UpdateMeshPart(MVGraphAPI.MeshData meshData)
        {
            if (!m_meshPart)
                m_meshPart = CreateNewMeshPart();

            UInt32 collectionsCapacity = GetMinimalMeshCollectionsCapacity(meshData);

            MvxUtils.EnsureCollectionMinimalCapacity<Vector3>(ref m_meshPartVertices, collectionsCapacity);
            fixed (Vector3* verticesPtr = m_meshPartVertices)
                meshData.CopyVerticesRaw((IntPtr)verticesPtr);

            if (!IgnoreNormals())
            {
                MvxUtils.EnsureCollectionMinimalCapacity<Vector3>(ref m_meshPartNormals, collectionsCapacity);
                fixed (Vector3* normalsPtr = m_meshPartNormals)
                    meshData.CopyNormalsRaw((IntPtr)normalsPtr);
            }

            if (!IgnoreColors())
            {
                MvxUtils.EnsureCollectionMinimalCapacity<Color32>(ref m_meshPartColors, collectionsCapacity);
                fixed (Color32* colorsPtr = m_meshPartColors)
                    meshData.CopyColorsRGBARaw((IntPtr)colorsPtr);
            }

            if (!IgnoreUVs())
            {
                MvxUtils.EnsureCollectionMinimalCapacity<Vector2>(ref m_meshPartUVs, collectionsCapacity);
                fixed (Vector2* uvsPtr = m_meshPartUVs)
                    meshData.CopyUVsRaw((IntPtr)uvsPtr);
            }

            UInt32 meshIndicesCount = GetFrameMeshIndicesCount(meshData);
            if (meshIndicesCount == 0)
            {
                m_meshPart.gameObject.SetActive(false);
                return;
            }
            MvxUtils.EnsureCollectionMinimalCapacity<Int32>(ref m_meshPartIndices, meshIndicesCount);
            CopyMeshIndicesToCollection(meshData, m_meshPartIndices);
            MVGraphAPI.MeshIndicesMode meshIndicesMode = GetFrameIndicesMode(meshData);
            
            // fill the unused trailing of the indices collection with the last used index
            Int32 unusedIndicesValue = m_meshPartIndices[(Int32)meshIndicesCount - 1];
            for (UInt32 unusedIndex = meshIndicesCount; unusedIndex < m_meshPartIndices.Length; unusedIndex++)
                m_meshPartIndices[unusedIndex] = unusedIndicesValue;
            
            m_meshPart.UpdateMesh(m_meshPartVertices, m_meshPartNormals, m_meshPartColors, m_meshPartUVs, m_meshPartIndices, IndicesModeToMeshTopology(meshIndicesMode));            
            m_meshPart.gameObject.SetActive(true);
        }

        private MvxMeshPart CreateNewMeshPart()
        {
            GameObject partGameObject = new GameObject("MeshPart");
            partGameObject.hideFlags = partGameObject.hideFlags | HideFlags.DontSave;
            partGameObject.transform.SetParent(m_transformFixGO.transform);
            partGameObject.transform.localPosition = Vector3.zero;
            partGameObject.transform.localRotation = Quaternion.identity;
            partGameObject.transform.localScale = Vector3.one;

            MvxMeshPart newMeshPart = partGameObject.AddComponent<MvxMeshPart>();
            newMeshPart.SetMaterials(m_materialInstances);
            return newMeshPart;
        }

        private UInt32 GetMinimalMeshCollectionsCapacity(MVGraphAPI.MeshData meshData)
        {
            UInt32 meshCollectionsCapacity = meshData.GetNumVertices();

            UInt32 colorsCount = IgnoreColors() ? 0 : meshData.GetNumColors();
            meshCollectionsCapacity = meshCollectionsCapacity >= colorsCount ? meshCollectionsCapacity : colorsCount;

            UInt32 normalsCount = IgnoreNormals() ? 0 : meshData.GetNumNormals();
            meshCollectionsCapacity = meshCollectionsCapacity >= normalsCount ? meshCollectionsCapacity : normalsCount;

            UInt32 uvsCount = IgnoreUVs() ? 0 : meshData.GetNumUVs();
            meshCollectionsCapacity = meshCollectionsCapacity >= uvsCount ? meshCollectionsCapacity : uvsCount;

            return meshCollectionsCapacity;
        }

        private MeshTopology IndicesModeToMeshTopology(MVGraphAPI.MeshIndicesMode indicesMode)
        {
            switch (indicesMode)
            {
                case MVGraphAPI.MeshIndicesMode.MIM_PointList: return MeshTopology.Points;
                case MVGraphAPI.MeshIndicesMode.MIM_LineList: return MeshTopology.Lines;
                case MVGraphAPI.MeshIndicesMode.MIM_TriangleList: return MeshTopology.Triangles;
                case MVGraphAPI.MeshIndicesMode.MIM_QuadList: return MeshTopology.Quads;
                default:
                    throw new System.ArgumentException("Missing conversion from an indices mode to a mesh topology");
            }
        }

        #endregion

        #region MonoBehaviour

        public virtual void Awake()
        {
            CreateMaterialInstances();
            CreateTransformFixGO();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            DestroyMeshPart();
            DestroyTransformFixGO();
            DestroyMaterialInstances();
        }

        private void CreateTransformFixGO()
        {
            if (m_transformFixGO != null)
                return;

            m_transformFixGO = new GameObject("TransformFix");
            m_transformFixGO.transform.parent = gameObject.transform;
            m_transformFixGO.transform.localPosition = Vector3.zero;
            m_transformFixGO.transform.localRotation = Quaternion.identity;
            m_transformFixGO.transform.localScale = new Vector3(1, 1, -1);
        }

        private void DestroyTransformFixGO()
        {
            if (m_transformFixGO == null)
                return;

            if (Application.isPlaying)
                Destroy(m_transformFixGO);
            else
                DestroyImmediate(m_transformFixGO);

            m_transformFixGO = null;
        }

        private void DestroyMeshPart()
        {
            if (!m_meshPart)
                return;

            m_meshPart.ClearMesh();
            m_meshPart.gameObject.SetActive(false);
            if (Application.isPlaying)
                Destroy(m_meshPart.gameObject);
            else
                DestroyImmediate(m_meshPart.gameObject);
            m_meshPart = null;
        }

        #endregion
    }
}