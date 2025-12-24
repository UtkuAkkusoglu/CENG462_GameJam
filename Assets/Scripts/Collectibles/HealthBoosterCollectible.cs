using Unity.Netcode;
using UnityEngine;

public class HealthBoosterCollectible : NetworkBehaviour, ICollectible
{
    [SerializeField] private int healthAmount = 25;
    private float spawnTime;

    private void Start() => spawnTime = Time.time;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Önemli: Sadece sunucu despawn yapabilir!
        if (!IsServer) return; 
        if (Time.time < spawnTime + 1.0f) return; // Başlangıçtaki (0,0) bug'ı önlemi

        var netObj = other.GetComponentInParent<NetworkObject>();
        
        if (netObj != null && netObj.IsPlayerObject && other.CompareTag("Player"))
        {
            // PLAYERSTATS BİLEŞENİNİ AL VE CANI KONTROL ET
            if (netObj.TryGetComponent(out PlayerStats stats))
            {
                // EĞER CANI ZATEN 100 İSE TOPLAMA (return yap)
                if (stats.Health.Value >= 100) 
                {
                    Debug.Log($"[HealthBooster] {netObj.name} canı zaten full, paket bırakıldı.");
                    return; 
                }

                // CANI 100'DEN AZ İSE TOPLA
                Collect(netObj.gameObject);
                GetComponent<NetworkObject>().Despawn(true);
            }
        }
    }

    public void Collect(GameObject player)
    {
        if (player.TryGetComponent(out PlayerStats stats))
        {
            stats.Health.Value = Mathf.Min(stats.Health.Value + healthAmount, 100);
            Debug.Log($"[Collectible] Can tazelendi: {stats.Health.Value}");
        }
    }
}