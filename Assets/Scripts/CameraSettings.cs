using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CameraSettings : MonoBehaviour
{
    private CameraConfig CamConfig;
    public TMP_Dropdown cameraDropdown;
    public WebCamDevice[] devices;


    public GameObject app;
    public GameObject camSettings;

    private void OnEnable()
    {
        CamConfig =  new CameraConfig();
        devices = WebCamTexture.devices;

        cameraDropdown.ClearOptions();

        cameraDropdown.onValueChanged.AddListener(OnCameraSelected);

        FillCameras();
    }

    public void LoadConfig()
    {
        CamConfig.LoadConfig();

        FillCameras();
    }
    public void OnCameraSelected(int index)
    {
        if (devices.Length > 0 && index >= 0 && index < devices.Length)
        {
            string selectedCamera = devices[index].name;
            Debug.Log("Selected camera: " + selectedCamera);
        }
    }
    public void FillCameras()
    {
        if (string.IsNullOrEmpty(CamConfig.camera))
        {
            // fill from cameras
            foreach (WebCamDevice device in devices)
            {

                if (!device.name.ToLower().Contains("virtual"))
                {
                    cameraDropdown.options.Add(new TMP_Dropdown.OptionData(device.name));
                }
            }

            cameraDropdown.value = 0;
            cameraDropdown.RefreshShownValue();
        }
        else
        {
            foreach (WebCamDevice device in devices)
            {
                if (!device.name.ToLower().Contains("virtual"))
                {
                    cameraDropdown.options.Add(new TMP_Dropdown.OptionData(device.name));
                }
            }

            SelectCameraByName(CamConfig.camera);
        }

       
    }
    public void SelectCameraByName(string cameraName)
    {
        Debug.Log("Select the one in the config ");
        int index = cameraDropdown.options.FindIndex(option => option.text == cameraName);

        Debug.Log($"index {index}");

        if (index != -1)
        {
            cameraDropdown.value = index;  // Set the dropdown value to the found index
            cameraDropdown.RefreshShownValue();  // Refresh the dropdown to show the selected camera
            OnCameraSelected(index);  // Optionally call the camera select method
        }
        else
        {
            Debug.LogWarning("Camera not found: " + cameraName);
        }
    }

    public void SaveConfig()
    {
        // Save the Camera
        int selectedIndex = cameraDropdown.value; // Get the selected index
        string selectedOption = cameraDropdown.options[selectedIndex].text; // Get the option text
        CamConfig.camera = selectedOption;
        CamConfig.SaveConfig();

        camSettings.SetActive(false);
        app.SetActive(true);
        
    }

}
