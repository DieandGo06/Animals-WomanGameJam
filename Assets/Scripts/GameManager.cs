using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;




    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        Application.targetFrameRate = 60;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void GoToGameScene()
    {
        SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }

    public void GoToMainMenuScene()
    {
        SceneManager.LoadScene("Menu", LoadSceneMode.Single);
    }

    public void GoToCreditosScene()
    {
        SceneManager.LoadScene("Creditos", LoadSceneMode.Single);
    }
}
