using System;
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
        Results,
        Seed
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

    public Button[] seedButtons;
    public int activeSeedButtons;
    enum SeedSection{
        Buttons,
        Numbers
    }
    SeedSection activeSeedSection = SeedSection.Buttons;
    int activeNumber = 0;

    public Color activeColor;
    public Color notActiveColor;

    public Screen activeScreen = Screen.Menu;

    public Score score;

    public TMP_Text[] seedText;

    public TMP_Text seedTextDeath;
    public TMP_Text seedTextResult;

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
            if (activeScreen == Screen.Seed)
            {
                SeedNavigation(moveInput);
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
            menuButtons[activeButton].GetComponentInChildren<TMP_Text>().color = notActiveColor;

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
            menuButtons[activeButton].GetComponentInChildren<TMP_Text>().color = activeColor;
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
            aboutButtons[activeAboutButtons].GetComponentInChildren<TMP_Text>().color = notActiveColor;

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
            aboutButtons[activeAboutButtons].GetComponentInChildren<TMP_Text>().color = activeColor;
        }
    }

    void SeedNavigation(Vector2 moveInput)
    {
        if (moveInput.x != 0)
        {
            if (activeSeedSection == SeedSection.Buttons)
            {
                seedButtons[activeSeedButtons].transform.position = new Vector3(
                    seedButtons[activeSeedButtons].transform.position.x,
                    seedButtons[activeSeedButtons].transform.position.y - 2,
                    seedButtons[activeSeedButtons].transform.position.z
                );
                seedButtons[activeSeedButtons].GetComponentInChildren<TMP_Text>().color = notActiveColor;

                if (moveInput.x > 0)
                {
                    activeSeedButtons += 1;
                }
                else if (moveInput.x < 0)
                {
                    activeSeedButtons -= 1;
                }

                if (activeSeedButtons >= seedButtons.Length)
                {
                    activeSeedButtons = 0;
                }
                if (activeSeedButtons < 0)
                {
                    activeSeedButtons = seedButtons.Length - 1;
                }

                seedButtons[activeSeedButtons].transform.position = new Vector3(
                    seedButtons[activeSeedButtons].transform.position.x,
                    seedButtons[activeSeedButtons].transform.position.y + 2,
                    seedButtons[activeSeedButtons].transform.position.z
                );
                seedButtons[activeSeedButtons].GetComponentInChildren<TMP_Text>().color = activeColor;
            }
            else
            {
                seedText[activeNumber].color = notActiveColor;
                if (moveInput.x > 0)
                {
                    activeNumber += 1;
                }
                else if (moveInput.x < 0)
                {
                    activeNumber -= 1;
                }
                if (activeNumber >= seedText.Length)
                {
                    activeNumber = 0;
                }
                if (activeNumber < 0)
                {
                    activeNumber = seedText.Length - 1;
                }
                seedText[activeNumber].color = activeColor;
            }
        }

        else if (moveInput.y != 0)
        {
            if (activeSeedSection == SeedSection.Buttons)
            {
                if ( moveInput.y > 0)
                {
                    activeSeedSection = SeedSection.Numbers;

                    seedButtons[activeSeedButtons].transform.position = new Vector3(
                        seedButtons[activeSeedButtons].transform.position.x,
                        seedButtons[activeSeedButtons].transform.position.y - 2,
                        seedButtons[activeSeedButtons].transform.position.z
                    );
                    seedButtons[activeSeedButtons].GetComponentInChildren<TMP_Text>().color = notActiveColor;

                    seedText[activeNumber].color = activeColor;
                }
            }
            else
            {
                int number = Int32.Parse(seedText[activeNumber].text);
                if (moveInput.y > 0)
                {
                    number += 1;
                }
                else if (moveInput.y < 0)
                {
                    number -= 1;
                }

                if (number >= 10)
                {
                    if (activeNumber > 0)
                    {
                        number = 0;
                    }
                    else
                    {
                        number = 1;
                    }
                }
                else if (number < 0)
                {
                    number = 9;
                }

                if (activeNumber == 0 && number == 0)
                {
                    number = 9;
                }

                seedText[activeNumber].text = number.ToString();
            }
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
            else if (activeScreen == Screen.Seed)
            {
                if (activeSeedSection == SeedSection.Numbers)
                {
                    activeSeedSection = SeedSection.Buttons;
                        seedButtons[activeSeedButtons].transform.position = new Vector3(
                        seedButtons[activeSeedButtons].transform.position.x,
                        seedButtons[activeSeedButtons].transform.position.y + 2,
                        seedButtons[activeSeedButtons].transform.position.z
                    );
                    seedButtons[activeSeedButtons].GetComponentInChildren<TMP_Text>().color = activeColor;
                    seedText[activeNumber].color = notActiveColor;
                }
                else
                {
                    seedButtons[activeSeedButtons].onClick.Invoke();
                }
            }
        }
    }

    public void OnDeath()
    {
        playerInput.SwitchCurrentActionMap("UI");
        deathScreen.SetActive(true);
        activeScreen = Screen.Death;
        seedTextDeath.text = tileManager.seed.ToString();
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

        tileManager.seed = (uint)Int32.Parse(
            String.Format(
                "{0}{1}{2}{3}",
                seedText[0].text,
                seedText[1].text,
                seedText[2].text,
                seedText[3].text
            )
        );

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

        seedTextResult.text = tileManager.seed.ToString();
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

    public void RegenSeed()
    {
        uint seed = (uint)UnityEngine.Random.Range(1000, 9999);

        string seedString = seed.ToString();

        for (int i = 0; i < seedText.Length; i++)
        {
            seedText[i].text = seedString[i].ToString();
        }
    }
}
