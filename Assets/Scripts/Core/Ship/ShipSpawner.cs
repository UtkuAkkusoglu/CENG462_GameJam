using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemyShipSpawner : NetworkBehaviour
{
    [Header("Gemi Ayarları")]
    [SerializeField] private GameObject shipPrefab;
    [SerializeField] private int maxShipsOnMap = 6; // Başlangıçta 6 tane
    [SerializeField] private float respawnDelay = 20f; // Ölünce 20 saniye bekle

    private List<GameObject> activeShips = new List<GameObject>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        StartCoroutine(InitialSpawnRoutine());
    }

    private IEnumerator InitialSpawnRoutine()
    {
        yield return new WaitForSeconds(1f); // Sahnenin yüklenmesini bekle
        
        // Başlangıçta haritayı 6 gemi ile doldur
        for (int i = 0; i < maxShipsOnMap; i++)
        {
            SpawnShip();
        }

        // Ölen gemileri takip edip respawn eden sürekli döngü
        while (true)
        {
            yield return new WaitForSeconds(5f); // Her 5 saniyede bir eksik var mı kontrol et
            activeShips.RemoveAll(ship => ship == null);

            if (activeShips.Count < maxShipsOnMap)
            {
                // Eksik gemi kadar respawn coroutine başlat
                int missingCount = maxShipsOnMap - activeShips.Count;
                for (int i = 0; i < missingCount; i++)
                {
                    StartCoroutine(RespawnShipWithDelay());
                }
            }
        }
    }

    private IEnumerator RespawnShipWithDelay()
    {
        // Bir slotu rezerve et ki Update döngüsü eksik sanıp tekrar doğurmasın
        GameObject placeholder = new GameObject("ShipPlaceholder");
        activeShips.Add(placeholder); 

        yield return new WaitForSeconds(respawnDelay);
        
        activeShips.Remove(placeholder);
        Destroy(placeholder);
        
        SpawnShip();
    }

    private void SpawnShip()
    {
        Vector3 spawnPos = SpawnPoint.GetAvailableShipPos();
        
        if (spawnPos != Vector3.zero)
        {
            GameObject ship = Instantiate(shipPrefab, spawnPos, Quaternion.identity);
            ship.GetComponent<NetworkObject>().Spawn();
            activeShips.Add(ship);
        }
    }
}