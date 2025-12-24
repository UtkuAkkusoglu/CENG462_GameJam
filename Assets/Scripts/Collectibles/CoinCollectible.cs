using Unity.Netcode;
using UnityEngine;

public class CoinCollectible : NetworkBehaviour, ICollectible
{
    [SerializeField] private int scoreAmount = 50; 

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Çarpışmanın gerçekleştiği konumu alalım
        Vector3 hitPos = transform.position;

        // 1. Şartlara bakmadan her teması loglayalım (Debugging için)
        Debug.Log($"[Fizik] Temas Algılandı! Yer: {hitPos}, Çarpan: {other.name}, Tag: {other.tag}");

        if (!IsServer) return;

        var networkObject = other.GetComponentInParent<NetworkObject>();

        if (networkObject != null && networkObject.IsPlayerObject && other.CompareTag("Player"))
        {
            Debug.Log($"[Coin] {hitPos} konumunda {networkObject.gameObject.name} tarafından toplandı.");
            Collect(networkObject.gameObject);
            GetComponent<NetworkObject>().Despawn(true);
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