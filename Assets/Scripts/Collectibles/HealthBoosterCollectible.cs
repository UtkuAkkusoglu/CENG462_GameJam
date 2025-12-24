using Unity.Netcode;
using UnityEngine;

public class HealthBoosterCollectible : NetworkBehaviour, ICollectible
{
    [SerializeField] private int healthAmount = 25;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return; // Sadece sunucu onaylar

        if (other.CompareTag("Player"))
        {
            Collect(other.gameObject);
            GetComponent<NetworkObject>().Despawn(); // Ağdan sil
        }
    }

    public void Collect(GameObject player)
    {
        if (player.TryGetComponent(out PlayerStats stats))
        {
            // PlayerStats içinde Health NetworkVariable olduğu için doğrudan ekliyoruz
            // Max 100 olacak şekilde sınırlandırma (Clamp)
            stats.Health.Value = Mathf.Min(stats.Health.Value + healthAmount, 100);
            Debug.Log($"[Collectible] Can tazelendi: {stats.Health.Value}");
        }
    }
}