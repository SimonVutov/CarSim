using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void LoadLevel1 ()
    {
        SceneManager.LoadScene("Track1");
    }

    public void LoadMenu ()
    {
        SceneManager.LoadScene(0);
    }

    public void QuitGame ()
    {
        Debug.Log("Quit!");
        Application.Quit();
    }
}
