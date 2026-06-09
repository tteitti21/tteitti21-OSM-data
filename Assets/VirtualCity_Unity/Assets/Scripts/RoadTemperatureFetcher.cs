using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

[System.Serializable]
public class SensorValue
{
    public string name;
    public float value;
    public string sensorUnit;
    public string measuredTime;
}

[System.Serializable]
public class StationData
{
    public int id;
    public string name;
    public SensorValue[] sensorValues;
}

[System.Serializable]
public class Geometry
{
    public string type;
    public float[] coordinates; // [lon, lat, alt]
}

[System.Serializable]
public class Properties
{
    public int id;
    public string name;
    public string collectionStatus;
    public string state;
    public string dataUpdatedTime;
}

[System.Serializable]
public class Feature
{
    public string type;
    public int id;
    public Geometry geometry;
    public Properties properties;
}

[System.Serializable]
public class StationsResponse
{
    public Feature[] features;
}

public class RoadTemperatureFetcher : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] public TMP_Text uiText;

    [Header("Settings")]
    [SerializeField] public float targetLat;
    [SerializeField] public float targetLon;

    private string baseUrl = "https://tie.digitraffic.fi/api/weather/v1/stations";

    void Start()
    {
        StartCoroutine(GetNearestStation());
    }

    IEnumerator GetNearestStation()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(baseUrl))
        {
            www.SetRequestHeader("Digitraffic-User", "MyUnityApp/1.0");
            www.SetRequestHeader("Accept-Encoding", "gzip");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                StationsResponse stations = JsonUtility.FromJson<StationsResponse>(json);

                if (stations.features.Length == 0)
                {
                    Debug.LogWarning("No stations found.");
                    yield break;
                }

                // Find nearest station
                Feature nearest = null;
                float minDist = float.MaxValue;

                foreach (var f in stations.features)
                {
                    float lon = f.geometry.coordinates[0];
                    float lat = f.geometry.coordinates[1];
                    float dist = HaversineDistance(lat, lon, targetLat, targetLon);

                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = f;
                    }
                }

                if (nearest != null)
                {
                    //Debug.Log($"Nearest station: {nearest.properties.name} (id: {nearest.id})");
                    StartCoroutine(GetStationData(nearest.id, nearest.properties.name));
                }
            }
            else
            {
                Debug.LogError("Station fetch error: " + www.error);
            }
        }
    }

    IEnumerator GetStationData(int id, string stationName)
    {
        string url = baseUrl + "/" + id + "/data";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("Digitraffic-User", "MyUnityApp/1.0");
            www.SetRequestHeader("Accept-Encoding", "gzip");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                StationData station = JsonUtility.FromJson<StationData>(json);

                if (station != null && station.sensorValues != null)
                {
                    foreach (var sensor in station.sensorValues)
                    {
                        if (sensor.name == "ILMA")
                        {
                            string text = $"Ilman lämpötila: {sensor.value} °C. Mittausasema: {stationName}";
                            if (uiText != null)
                                uiText.text = text;
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("Data fetch error: " + www.error);
                if (uiText != null) uiText.text = "Virhe haettaessa dataa.";
            }
        }
    }

    // Haversine distance in km
    private float HaversineDistance(float lat1, float lon1, float lat2, float lon2)
    {
        float R = 6371f; // km
        float dLat = Mathf.Deg2Rad * (lat2 - lat1);
        float dLon = Mathf.Deg2Rad * (lon2 - lon1);
        float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
                  Mathf.Cos(Mathf.Deg2Rad * lat1) * Mathf.Cos(Mathf.Deg2Rad * lat2) *
                  Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);
        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        return R * c;
    }
}
