using Unity.Netcode;
using UnityEngine;

public class FireRateBoosterCollectible : NetworkBehaviour, ICollectible
{
    [SerializeField] private float multiplier = 2.0f; // 2 kat hızlı ateş
    [SerializeField] private float duration = 5.0f;  // 5 saniye boyunca

    private void OnTriggerEnter2D(Collider2D other)
    {
        // if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            Collect(other.gameObject);
            GetComponent<NetworkObject>().Despawn();
        }
    }

    public void Collect(GameObject player)
    {
        if (player.TryGetComponent(out PlayerStats stats))
        {
            stats.ApplyFireRateBoost(multiplier, duration);
        }
    }
}