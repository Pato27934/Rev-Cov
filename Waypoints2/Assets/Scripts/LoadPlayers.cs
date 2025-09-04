using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.RestService;
using System.IO;
using System.Text;
using UnityEngine;

public class LoadPlayers
{
    public int numberOfLaps = 0;
    public float miliSecondsDelay = 0.0f;
    public List<PlayerData> playersArray = new List<PlayerData>();

    private string ReadFile(string file)
    {
        string fileContent = "";
        string filePath = Application.dataPath + file;

        StreamReader fileReader = new StreamReader(filePath, Encoding.Default);
        fileContent = fileReader.ReadToEnd();
        fileReader.Close();

        return fileContent;
    }

    public void configGame(string file, int numberOfCars)
    {
        GameConfigObj gameConfigObj = JsonUtility.FromJson<GameConfigObj>(ReadFile(file));

        numberOfLaps = gameConfigObj.GameConfiguration.lapsNumber;
        miliSecondsDelay = gameConfigObj.GameConfiguration.playersInstantiationDelay;

        for (int i = 0; i < numberOfCars; i++)
        {
            PlayerData playerData = new PlayerData();
            Color color = new Color();
            //int index = Random.Range(0, gameConfigObj.Players.Count);
            int index = i % gameConfigObj.Players.Count;

            playerData.name = gameConfigObj.Players[index].Name;
            playerData.velocity = gameConfigObj.Players[index].Velocity;
            ColorUtility.TryParseHtmlString(gameConfigObj.Players[index].Color, out color);
            playerData.bodyColor = color;

            playersArray.Add(playerData);
        }
    }
}
