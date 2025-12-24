using Unity.Netcode;
using UnityEngine;

public class ShieldCollectible : NetworkBehaviour, ICollectible
{
    [SerializeField] private float duration = 7.0f; // Kalkan 7 saniye sürsün

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
            stats.ApplyShield(duration);
        }
    }
}