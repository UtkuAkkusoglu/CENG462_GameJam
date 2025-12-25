using Unity.Netcode;
using UnityEngine;

public class CoinCollectible : NetworkBehaviour, ICollectible
{
    [SerializeField] private int scoreAmount = 50; 
    private float spawnTime;

    private void Start()
    {
        // Objenin yaratıldığı anı kaydet (0,0 bug'ını engellemek için)
        spawnTime = Time.time;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Sunucu değilsek hiçbir fiziksel/mantıksal işlem yapma
        if (!IsServer) return; 

        // Işınlanma sırasında (ilk 1 sn) yanlışlıkla toplanmasın
        if (Time.time < spawnTime + 1.0f) return;

        // Sadece Player tag'ine sahip objeleri kontrol et
        if (other.CompareTag("Player"))
        {
            var networkObject = other.GetComponentInParent<NetworkObject>();

            // Eğer çarpan bir Player objesiyse işlemi yap
            if (networkObject != null && networkObject.IsPlayerObject)
            {
                // PlayerStats bileşenini bul ve puanı ekle
                if (networkObject.TryGetComponent<PlayerStats>(out var stats))
                {
                    stats.AddScore(scoreAmount); // Bu metot IsServer kontrolü içeriyor
                    
                    // Obje ağdan silindiğinde tüm clientlarda yok olur
                    GetComponent<NetworkObject>().Despawn(true);
                }
            }
        }
    }

    public void Collect(GameObject player)
    {
        if (player.TryGetComponent(out PlayerStats stats))
        {
            stats.AddScore(scoreAmount);
            Debug.Log($"[Score Update] {player.name} için skor {scoreAmount} arttı.");
        }
    }
}