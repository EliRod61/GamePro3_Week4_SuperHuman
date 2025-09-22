using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour
{
    public GameObject UI_Menu;
    [SerializeField] PlayerMovement playerScript;
    [SerializeField] GameManager gameManager;

    public void OpenUI()
    {
        UI_Menu.SetActive(true);
    }

    public void CloseUI()
    {
        gameManager.ResumeTime();
        UI_Menu.SetActive(false);
    }

    public void LoadNextScene()
    {
        SceneManager.LoadScene(1);
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
    public void ResetToCheckpoint()
    {
        playerScript.RespawnPlayer();
        gameManager.ResumeTime();
        UI_Menu.SetActive(false);
    }
    public void ReloadScene()
    {
        gameManager.ResumeTime();
        UI_Menu.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
