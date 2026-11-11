using System.Collections.Generic;
using UnityEngine;

// Periodically asks the AI server for a new customer and spawns them at the door
public class CustomerSpawner : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private Customer customerPrefab;      // Optional - a placeholder square is used if empty
    [SerializeField] private float firstSpawnDelay = 2f;
    [SerializeField] private float spawnInterval = 25f;
    [SerializeField] private int maxCustomers = 3;
    [SerializeField] private string[] characterIds;        // Leave empty for random characters from the server

    [Header("Positions")]
    [SerializeField] private Transform exitPoint;          // Where customers enter/leave - defaults to this object
    [SerializeField] private Vector2 standAreaCenter = new Vector2(0f, 2f); // Relative to this object
    [SerializeField] private Vector2 standAreaSize = new Vector2(6f, 1.5f);

    private readonly List<Customer> customers = new List<Customer>();
    private float timer;
    private bool serverReady;
    private bool waitingOnServer;

    // Check the server is up before spawning anyone
    private void Start()
    {
        timer = spawnInterval - firstSpawnDelay;
        if (AIServerClient.Instance == null)
        {
            Debug.LogError("[AI] No AIServerClient in the scene - add one to use CustomerSpawner.");
            enabled = false;
            return;
        }
        AIServerClient.Instance.CheckHealth(
            health =>
            {
                serverReady = true;
                if (!health.ollama_reachable) Debug.LogWarning("[AI] Server is up but can't reach Ollama - run `ollama serve`.");
                else if (!health.model_installed) Debug.LogWarning($"[AI] Ollama model missing - run `ollama pull {health.model}`.");
                else Debug.Log($"[AI] Connected to AI server ({health.model}).");
            },
            error => Debug.LogWarning($"[AI] AI server not reachable, customers won't spawn. Start it with `python server.py` in AI-server/. ({error})"));
    }

    private void Update()
    {
        if (!serverReady || waitingOnServer) return;
        customers.RemoveAll(c => c == null);
        if (customers.Count >= maxCustomers) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnCustomer();
        }
    }

    private void SpawnCustomer()
    {
        string characterId = characterIds != null && characterIds.Length > 0
            ? characterIds[Random.Range(0, characterIds.Length)]
            : "";
        ShopItemData[] items = ShopManager.Instance != null ? ShopManager.Instance.GetShopItems() : null;

        waitingOnServer = true;
        AIServerClient.Instance.SpawnCustomer(characterId, items,
            data =>
            {
                waitingOnServer = false;
                Vector3 exit = exitPoint != null ? exitPoint.position : transform.position;
                Customer customer = customerPrefab != null
                    ? Instantiate(customerPrefab, exit, Quaternion.identity)
                    : CreatePlaceholderCustomer(exit);
                customer.Init(data, RandomStandPosition(), exit);
                customers.Add(customer);
            },
            error =>
            {
                waitingOnServer = false;
                Debug.LogWarning($"[AI] Couldn't spawn customer: {error}");
            });
    }

    private Vector3 RandomStandPosition()
    {
        Vector2 offset = standAreaCenter + new Vector2(
            Random.Range(-standAreaSize.x, standAreaSize.x) / 2f,
            Random.Range(-standAreaSize.y, standAreaSize.y) / 2f);
        return transform.position + (Vector3)offset;
    }

    // Coloured square on the Interactable layer, used until there's real customer art
    private Customer CreatePlaceholderCustomer(Vector3 position)
    {
        var go = new GameObject("Customer");
        go.transform.position = position;
        go.layer = LayerMask.NameToLayer("Interactable");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        sr.sortingOrder = 5;

        var col = go.AddComponent<BoxCollider2D>();
        var colInteract = go.AddComponent<CircleCollider2D>();
        colInteract.isTrigger = true;
        colInteract.radius = 1.5f;

        return go.AddComponent<Customer>();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + (Vector3)standAreaCenter, standAreaSize);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(exitPoint != null ? exitPoint.position : transform.position, 0.3f);
    }
}
