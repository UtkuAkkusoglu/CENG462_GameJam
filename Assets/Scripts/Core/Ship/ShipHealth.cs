using Unity.Netcode;
using UnityEngine;

public class ShipHealth : NetworkBehaviour
{
    public NetworkVariable<int> Health = new NetworkVariable<int>(60);
    [SerializeField] private GameObject explosionPrefab;

    public void TakeDamage(int damage)
    {
        if (!IsServer) return;
        Health.Value -= damage;
        if (Health.Value <= 0) Die();
    }

    private void Die()
    {
        // 1. Önce patlama efektini gönder (Nesne yok olmadan hemen önce)
        SpawnExplosionClientRpc(transform.position);

        if (IsServer && NetworkObject != null)
        {
            if (NetworkObject.IsSpawned)
            {
                // DEĞİŞİKLİK BURADA: 'true' yapıyoruz!
                // Despawn(true) şu anlama gelir: "Ağdan düşür VE tüm oyuncularda bu objeyi yok et."
                NetworkObject.Despawn(true);
            }

            // Destroy(gameObject); <-- BU SATIRA GEREK YOK!
            // Çünkü Despawn(true) zaten objeyi otomatik olarak yok eder.
            // Eğer hem Despawn(true) hem Destroy kullanırsan Unity hata verebilir.
        }
    }

    [ClientRpc]
    private void SpawnExplosionClientRpc(Vector3 pos)
    {
        if (explosionPrefab != null) 
            Instantiate(explosionPrefab, pos, Quaternion.identity);
    }
}