using Unity.Netcode;
using UnityEngine;

public class ShipHealth : NetworkBehaviour
{
    public NetworkVariable<int> Health = new NetworkVariable<int>(60);
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private int shipKillScore = 50; // Gemi puanı

    // GÜNCELLEDİK: attackerId parametresi eklendi
    public void TakeDamage(int damage, ulong attackerId) 
    {
        if (!IsServer) return;
        Health.Value -= damage;
        if (Health.Value <= 0) Die(attackerId);
    }

    private void Die(ulong attackerId)
    {
        SpawnExplosionClientRpc(transform.position);

        if (IsServer)
        {
            // PATLATAN KİŞİYE PUAN VER
            AwardPointsToAttacker(attackerId, shipKillScore);

            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }
    }

    private void AwardPointsToAttacker(ulong attackerId, int amount)
    {
        foreach (var player in FindObjectsByType<PlayerStats>(FindObjectsSortMode.None))
        {
            if (player.OwnerClientId == attackerId)
            {
                player.AddScore(amount);
                break;
            }
        }
    }

    [ClientRpc]
    private void SpawnExplosionClientRpc(Vector3 pos)
    {
        if (explosionPrefab != null) Instantiate(explosionPrefab, pos, Quaternion.identity);
    }
}