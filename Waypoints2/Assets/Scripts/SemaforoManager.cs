using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SemaforoManager : MonoBehaviour
{
    public GameObject gameCanvas;
    private List<PlayerData> playersArray;
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

    [Header("Stoplight Blocks")]
    public List<GameObject> stoplightBlocks = new List<GameObject>(); // Assign in Inspector

    private int currentPhase = 0;
    public float phaseDuration = 3f; // Duration of each phase in seconds

    private Coroutine phaseRoutine;

    #region Base Functions

    void Awake()
    {
        CreatePlayersArray();
        gameCanvas = GameObject.Find("GameCanvas");
    }

    void Start()
    {
        StartCoroutine("InstantiatePlayers");
        phaseRoutine = StartCoroutine(StoplightPhaseRoutine());
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
        AICarSemaforoScript raceCarAI;

        if (playerCreated < playersArray.Count)
        {
            raceCar = Instantiate(Resources.Load("SemaforoCar") as GameObject);
            raceCar.transform.position = startingPoint.transform.position;
            raceCarAI = raceCar.GetComponent<AICarSemaforoScript>();
            raceCarAI.playerName = playersArray[playerCreated].name;
            raceCarAI.velocity = playersArray[playerCreated].velocity;
            raceCarAI.bodyColor = playersArray[playerCreated].bodyColor;
            raceCarAI.gameManager = gameObject.GetComponent<SemaforoManager>();

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

    IEnumerator StoplightPhaseRoutine()
    {
        while (true)
        {
            SetStoplightPhase(currentPhase);
            yield return new WaitForSeconds(phaseDuration);
            currentPhase = (currentPhase + 1) % stoplightBlocks.Count;
        }
    }

    void SetStoplightPhase(int phase)
    {
        for (int i = 0; i < stoplightBlocks.Count; i++)
        {
            var collider = stoplightBlocks[i].GetComponent<Collider>();
            var renderer = stoplightBlocks[i].GetComponent<Renderer>();
            bool isInactive = (i == phase); // Only current phase pillar disappears

            if (collider != null)
                collider.enabled = !isInactive;

            if (renderer != null)
                renderer.enabled = !isInactive;
        }
    }

    public void CheckConditions(GameObject car, int lap, int remainingNode)
    {
        if (lap >= lapsToComplete)
        {
            CleanGameElements();
        }
    }

    void CleanGameElements()
    {
        // Implement car cleanup logic if needed
    }

    public void ResetGame()
    {
        Debug.Log("Reset Function");
        CreatePlayersArray();
        Debug.Log("Re-creating Assets");
        StartCoroutine("InstantiatePlayers");
    }

    #endregion

    void Update()
    {
        // No leaderboard update needed
    }

    void OnDisable()
    {
        if (phaseRoutine != null)
            StopCoroutine(phaseRoutine);
    }
}
