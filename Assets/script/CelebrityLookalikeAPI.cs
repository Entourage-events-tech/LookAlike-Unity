using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;


namespace CelebrityLookalike
{
    /// <summary>
    /// API client for communicating with the Celebrity Lookalike Flask API
    /// </summary>
    public class CelebrityLookalikeAPI : MonoBehaviour
    {
        [Header("API Configuration")]
        [SerializeField] private string apiUrl = "http://localhost:5000/api";

        [Header("Debug")]
        [SerializeField] private bool logResponses = false;

        // Event fired when results are received
        public event Action<List<CelebrityMatch>> OnResultsReceived;
        public event Action<string> OnError;

        /// <summary>
        /// Make a request to the API with an image
        /// </summary>
        public void FindLookalikes(byte[] imageData, string fileName, int topK = 5)
        {
             _ = UploadImage(imageData, fileName, topK) ;
        }

        /// <summary>
        /// Find look-alikes using a webcam texture
        /// </summary>

        public void FindLookalikes(Texture2D webcamTexture, int topK = 5)
        {
            //if (webcamTexture == null || !webcamTexture.isPlaying)
            //{
            //    OnError?.Invoke("Webcam not active");
            //    return;
            //}

            // Create a texture to hold the webcam image
            


            // Convert to JPG (not PNG) which will ensure proper RGB format
            byte[] imageData = webcamTexture.EncodeToJPG(100);

            FindLookalikes(imageData, "webcam.jpg", topK);
        }

        private async UniTask UploadImage(byte[] imageData, string fileName, int topK)
        {
            // Create form
            WWWForm form = new WWWForm();
            form.AddBinaryData("file", imageData, fileName, "image/jpeg");
            form.AddField("top_k", topK.ToString());

            // Send request
            using (UnityWebRequest www = UnityWebRequest.Post($"{apiUrl}/upload_lookalike", form))
            {
                await www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"API Error: {www.error}");
                    OnError?.Invoke(www.error);
                }
                else
                {
                    string response = www.downloadHandler.text;

                    if (logResponses)
                        Debug.Log($"API Response: {response}");

                    try
                    {
                        // Parse JSON response
                        LookalikeResponse apiResponse = JsonUtility.FromJson<LookalikeResponse>(response);

                        if (apiResponse != null && apiResponse.success && apiResponse.results != null)
                        {
                            // Convert API results to CelebrityMatch objects
                            List<CelebrityMatch> results = new List<CelebrityMatch>();

                            foreach (var match in apiResponse.results)
                            {
                                results.Add(new CelebrityMatch
                                {
                                    Name = match.name,
                                    Similarity = match.similarity / 100f,  // Convert to 0-1 scale
                                    ImagePath = match.image_path
                                });
                            }

                            // Fire event with results
                            OnResultsReceived?.Invoke(results);
                        }
                        else
                        {
                            OnError?.Invoke("No matches found or invalid response");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error parsing response: {ex.Message}");
                        OnError?.Invoke($"Error parsing response: {ex.Message}");
                    }
                }
            }

            await UniTask.Yield();
        }

        // JSON Response Classes
        [Serializable]
        private class LookalikeResponse
        {
            public bool success;
            public List<ApiCelebrityMatch> results;
            public float search_time;
            public FaceDetection face_detected;
        }

        [Serializable]
        private class FaceDetection
        {
            public float confidence;
            public FaceRegion region;
        }

        [Serializable]
        private class FaceRegion
        {
            public int h;
            public int w;
            public int x;
            public int y;
            public int[] left_eye;
            public int[] right_eye;
        }

        [Serializable]
        private class ApiCelebrityMatch
        {
            public string name;
            public float similarity;
            public string image_path;
        }
    }

    /// <summary>
    /// Represents a celebrity match with compatible properties
    /// </summary>
    [Serializable]
    public class CelebrityMatch
    {
        public string Name;
        public float Similarity;
        public string ImagePath;

        // Properties expected by the UI
        public string DisplayName => Name;
        public string[] ImagePaths => new string[] { ImagePath };
    }
}