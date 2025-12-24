using Unity.Netcode;
using UnityEngine;

public class CoinCollectible : NetworkBehaviour, ICollectible
{
    [SerializeField] private int scoreAmount = 50; 

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Sadece sunucu (Server) bu işlemi yönetir, yorum satırını kapatınca oyun başlar başlamaz collision çalışıyor
        // if (!IsServer) return; 

        // 2. Çarpan nesnenin Player olup olmadığını kontrol et
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[Coin] Altın toplandı! Toplayan: {other.name}");
            
            Collect(other.gameObject);

            // 3. Objeyi ağ üzerinden yok et (Herkesin ekranından silinir)
            if (NetworkObject.IsSpawned)
            {
                GetComponent<NetworkObject>().Despawn(true); 
            }
        }
    }

    public void Collect(GameObject player)
    {
        if (player.TryGetComponent(out PlayerStats stats))
        {
            stats.AddScore(scoreAmount);
            // Puan artışını konsolda teyit etmek için
            Debug.Log($"[Score Update] {player.name} için skor {scoreAmount} arttı.");
        }
    }
}