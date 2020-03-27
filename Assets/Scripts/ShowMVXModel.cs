using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MVXUnity;

public class ShowMVXModel : MonoBehaviour
{


    public MvxDataStream dataStreamPrefab;

    private string filePath = null;
    public static List<MvxDataStream> dataStreams = new List<MvxDataStream>();
    string devicePath = null;
    // Use this for initialization
    void Start()
    {
        Debug.Log("START");

        //此filepath是ios package中读取streamingassets目录下的模型路径
        //对应的android platform/pc 若不知道网上可以查阅资料
        //filePath = "file://" + Application.streamingAssetsPath + "/Resources/Katya.mvx";
#if UNITY_IPHONE
                    filePath = Application.dataPath + "/Raw" + "/tianyao.mvx";
                    Debug.Log("路径 + " + filePath);
                    devicePath=Application.dataPath + "/Raw/";
#elif UNITY_EDITOR
        filePath = Application.dataPath + "/StreamingAssets" + "/tianyao.mvx";
        Debug.Log("路径 + " + filePath);
        devicePath = Application.dataPath + "/StreamingAssets/";
#elif UNITY_ANDROID
                filePath="tianyao.mvx";
        devicePath="";
#endif
        filePath = devicePath + "tianyao.mvx";
        addMvxModel();
        filePath = devicePath + "kgirl.mvx";
        //addMvxModel();

    }


    // Update is called once per frame
    void Update()
    {

    }

    void addMvxModel()
    {
        MvxDataStream dataStream;
        dataStream = Instantiate(dataStreamPrefab);
        dataStreams.Add(dataStream);
        MvxFileDataStreamDefinition dataStreamDefinition = new MvxFileDataStreamDefinition();
        //赋值文件路径
        dataStreamDefinition.filePath = filePath;
        //将dataStream中的definition重新赋值为新建的definition
        dataStream.dataStreamDefinition = dataStreamDefinition;
        //设置位置,旋转,缩放等
        dataStream.transform.position = new Vector3(0, 0, 0);
        if (filePath == devicePath + "kgirl.mvx")
        {
            dataStream.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
        }
        else
        {
            dataStream.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
        }
        dataStream.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
        dataStream.Pause();

    }

    public static void changeMvxModel(int i, int index)
    {
        string devicePath, filePath;
        #if UNITY_IPHONE
                filePath = Application.dataPath + "/Raw" + "/tianyao.mvx";
                Debug.Log("路径 + " + filePath);
        devicePath = Application.dataPath + "/Raw/";
        #elif UNITY_EDITOR
                filePath = Application.dataPath + "/StreamingAssets" + "/tianyao.mvx";
                Debug.Log("路径 + " + filePath);
                devicePath = Application.dataPath + "/StreamingAssets/";
        #elif UNITY_ANDROID
                filePath="tianyao.mvx";
                devicePath="";
        #endif
        switch (index)
        {
            case 0:
                filePath = devicePath + "tianyao.mvx";
                break;
            case 1:
                filePath = devicePath + "Katya.mvx";
                break;
            case 2:
                filePath = devicePath + "liuyinan1.mvx";
                break;
            case 3:
                filePath = devicePath + "liuyinan2.mvx";
                break;
            default:
                filePath = devicePath + "tianyao.mvx";
                break;

        }

        MvxFileDataStreamDefinition dataStreamDefinition = new MvxFileDataStreamDefinition();
        //赋值文件路径
        dataStreamDefinition.filePath = filePath;

        dataStreams[i].dataStreamDefinition = dataStreamDefinition;
    }





}
