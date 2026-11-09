using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Field names are snake_case to match the Python server's JSON (JsonUtility maps names exactly)
[Serializable]
public class ShopItemData
{
    public string name;
    public int price;
    public int stock;
}

[Serializable]
public class SpawnRequestData
{
    public string character_id;
    public ShopItemData[] shop_items;
}

[Serializable]
public class OfferRequestData
{
    public string item;
    public int price;
    public ShopItemData[] shop_items;
}

[Serializable]
public class CustomerData
{
    public string session_id;
    public string character_id;
    public string name;
    public string color;
    public string wants;
    public int satisfaction;
    public string status;
    public int offers_left;
    public string say; // What the customer just said (greeting, reaction, or ban line)
}

[Serializable]
public class OfferResponseData : CustomerData
{
    public string outcome; // accept | counter | reject | wrong_item
    public int counter_offer;
    public bool sale;
    public string sale_item;
    public int sale_price;
    public bool leaving;
    public bool ai_online;
}

[Serializable]
public class HealthData
{
    public string status;
    public string model;
    public bool ollama_reachable;
    public bool model_installed;
}

// Talks to the Python AI server (AI-server/server.py) over HTTP
public class AIServerClient : MonoBehaviour
{
    public static AIServerClient Instance { get; private set; }

    [SerializeField] private string serverUrl = "http://127.0.0.1:8000";
    [SerializeField] private int timeoutSeconds = 60;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public void CheckHealth(Action<HealthData> onSuccess, Action<string> onError) =>
        StartCoroutine(Send("GET", "/health", null, onSuccess, onError));

    public void SpawnCustomer(string characterId, ShopItemData[] shopItems, Action<CustomerData> onSuccess, Action<string> onError)
    {
        var body = new SpawnRequestData { character_id = characterId ?? "", shop_items = shopItems };
        StartCoroutine(Send("POST", "/customers/spawn", JsonUtility.ToJson(body), onSuccess, onError));
    }

    public void MakeOffer(string sessionId, string item, int price, ShopItemData[] shopItems, Action<OfferResponseData> onSuccess, Action<string> onError)
    {
        var body = new OfferRequestData { item = item, price = price, shop_items = shopItems };
        StartCoroutine(Send("POST", $"/customers/{sessionId}/offer", JsonUtility.ToJson(body), onSuccess, onError));
    }

    public void BanCustomer(string sessionId, Action<CustomerData> onSuccess, Action<string> onError) =>
        StartCoroutine(Send("POST", $"/customers/{sessionId}/ban", "{}", onSuccess, onError));

    // Fire and forget - tells the server to forget a customer that left
    public void RemoveCustomer(string sessionId) =>
        StartCoroutine(Send<CustomerData>("DELETE", $"/customers/{sessionId}", null, null, null));

    private IEnumerator Send<T>(string method, string path, string json, Action<T> onSuccess, Action<string> onError)
    {
        using var request = new UnityWebRequest(serverUrl + path, method);
        if (json != null)
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.SetRequestHeader("Content-Type", "application/json");
        }
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = timeoutSeconds;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            string error = $"{method} {path} failed: {request.error} {request.downloadHandler.text}";
            if (onError != null) onError(error);
            else Debug.LogWarning($"[AI] {error}");
            yield break;
        }

        onSuccess?.Invoke(JsonUtility.FromJson<T>(request.downloadHandler.text));
    }
}
