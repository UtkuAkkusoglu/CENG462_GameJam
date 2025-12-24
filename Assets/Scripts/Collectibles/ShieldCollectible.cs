using Unity.Netcode;
using UnityEngine;

public class ShieldCollectible : NetworkBehaviour, ICollectible
{
    [SerializeField] private float duration = 7.0f;
    private float spawnTime;

    private void Start() => spawnTime = Time.time;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return; // Kritik eksik
        if (Time.time < spawnTime + 1.0f) return;

        var netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsPlayerObject && other.CompareTag("Player"))
        {
            Collect(netObj.gameObject);
            GetComponent<NetworkObject>().Despawn(true);
        }
    }

    public void Collect(GameObject player)
    {
        if (player.TryGetComponent(out PlayerStats stats))
        {
            stats.ApplyShield(duration);
        }
    }
}