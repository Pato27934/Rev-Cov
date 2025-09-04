using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject gameCanvas;
    private List<PlayerData> playersArray;
    public PlayerInfoUI[] playerInfoUIs;
    private LoadPlayers loader = new LoadPlayers();
    private int playerCreated = 0;

    [Header("Cars Starting Point")]
    public GameObject startingPoint;

    [Header("Game Management")]
    public string filepath = "/Scripts/Utils/data.txt";
    public int numberOfCars = 4;

    [Header("Relevant Info")]
    public int lapsToComplete = 0;
    public float spawnTime = 0.0f;

    [Header("Leader Board")]
    public List<GameObject> carObjects = new List<GameObject>();
    public List<GameObject> carInfoObjects = new List<GameObject>();
    public GameObject resetCanvas;
    public TextMeshProUGUI winner;
    public GameObject playerInfoUIPrefab; // Assign your PlayerInfoUI prefab in the Inspector
    public Transform playersInfoPanel;    // Assign the parent panel (playersinfo) in the Inspector

    #region Base Functions

    void Awake()
    {
        CreatePlayersArray();
        gameCanvas = GameObject.Find("GameCanvas");
    }

    void Start()
    {
        StartCoroutine("InstantiatePlayers");
    }

    #endregion

    #region Game Logic Functions

    void CreatePlayersArray()
    {
        loader.configGame(filepath, numberOfCars);
        lapsToComplete = loader.numberOfLaps;
        spawnTime = loader.miliSecondsDelay / 1000;
        playersArray = loader.playersArray;
    }

    IEnumerator InstantiatePlayers()
    {
        GameObject raceCar;
        AICarScript raceCarAI;

        //GameObject playerCarData;
        //PlayerInfoUI playerCarDataUI;

        if (playerCreated < playersArray.Count)
        {
            raceCar = Instantiate(Resources.Load("SkyCar") as GameObject);
            raceCar.transform.position = startingPoint.transform.position;
            raceCarAI = raceCar.GetComponent<AICarScript>();
            raceCarAI.playerName = playersArray[playerCreated].name;
            raceCarAI.velocity = playersArray[playerCreated].velocity;
            raceCarAI.bodyColor = playersArray[playerCreated].bodyColor;
            raceCarAI.gameManager = gameObject.GetComponent<GameManager>();
            GameObject uiPanel = Instantiate(playerInfoUIPrefab, playersInfoPanel);
            PlayerInfoUI playerInfoUI = uiPanel.GetComponent<PlayerInfoUI>();
            raceCarAI.carInfoUI = playerInfoUI;
            carInfoObjects.Add(uiPanel);

            playerInfoUI.UpdateInfo(
                playersArray[playerCreated].name,
                playersArray[playerCreated].velocity,
                playersArray[playerCreated].bodyColor,
                0 // laps start at 0
            );

            carObjects.Add(raceCar);

            playerCreated++;

            yield return new WaitForSeconds(spawnTime);
            StartCoroutine("InstantiatePlayers");
        }
        else
        {
            playerCreated = 0;
            yield return new WaitForSeconds(0.000001f);
        }
    }

    void UpdatePlayerInfoPanelOrder()
    {
        // Build a list of tuples: (car, carInfoPanel, progress)
        var carProgressList = new List<(AICarScript car, GameObject panel, int laps, int remainingNodes)>();

        for (int i = 0; i < carObjects.Count; i++)
        {
            var carScript = carObjects[i].GetComponent<AICarScript>();
            int laps = carScript.lapsCompleted;
            int remaining = carScript.remainingNodes;
            carProgressList.Add((carScript, carInfoObjects[i], laps, remaining));
        }

        // Sort: first by laps (descending), then by remaining nodes (ascending)
        carProgressList.Sort((a, b) =>
        {
            int lapCompare = b.laps.CompareTo(a.laps); // more laps first
            if (lapCompare != 0) return lapCompare;
            return a.remainingNodes.CompareTo(b.remainingNodes); // fewer nodes first
        });

        // Reorder panels in the UI
        for (int i = 0; i < carProgressList.Count; i++)
        {
            carProgressList[i].panel.transform.SetSiblingIndex(i);
        }
    }

    public void CheckConditions(GameObject car, int lap, int remainingNode)
    {
        if (lap >= lapsToComplete)
        {
            CleanGameElements();
            winner.text += "Winner!!!\n" + car.GetComponent<AICarScript>().playerName;
            resetCanvas.SetActive(true);
        }
    }

    void CleanGameElements()
    {
        foreach (GameObject carObject in carObjects)
        {
            Destroy(carObject);
        }

        carObjects.Clear();

        foreach (GameObject carInfoObject in carInfoObjects)
        {
            Destroy(carInfoObject);
        }

        carInfoObjects.Clear();
    }

    public void ResetGame()
    {
        Debug.Log("Reset Function");
        CreatePlayersArray();
        resetCanvas.SetActive(false);
        Debug.Log("Re-creating Assets");
        StartCoroutine("InstantiatePlayers");
    }

    #endregion

    void Update()
    {
        UpdatePlayerInfoPanelOrder();
    }
}
