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
        // 1. Sadece sunucu silme yetkisine sahip (Hata almaman için en kritik satır)
        if (!IsServer) return;

        // 2. Işınlanma sırasında (ilk 1 sn) yanlışlıkla toplanmasın
        if (Time.time < spawnTime + 1.0f) return;

        // Çarpışmanın gerçekleştiği konumu alalım
        Vector3 hitPos = transform.position;

        // Ana objenin NetworkObject'ini bul (Paletten çarpsa bile gövdeyi bulur)
        var networkObject = other.GetComponentInParent<NetworkObject>();

        if (networkObject != null && networkObject.IsPlayerObject && other.CompareTag("Player"))
        {
            Debug.Log($"[Coin] {hitPos} konumunda {networkObject.gameObject.name} tarafından toplandı.");
            Collect(networkObject.gameObject);
            
            // Sahne objesi uyarısını engellemek için Despawn(true)
            GetComponent<NetworkObject>().Despawn(true);
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