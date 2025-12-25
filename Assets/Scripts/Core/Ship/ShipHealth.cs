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
        // Patlama efektini tüm client'larda göster
        SpawnExplosionClientRpc(transform.position);

        if (IsServer && NetworkObject != null)
        {
            if (NetworkObject.IsSpawned)
            {
                // 1. ADIM: Ağdan güvenli bir şekilde çek
                // false: Netcode'un objeyi yok etmeye çalışmasını engeller, sadece ağ kaydını siler.
                NetworkObject.Despawn(false);
            }

            // 2. ADIM: Şimdi objeyi fiziksel olarak sahneden silebiliriz
            // Sahneye elle konan objeler için en temiz ve uyarısız yöntem budur.
            Destroy(gameObject);
        }
    }

    [ClientRpc]
    private void SpawnExplosionClientRpc(Vector3 pos)
    {
        if (explosionPrefab != null) 
            Instantiate(explosionPrefab, pos, Quaternion.identity);
    }
}