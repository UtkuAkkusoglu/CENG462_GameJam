using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TankSpawner : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject tankPrefab;
    [SerializeField] private List<Transform> spawnPoints;

    public override void OnNetworkSpawn()
    {
        // Bu kod Game Scene yüklendiğinde çalışır
        if (IsServer)
        {
            // 1. Sonradan bağlananlar için kapıyı açık tut
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;

            // 2. Sahne yüklendiğinde HALİHAZIRDA bağlı olan herkesi (Host dahil) spawn et
            // Bu kısım çok önemli, yoksa Host kendini yaratamaz
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                SpawnPlayer(clientId);
            }
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        SpawnPlayer(clientId);
    }

    private void SpawnPlayer(ulong clientId)
    {
        // Spawn noktasını seç
        int spawnIndex = (int)(clientId % (ulong)spawnPoints.Count);
        Transform selectedSpawnPoint = spawnPoints[spawnIndex];

        // Tankı yarat
        GameObject newTank = Instantiate(
            tankPrefab,
            selectedSpawnPoint.position,
            Quaternion.identity
        );

        // Ağa bildir: "Bu tank şu ID'li oyuncunundur"
        newTank.GetComponent<NetworkObject>()
               .SpawnAsPlayerObject(clientId, true);
    }

    public override void OnNetworkDespawn()
    {
        // Server kapanırken event aboneliğini temizle
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        }
    }
}
