using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TankSpawner : NetworkBehaviour
{
    [Header("Ayarlar")]
    [SerializeField] private GameObject tankPrefab;
    [SerializeField] private List<Transform> spawnPoints;

    public override void OnNetworkSpawn()
    {
        // Bu kod Game Scene yüklendiðinde çalýþýr
        if (IsServer)
        {
            // 1. Sonradan baðlananlar için kapýyý açýk tut
            NetworkManager.Singleton.OnClientConnectedCallback += PlayerBaglandi;

            // 2. Sahne yüklendiðinde HALÝHAZIRDA baðlý olan herkesi (Host dahil) spawn et
            // Bu kýsým çok önemli, yoksa Host kendini yaratamaz.
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                SpawnPlayer(clientId);
            }
        }
    }

    private void PlayerBaglandi(ulong clientId)
    {
        SpawnPlayer(clientId);
    }

    private void SpawnPlayer(ulong clientId)
    {
        // Spawn noktasýný seç
        int index = (int)(clientId % (ulong)spawnPoints.Count);
        Transform secilenNokta = spawnPoints[index];

        // Tanký yarat
        GameObject yeniTank = Instantiate(tankPrefab, secilenNokta.position, Quaternion.identity);

        // Aða bildir: "Bu tank þu ID'li oyuncunundur"
        yeniTank.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= PlayerBaglandi;
        }
    }
}