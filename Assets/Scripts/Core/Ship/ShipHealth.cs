using Unity.Netcode;
using UnityEngine;

public class ShipHealth : NetworkBehaviour
{
    // Health NetworkVariable'ı varsayılan olarak Everyone Read, Server Write olmalı
    public NetworkVariable<int> Health = new NetworkVariable<int>(60);
    [SerializeField] private GameObject explosionPrefab;

    public void TakeDamage(int damage)
    {
        if (!IsServer) return; // Hasar hesabı sadece sunucuda yapılır

        Health.Value -= damage;

        if (Health.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Patlama efektini tüm client'larda göster
        SpawnExplosionClientRpc(transform.position);

        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            // 1. ADIM: Ağdan güvenli bir şekilde çek (false: Netcode objeyi yok etmeye çalışmaz)
            // Bu satır uyarının kaynağını (unexpected behavior riskini) ortadan kaldırır.
            NetworkObject.Despawn(false);

            // 2. ADIM: Şimdi objeyi fiziksel olarak sahneden silebiliriz.
            // Sahneye elle konan objeler için en temiz yöntem budur.
            Destroy(gameObject);
        }
    }

    [ClientRpc]
    private void SpawnExplosionClientRpc(Vector3 pos)
    {
        // Patlama efekti ağ üzerinde senkronize olmasa da olur, görseldir.
        if (explosionPrefab != null) 
            Instantiate(explosionPrefab, pos, Quaternion.identity);
    }
}