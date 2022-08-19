using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;


[System.Serializable]
public struct Score
{
    public int BuildingsDestroyed;
    public int TrapsDestroyed;
    public int PowerLinesDestroyed;
    public int BuildingsLeft;
    public int TrapsLeft;
    public int PowerLinesLeft;
    public int LeftoversUtilized;
    public int LeftoversLeft;
}


public class AppController : MonoBehaviour
{
    // Start is called before the first frame update
    public enum Screen
    {
        Menu,
        About,
        Death,
        Results
    }

    public TileManager tileManager;
    public GameObject playerObj;
    public GameObject loadingScreen;
    public GameObject menuScreen;
    public GameObject deathScreen;
    public GameObject resultsScreen;
    public GameObject mainCamera;
    public GameObject world;
    public PlayerInput playerInput;

    public TMP_Text utilized;
    public TMP_Text traps;
    public TMP_Text junk;
    public TMP_Text total;

    public Button[] menuButtons;
    public int activeButton = 0;

    public Button[] aboutButtons;
    public int activeAboutButtons;

    public Screen activeScreen = Screen.Menu;

    public Score score;

    public void Navigate(InputAction.CallbackContext context)
    {
        Debug.Log("bong");
        if (context.started || context.performed)
        {
            Debug.Log("bang");

            Vector2 moveInput = context.ReadValue<Vector2>();

            if (activeScreen == Screen.Menu)
            {
                MenuNavigation(moveInput);
            }
            if (activeScreen == Screen.About)
            {
                AboutNavigation(moveInput);
            }
        }
    }

    void MenuNavigation(Vector2 moveInput)
    {
        if (moveInput.y != 0)
        {
            // return;

            menuButtons[activeButton].transform.position = new Vector3(
                menuButtons[activeButton].transform.position.x + 10,
                menuButtons[activeButton].transform.position.y,
                menuButtons[activeButton].transform.position.z
            );

            if (moveInput.y > 0)
            {
                activeButton -= 1;
            }
            else if (moveInput.y < 0)
            {
                activeButton += 1;
            }

            if (activeButton >= menuButtons.Length)
            {
                activeButton = 0;
            }
            if (activeButton < 0)
            {
                activeButton = menuButtons.Length - 1;
            }

            menuButtons[activeButton].transform.position = new Vector3(
                menuButtons[activeButton].transform.position.x - 10,
                menuButtons[activeButton].transform.position.y,
                menuButtons[activeButton].transform.position.z
            );
        }
    }

    void AboutNavigation(Vector2 moveInput)
    {
        if (moveInput.x != 0)
        {
            // return;

            aboutButtons[activeAboutButtons].transform.position = new Vector3(
                aboutButtons[activeAboutButtons].transform.position.x,
                aboutButtons[activeAboutButtons].transform.position.y - 2,
                aboutButtons[activeAboutButtons].transform.position.z
            );

            if (moveInput.x > 0)
            {
                activeAboutButtons += 1;
            }
            else if (moveInput.x < 0)
            {
                activeAboutButtons -= 1;
            }

            if (activeAboutButtons >= aboutButtons.Length)
            {
                activeAboutButtons = 0;
            }
            if (activeAboutButtons < 0)
            {
                activeAboutButtons = aboutButtons.Length - 1;
            }

            aboutButtons[activeAboutButtons].transform.position = new Vector3(
                aboutButtons[activeAboutButtons].transform.position.x,
                aboutButtons[activeAboutButtons].transform.position.y + 2,
                aboutButtons[activeAboutButtons].transform.position.z
            );
        }
    }

    public void SetActiveScreen(int screen)
    {
        activeScreen = (Screen)screen;
    }

    public void Submit(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (activeScreen == Screen.Menu)
            {
                menuButtons[activeButton].onClick.Invoke();
            }
            else if (activeScreen == Screen.About)
            {
                aboutButtons[activeAboutButtons].onClick.Invoke();
            }
            else if (activeScreen == Screen.Death)
            {
                SceneManager.LoadScene("SampleScene");
            }
            else if (activeScreen == Screen.Results)
            {
                SceneManager.LoadScene("SampleScene");
            }
        }
    }

    public void OnDeath()
    {
        playerInput.SwitchCurrentActionMap("UI");
        deathScreen.SetActive(true);
        activeScreen = Screen.Death;
    }

    void Awake()
    {
        // StartCoroutine(WaitUntilInitialized());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator WaitUntilInitialized()
    {
        yield return new WaitUntil(() => tileManager.Initialized);

        // playerObj.transform.position = tileManager.GetPlayerStartPos();
        // Vector3 savePos = playerObj.transform.position;
        // world.transform.position = new Vector3(
        //     (int)(-savePos.x / 56)* 56,
        //     (int)(-savePos.y / 56)* 56,
        //     0f
        // );
        playerObj.GetComponent<PlayerController>().Initialized = true;;

        loadingScreen.SetActive(false);

        playerInput.SwitchCurrentActionMap("Player");
    }

    public void OnExitButton()
    {
        Application.Quit();
    }

    public void OnStartButton()
    {
        menuScreen.SetActive(false);
        loadingScreen.SetActive(true);

        tileManager.StartGen();
        StartCoroutine(WaitUntilInitialized());
    }

    public void FinishGame()
    {
        playerInput.SwitchCurrentActionMap("UI");
        activeScreen = Screen.Results;
        resultsScreen.SetActive(true);

        int utilizedScore = score.BuildingsDestroyed * 10 - (score.BuildingsLeft - score.BuildingsDestroyed) * 3;
        int trapsScore = score.TrapsDestroyed * 1 - (score.TrapsLeft - score.TrapsDestroyed) * 10;
        int junkScore = score.LeftoversUtilized * 3 - (score.LeftoversLeft - score.LeftoversUtilized) * 3;

        utilized.text = $"{utilizedScore}";
        traps.text = $"{trapsScore}";
        junk.text = $"{junkScore}";
        total.text = $"{utilizedScore + trapsScore + junkScore}";
    }

    public GameObject[] aboutSubscreens;
    public int currentAboutSubscreens = 0;
    public void SwitchAboutSubscreen()
    {
        aboutSubscreens[currentAboutSubscreens].SetActive(false);

        currentAboutSubscreens += 1;
        if (currentAboutSubscreens >= aboutSubscreens.Length)
        {
            currentAboutSubscreens = 0;
        }
        aboutSubscreens[currentAboutSubscreens].SetActive(true);
    }
}
