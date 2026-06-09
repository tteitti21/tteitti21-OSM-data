using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

[System.Serializable]
public class TemperatureResponse
{
    public string station_name;
    public float temperature;
}

public class AirTemperatureFetcher : MonoBehaviour
{
    [SerializeField] public TMP_Text uiText;
    [SerializeField] public float lat;
    [SerializeField] public float lon;
    [SerializeField] public string flaskUrl;

    void Start()
    {
        StartCoroutine(FetchAirTemperature());
    }

    [System.Serializable]
    public class Coordinates
    {
        public float lat;
        public float lon;

        public Coordinates(float latitude, float longitude)
        {
            lat = latitude;
            lon = longitude;
        }
    }


    IEnumerator FetchAirTemperature()
    {
        Coordinates coords = new Coordinates(lat, lon);
        string json = JsonUtility.ToJson(coords);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(flaskUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                TemperatureResponse response = JsonUtility.FromJson<TemperatureResponse>(www.downloadHandler.text);
                if (uiText != null)
                    uiText.text = $"Ilman lämpötila: {response.temperature} °C\n Hakuasema: {response.station_name}";
            }
            else
            {
                uiText.text = "Error fetching temperature";
                Debug.LogError(www.error);
            }
        }
    }
}
