using Unity.Netcode;
using UnityEngine;

public class SpeedBoosterCollectible : NetworkBehaviour, ICollectible
{
    [SerializeField] private float multiplier = 15f;
    [SerializeField] private float duration = 5f;
    private float spawnTime;

    private void Start() 
    {
        // Doğma zamanını kaydet
        spawnTime = Time.time;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Sadece sunucu objeyi ağdan silebilir
        if (!IsServer) return; 

        // 2. İlk 1 saniye içinde (ışınlanma sırasında) toplamayı engelle
        if (Time.time < spawnTime + 1.0f) return;

        // 3. Palet mi yoksa gövde mi bakmadan ana NetworkObject'i bul
        var networkObject = other.GetComponentInParent<NetworkObject>();

        // 4. Eğer bir oyuncuysa ve tag'i doğruysa işle
        if (networkObject != null && networkObject.IsPlayerObject && other.CompareTag("Player"))
        {
            Debug.Log($"[SpeedBooster] {networkObject.gameObject.name} tarafından toplandı!");
            Collect(networkObject.gameObject);
            
            // Sahne objesi uyarısı almamak için 'true' (destroy) kullanıyoruz
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