using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CameraConfig 
{
    public string camera {  get; set; }

    private string configFilePath;

    public CameraConfig()
    {
        configFilePath = Path.Combine(Application.streamingAssetsPath, "CameraConfig.json");
    }

    public void SaveConfig()
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(configFilePath, json);
        Debug.Log("Configuration saved to " + configFilePath);
    }

    public void LoadConfig()
    {
        if (File.Exists(configFilePath))
        {
            string json = File.ReadAllText(configFilePath);
            JsonConvert.PopulateObject(json, this);
        }
        else
        {
            Debug.LogWarning("Configuration file not found. Using default values.");
        }
    }
}
