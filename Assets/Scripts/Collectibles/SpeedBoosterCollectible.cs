using Unity.Netcode;
using UnityEngine;

public class SpeedBoosterCollectible : NetworkBehaviour, ICollectible
{
    [SerializeField] private float multiplier = 1.5f;
    [SerializeField] private float duration = 5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // if (!IsServer) return; 
        
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[SpeedBooster] Hız artırıcı toplandı! Toplayan: {other.name}");
            Collect(other.gameObject);
            GetComponent<NetworkObject>().Despawn(true); 
        }
    }

    public void Collect(GameObject player)
    {
        if (player.TryGetComponent(out PlayerStats stats))
        {
            stats.ApplySpeedBoost(multiplier, duration);
        }
    }
}