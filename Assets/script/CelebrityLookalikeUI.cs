using CelebrityLookalike;
using Cysharp.Threading.Tasks;
using PimDeWitte.UnityMainThreadDispatcher;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CelebrityLookalikeUI : MonoBehaviour
{
    // UI References
    [Header("Camera Setup")]
    public RawImage cameraView;
    public AspectRatioFitter aspectRatioFitter;
    public Button captureButton;


    [Header("Results Panel")]
    public GameObject resultsPanel;
    public RawImage capturedImageView;
    public RawImage confirmedImage;

    //public RawImage CelebritiesParent;

    public RawImage celebImageView;

    public TextMeshProUGUI nameText;
    public TextMeshProUGUI similarityText;

    public Button prevButton;
    public Button nextButton;
    public Button closeButton;
    public Button prevResultButton;
    public Button nextResultButton;
    public TextMeshProUGUI resultIndexText;

    [Header("Configuration")]
    public int maxResults = 5;

    [Header("API Configuration")]
    public CelebrityLookalikeAPI apiClient;

    // Optional loading indicator
    public GameObject loadingPanel;


    public CameraConfig cameraConfig;

    // Private variables
    private WebCamTexture webCamTexture;
    private Texture2D capturedImage;
    private List<CelebrityMatch> currentResults;
    private int currentResultIndex = 0;
    private int currentImageIndex = 0;



    public void OnConfirmedCelebrity()
    {
        confirmedImage.texture = celebImageView.texture;
        confirmedImage.GetComponent<AspectRatioFitter>().aspectRatio = celebImageView.GetComponent<AspectRatioFitter>().aspectRatio;
    }
   
    public void InitializeCamera()
    {
        try
        {
            // Initialize and start webcam
            WebCamDevice[] devices = WebCamTexture.devices;

            Debug.Log($"Camera Device Count: {devices.Length}");

            if (devices.Length == 0)
            {
                Debug.LogWarning("No cameras detected!");
                return;
            }


            // Check if the configured camera exists
            bool cameraExists = devices.Any(d => d.name == cameraConfig.camera);
            if (!cameraExists)
            {
                string message = $"Configured camera '{cameraConfig.camera}' not found. Using default camera.";
                Debug.LogWarning(message);

                // Use the first available camera instead
                cameraConfig.camera = devices[0].name;
            }

            // Create the WebCamTexture with proper error handling
            webCamTexture = new WebCamTexture(cameraConfig.camera, 640 , 360 , 30);

            Material newMaterial = new Material(Shader.Find("UI/Default"));
            cameraView.material = newMaterial;
            cameraView.material.mainTexture = webCamTexture;

            webCamTexture.Play();

            capturedImage = new Texture2D((int)webCamTexture.width, (int)webCamTexture.height, TextureFormat.RGB24, false);


            _ = AdjustAspectRatio();
        }
        catch (Exception ex)
        {
           
            Debug.LogError($"Camera error: {ex.Message}");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // Find or create API client if not assigned
        if (apiClient == null)
        {
            apiClient = FindObjectOfType<CelebrityLookalikeAPI>();

            if (apiClient == null)
            {
                apiClient = gameObject.AddComponent<CelebrityLookalikeAPI>();
                Debug.Log("Created new API client");
            }
        }

        // Subscribe to API events
        apiClient.OnResultsReceived += OnAPIResultsReceived;
        apiClient.OnError += OnAPIError;

        // Set up UI
        // SetupCamera();

        cameraConfig = new CameraConfig();
        cameraConfig.LoadConfig();

        InitializeCamera();

        SetupButtons();

        // Hide results panel initially
        resultsPanel.SetActive(false);

        // Hide loading panel if it exists
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }


    private async UniTask AdjustAspectRatio()
    { 
    
        // Set aspect ratio
        float ratio = (float)webCamTexture.width / (float)webCamTexture.height;
        aspectRatioFitter.aspectRatio = ratio;

        // Adjust image rotation
        cameraView.rectTransform.localEulerAngles = new Vector3(0, 0, -webCamTexture.videoRotationAngle);
        await UniTask.Yield();

    }

    private void SetupButtons()
    {
        // Main buttons
        captureButton.onClick.AddListener(CaptureAndSearch);
        closeButton.onClick.AddListener(() => resultsPanel.SetActive(false));

        // Navigation buttons
        prevResultButton.onClick.AddListener(ShowPreviousResult);
        nextResultButton.onClick.AddListener(ShowNextResult);
        prevButton.onClick.AddListener(ShowPreviousImage);
        nextButton.onClick.AddListener(ShowNextImage);
    }

    private void CaptureAndSearch()
    {
        UnityMainThreadDispatcher.Instance().Enqueue(CaptureAndSearchCoroutine());
    }

    private IEnumerator CaptureAndSearchCoroutine()
    {
        // Check if webcam is running
        if (webCamTexture == null || !webCamTexture.isPlaying)
        {
            Debug.LogError("Webcam not running");
            yield break;
        }

        // Disable capture button while processing
        captureButton.interactable = false;

        // Show loading indicator if it exists
        if (loadingPanel != null)
            loadingPanel.SetActive(true);


        // Use a separate frame to start the API call
        yield return new WaitForEndOfFrame();


        Texture2D snapshot = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGB24, false);
        snapshot.SetPixels32(webCamTexture.GetPixels32());
        snapshot.Apply();

        Debug.Log($" {webCamTexture.width} - {webCamTexture.height} ");

        //capturedImage = new Texture2D((int)snapshot.width, (int)snapshot.height, TextureFormat.RGB24, false);
        // Display captured image
        capturedImageView.texture = snapshot;


        AspectRatioFitter aspectFitter = capturedImageView.GetComponent<AspectRatioFitter>();
        if (aspectFitter != null)
        {
            aspectFitter.aspectRatio = (float)capturedImageView.texture.width / capturedImage.height;
        }


        // Call the API with the webcam texture
        apiClient.FindLookalikes( snapshot , maxResults);

        // Note: The rest will be handled by the OnAPIResultsReceived callback
    }

    private void OnAPIResultsReceived(List<CelebrityMatch> results)
    {
        // Hide loading indicator
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        // Store results
        currentResults = results;
        currentResultIndex = 0;
        currentImageIndex = 0;

        if (currentResults.Count > 0)
        {
            // Show results panel
            resultsPanel.SetActive(true);

            // Display first result
            DisplayCurrentResult();
        }
        else
        {
            Debug.LogWarning("No matches found");
        }

        // Re-enable capture button
        captureButton.interactable = true;
    }

    private void OnAPIError(string errorMessage)
    {
        Debug.LogError($"API Error: {errorMessage}");

        // Hide loading indicator
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        // Re-enable capture button
        captureButton.interactable = true;
    }

    private void DisplayCurrentResult()
    {
        if (currentResults.Count == 0 || currentResultIndex >= currentResults.Count)
            return;

        var result = currentResults[currentResultIndex];

        // Update text
        nameText.text = result.DisplayName;
        similarityText.text = $"Similarity: {result.Similarity:P2}";
        resultIndexText.text = $"Match {currentResultIndex + 1} of {currentResults.Count}";

        // Update navigation buttons
        prevResultButton.interactable = currentResultIndex > 0;
        nextResultButton.interactable = currentResultIndex < currentResults.Count - 1;

        // Load celebrity image if available
        LoadCelebrityImage(result);
    }

    private void LoadCelebrityImage(CelebrityMatch match)
    {
        if (match.ImagePaths == null || match.ImagePaths.Length == 0)
        {
            // No images available
            celebImageView.texture = null;
            prevButton.interactable = false;
            nextButton.interactable = false;
            return;
        }

        // Update image navigation
        currentImageIndex = Mathf.Clamp(currentImageIndex, 0, match.ImagePaths.Length - 1);
        prevButton.interactable = currentImageIndex > 0;
        nextButton.interactable = currentImageIndex < match.ImagePaths.Length - 1;

        // Load image
        _=LoadImageCoroutine(match.ImagePaths[currentImageIndex]);
    }

    private async UniTask LoadImageCoroutine(string path)
    {
        // Read file
        try
        {
            byte[] fileData = await File.ReadAllBytesAsync(path);

            // Create texture
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(fileData);

            // Display image
            celebImageView.texture = texture;


            AspectRatioFitter aspectFitter = celebImageView.GetComponent<AspectRatioFitter>();
            if (aspectFitter != null)
            {
                aspectFitter.aspectRatio = (float)texture.width / texture.height;
            }

        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load image from {path}: {ex.Message}");
            celebImageView.texture = null;
        }

        await UniTask.Yield();
    }

    private void ShowPreviousResult()
    {
        if (currentResultIndex > 0)
        {
            currentResultIndex--;
            currentImageIndex = 0;
            DisplayCurrentResult();
        }
    }

    private void ShowNextResult()
    {
        if (currentResultIndex < currentResults.Count - 1)
        {
            currentResultIndex++;
            currentImageIndex = 0;
            DisplayCurrentResult();
        }
    }

    private void ShowPreviousImage()
    {
        if (currentImageIndex > 0)
        {
            currentImageIndex--;
            LoadCelebrityImage(currentResults[currentResultIndex]);
        }
    }

    private void ShowNextImage()
    {
        var match = currentResults[currentResultIndex];
        if (match.ImagePaths != null && currentImageIndex < match.ImagePaths.Length - 1)
        {
            currentImageIndex++;
            LoadCelebrityImage(match);
        }
    }

    // Utility function to rotate textures
    private Texture2D RotateTexture(Texture2D original, float angle)
    {
        // Simple implementation for common rotation angles
        if (angle == 90)
        {
            Texture2D rotated = new Texture2D(original.height, original.width);
            for (int y = 0; y < original.height; y++)
            {
                for (int x = 0; x < original.width; x++)
                {
                    rotated.SetPixel(original.height - y - 1, x, original.GetPixel(x, y));
                }
            }
            rotated.Apply();
            return rotated;
        }
        else if (angle == 180)
        {
            Texture2D rotated = new Texture2D(original.width, original.height);
            for (int y = 0; y < original.height; y++)
            {
                for (int x = 0; x < original.width; x++)
                {
                    rotated.SetPixel(original.width - x - 1, original.height - y - 1, original.GetPixel(x, y));
                }
            }
            rotated.Apply();
            return rotated;
        }
        else if (angle == 270)
        {
            Texture2D rotated = new Texture2D(original.height, original.width);
            for (int y = 0; y < original.height; y++)
            {
                for (int x = 0; x < original.width; x++)
                {
                    rotated.SetPixel(y, original.width - x - 1, original.GetPixel(x, y));
                }
            }
            rotated.Apply();
            return rotated;
        }

        // Return original if no rotation needed
        return original;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (apiClient != null)
        {
            apiClient.OnResultsReceived -= OnAPIResultsReceived;
            apiClient.OnError -= OnAPIError;
        }

        // Clean up
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
        }
    }
}