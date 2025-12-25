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

        if (Health.Value <= 0)
        {
            SpawnExplosionClientRpc(transform.position);
            GetComponent<NetworkObject>().Despawn(); // Gemiyi yok et
        }
    }

    [ClientRpc]
    private void SpawnExplosionClientRpc(Vector3 pos)
    {
        if (explosionPrefab != null) Instantiate(explosionPrefab, pos, Quaternion.identity);
    }
}