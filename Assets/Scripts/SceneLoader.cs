using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenBeachScene()
    {
        SceneManager.LoadScene("Beach");
    }

    public void OpenIndoorScene()
    {
        SceneManager.LoadScene("Indoor");
    }

    public void OpenOutdoorScene()
    {
        SceneManager.LoadScene("Outdoor");
    }
}
