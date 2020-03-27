using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

using MVXUnity;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(ARRaycastManager))]
public class ARSampleController : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Instantiates this prefab on a plane at the touch location.")]
    GameObject m_PlacedPrefab;

    /// <summary>
    /// The prefab to instantiate on touch.
    /// </summary>
    public GameObject placedPrefab
    {
        get { return m_PlacedPrefab; }
        set { m_PlacedPrefab = value; }
    }

    /// <summary>
    /// The object instantiated as a result of a successful raycast intersection with a plane.
    /// </summary>
    public GameObject spawnedObject { get; private set; }


    public MvxDataStream dataStreamPrefab;

    public string filePath = null;
    private string devicePath = null;
#if UNITY_IPHONE
    private MvxDataStream dataStream1;
    private MvxDataStream dataStream2;
#elif UNITY_ANDROID
    private GameObject dataStream1;
    private GameObject dataStream2;
#endif
    private int filePathIndex;

    public GameObject m_cube;

    void Start()
    {
        Debug.Log("START");

        //此filepath是ios package中读取streamingassets目录下的模型路径
        //对应的android platform/pc 若不知道网上可以查阅资料
        //filePath = "file://" + Application.streamingAssetsPath + "/Resources/Katya.mvx";
#if UNITY_IPHONE
        filePath = Application.dataPath + "/Raw" + "/liuyinan.mvx";
        Debug.Log("路径 + " + filePath);
        devicePath=Application.dataPath + "/Raw/";
#elif UNITY_EDITOR
        filePath = Application.dataPath + "/StreamingAssets" + "/Katya.mvx";
        Debug.Log("路径 + " + filePath);
        devicePath = Application.dataPath + "/StreamingAssets/";
#elif UNITY_ANDROID
        filePath="tianyao.mvx";
        devicePath="";
#endif

#if UNITY_IPHONE
        filePathIndex = 0;
        dataStream1 = addMvxModel(filePath);
        filePath = devicePath + "lisaijia.mvx";
        dataStream2 = addMvxModel(filePath);
#elif UNITY_ANDROID

        dataStream1 = Instantiate(m_cube,
                                  Vector3.zero,
                                  Quaternion.Euler(Vector3.zero));
        dataStream2 = Instantiate(m_cube,
                          Vector3.zero,
                          Quaternion.Euler(Vector3.zero));


#endif
    }

    void Awake()
    {
        m_RaycastManager = GetComponent<ARRaycastManager>();
    }

    bool TryGetTouchPosition(out Vector2 touchPosition)
    {
#if UNITY_EDITOR
        if (Input.GetMouseButton(0))
        {
            var mousePosition = Input.mousePosition;
            touchPosition = new Vector2(mousePosition.x, mousePosition.y);
            return true;
        }
#else
        if (Input.touchCount > 0)
        {
            touchPosition = Input.GetTouch(0).position;
            return true;
        }
#endif

        touchPosition = default;
        return false;
    }

    void Update()
    {
        if (!TryGetTouchPosition(out Vector2 touchPosition))
            return;

        if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinPolygon))
        {
            // Raycast hits are sorted by distance, so the first one
            // will be the closest hit.
            var hitPose = s_Hits[0].pose;

            if (spawnedObject == null)
            {
                spawnedObject = Instantiate(m_PlacedPrefab, hitPose.position, hitPose.rotation);
            }
            else
            {
                spawnedObject.transform.position = hitPose.position;
            }
        }
    }

    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    ARRaycastManager m_RaycastManager;

    MvxDataStream addMvxModel(string filePath)
    {
        MvxDataStream dataStream = Instantiate(dataStreamPrefab);

        MvxFileDataStreamDefinition dataStreamDefinition = new MvxFileDataStreamDefinition();
        //赋值文件路径


        dataStreamDefinition.filePath = filePath;

        //将dataStream中的definition重新赋值为新建的definition
        dataStream.dataStreamDefinition = dataStreamDefinition;
        //设置位置,旋转,缩放等
        dataStream.transform.position = new Vector3(0, 0, 0);
        dataStream.transform.rotation = new Quaternion(0, 0, 0, 0);
        dataStream.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
        return dataStream;

    }


    public void onClickReturn()
    {
        SceneManager.LoadScene(0);
    }

    public void onClickScaleUp()
    {
        Vector3 currentScale;
        currentScale = dataStream1.transform.localScale;
        currentScale.x *= 1.2f;
        currentScale.y *= 1.2f;
        currentScale.z *= 1.2f;
        dataStream1.transform.localScale = currentScale;
    }
    public void onClickScaleUp2()
    {
        Vector3 currentScale;
        currentScale = dataStream2.transform.localScale;
        currentScale.x *= 1.2f;
        currentScale.y *= 1.2f;
        currentScale.z *= 1.2f;
        dataStream2.transform.localScale = currentScale;
    }
    public void onClickScaleDown()
    {
        Vector3 currentScale;
        currentScale = dataStream1.transform.localScale;
        currentScale.x *= 0.833f;
        currentScale.y *= 0.833f;
        currentScale.z *= 0.833f;
        dataStream1.transform.localScale = currentScale;
    }
    public void onClickScaleDown2()
    {
        Vector3 currentScale;
        currentScale = dataStream2.transform.localScale;
        currentScale.x *= 0.833f;
        currentScale.y *= 0.833f;
        currentScale.z *= 0.833f;
        dataStream2.transform.localScale = currentScale;
    }
    public void onClickPlaceActor1()
    {
        dataStream1.transform.position = spawnedObject.transform.position;
    }
    public void onClickPlaceActor2()
    {
        dataStream2.transform.position = spawnedObject.transform.position;
    }

    public void onClickActor1ClockWise()
    {
        dataStream1.transform.Rotate(new Vector3(0, 10, 0));
    }
    public void onClickActor1RevClockWise()
    {
        dataStream1.transform.Rotate(new Vector3(0, -10, 0));
    }
    public void onClickActor2ClockWise()
    {
        dataStream2.transform.Rotate(new Vector3(0, 10, 0));
    }
    public void onClickActor2RevClockWise()
    {
        dataStream2.transform.Rotate(new Vector3(0, -10, 0));
    }

    void LateUpdate()
    {
        float pinchAmount = 0;
        Quaternion desiredRotation = transform.rotation;

        DetectTouchMovement.Calculate();

        if (Mathf.Abs(DetectTouchMovement.pinchDistanceDelta) > 0)
        { // zoom
            pinchAmount = DetectTouchMovement.pinchDistanceDelta;
        }

        if (Mathf.Abs(DetectTouchMovement.turnAngleDelta) > 0)
        { // rotate
            Vector3 rotationDeg = Vector3.zero;
            rotationDeg.z = -DetectTouchMovement.turnAngleDelta;
            desiredRotation *= Quaternion.Euler(rotationDeg);
        }


        // not so sure those will work:
        dataStream1.transform.rotation = desiredRotation;
        dataStream1.transform.localScale += Vector3.one * pinchAmount;
    }
}

