using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Logic : MonoBehaviour
{
    public void LoadMainScene()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void QuitApplictation()
    {
        Application.Quit();
    }
}
