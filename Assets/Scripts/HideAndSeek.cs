using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using MVXUnity;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
/// <summary>
/// Listens for touch events and performs an AR raycast from the screen touch point.
/// AR raycasts will only hit detected trackables like feature points and planes.
///
/// If a raycast hits a trackable, the <see cref="placedPrefab"/> is instantiated
/// and moved to the hit position.
/// </summary>
[RequireComponent(typeof(ARRaycastManager))]
public class HideAndSeek : MonoBehaviour
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

    public MvxDataStream dataStreamPrefab;
    public GameObject Cube;
    public GameObject Puzzle;
    GameObject m_puzzle;

    private string filePath = null;
    private string devicefilePath = null;
    private MvxDataStream dataStream1;
    private MvxDataStream dataStream2;
    private int filePathIndex;

    private Vector3 modelpos,oripos;
    public GameObject arcamera;

    public Text text;
    float distance;

    void Start()
    {
        Debug.Log("START");

        //此filepath是ios package中读取streamingassets目录下的模型路径
        //对应的android platform/pc 若不知道网上可以查阅资料
        //filePath = "file://" + Application.streamingAssetsPath + "/Resources/Katya.mvx";

        //devicefilePath=Application.dataPath + "/Raw/";


        //devicefilePath = Application.dataPath + "/StreamingAssets/";

        devicefilePath = "";


        filePath = devicefilePath + "tianyao.mvx";
        Debug.Log(filePath);
        filePathIndex = 0;
        dataStream1 = addMvxModel(filePath);
        filePath = devicefilePath + "Katya.mvx";

       // dataStream2 = addMvxModel(filePath);


    }

    /// <summary>
    /// The object instantiated as a result of a successful raycast intersection with a plane.
    /// </summary>
    public GameObject spawnedObject { get; private set; }



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
        distance = (arcamera.transform.position.x - modelpos.x) * (arcamera.transform.position.x - modelpos.x) + (arcamera.transform.position.z - modelpos.z) * (arcamera.transform.position.z - modelpos.z)+ (arcamera.transform.position.y - modelpos.y) * (arcamera.transform.position.y - modelpos.y);
        text.text = distance.ToString();
        if (distance < 2.3)
        {
            int posnumber;
            posnumber = Random.Range(1, 6);
            posnumber = posnumber % 5;
            float offsetx;
            float offsety;
            offsetx = 0;
            offsety = 0;
            switch (posnumber)
            {
                case 0:
                    offsetx = 0;
                    offsety = 0;
                    break;
                case 1:
                    offsetx = -1.5f;
                    offsetx = -1.5f;
                    break;
                case 2:
                    offsetx = -1.5f;
                    offsety = 1.5f;
                    break;
                case 3:
                    offsetx = 1.5f;
                    offsety = -1.5f;
                    break;
                case 4:
                    offsetx = -1.5f;
                    offsety = -1.5f;
                    break;
                default:
                    offsety = 0;
                    offsetx = 0;
                    break;
            }
            modelpos.x = oripos.x + offsetx;
            modelpos.z = oripos.z + offsety;
            dataStream1.transform.position = modelpos;


        }

        if (!TryGetTouchPosition(out Vector2 touchPosition))
            return;

        if (EventSystem.current.IsPointerOverGameObject())
        {

            // Do nothing
        }

        else if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinPolygon))
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
                dataStream1.transform.position = hitPose.position;
                modelpos = hitPose.position;
                oripos = modelpos;
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
        dataStream.transform.rotation = Quaternion.Euler(new Vector3(0, 10, 0));
        dataStream.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
        return dataStream;

    }



    public void onClickReturn()
    {
        SceneManager.LoadScene(0);
    }

    public void onClickPlace()
    {

        if (!m_puzzle)
        {
            float x, y, z;
            x = dataStream1.transform.position.x;
            y = dataStream1.transform.position.y;
            z = dataStream1.transform.position.z;
            m_puzzle = Instantiate(Puzzle, dataStream1.transform.position, dataStream1.transform.rotation);
        }
        else
            m_puzzle.transform.position = dataStream1.transform.position;
        int i, j;
        for (i = -5; i <= 5; i++)
        {
            for (j = -5; j <= 5; j++)
            {
               // Instantiate(Cube, new Vector3(x+i, y, z+j), Quaternion.Euler(new Vector3(0, 0, 0)));
                
            }
        }
    }
   
}


