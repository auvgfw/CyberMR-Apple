using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    // Start is called before the first frame update

    int secretClickedTimes;
    private void Start()
    {
        secretClickedTimes = 0;
    }

    public void OnBtn_ImageTrack_NameCardClick()
    {

        SceneManager.LoadScene(3);

    }
    public void OnBtn_AR_SampleClick()
    {

        SceneManager.LoadScene(1);

    }
    public void OnBtn_HideAndSeekClick()
    {
        SceneManager.LoadScene(2);
    }

    public void OnBtn_DramaClick()
    {
        SceneManager.LoadScene(3);
    }

}
