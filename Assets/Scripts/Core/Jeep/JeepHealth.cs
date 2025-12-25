using Unity.Netcode;
using UnityEngine;

public class JeepHealth : NetworkBehaviour
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
                // DEÐÝÞÝKLÝK BURADA: 'true' yapýyoruz!
                // Despawn(true) þu anlama gelir: "Aðdan düþür VE tüm oyuncularda bu objeyi yok et."
                NetworkObject.Despawn(true);
            }

            // Destroy(gameObject); <-- BU SATIRA GEREK YOK!
            // Çünkü Despawn(true) zaten objeyi otomatik olarak yok eder.
            // Eðer hem Despawn(true) hem Destroy kullanýrsan Unity hata verebilir.
        }
    }

    [ClientRpc]
    private void SpawnExplosionClientRpc(Vector3 pos)
    {
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, pos, Quaternion.identity);
    }
}