using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class JeepSpawner : NetworkBehaviour
{
    [Header("Jeep Ayarları")]
    [SerializeField] private GameObject jeepPrefab;
    [SerializeField] private int maxJeepsOnMap = 4; // Belirttiğin gibi 4 tane
    [SerializeField] private float respawnDelay = 20f;

    private List<GameObject> activeJeeps = new List<GameObject>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        StartCoroutine(InitialSpawnRoutine());
    }

    private IEnumerator InitialSpawnRoutine()
    {
        yield return new WaitForSeconds(1f); // Sahne tam yüklensin
        
        for (int i = 0; i < maxJeepsOnMap; i++)
        {
            SpawnJeep();
        }

        while (true)
        {
            yield return new WaitForSeconds(5f);
            activeJeeps.RemoveAll(jeep => jeep == null);

            if (activeJeeps.Count < maxJeepsOnMap)
            {
                int missingCount = maxJeepsOnMap - activeJeeps.Count;
                for (int i = 0; i < missingCount; i++)
                {
                    StartCoroutine(RespawnJeepWithDelay());
                }
            }
        }
    }

    private IEnumerator RespawnJeepWithDelay()
    {
        // Placeholder mantığı mükemmel, tekrar doğuşu engeller
        GameObject placeholder = new GameObject("JeepPlaceholder");
        activeJeeps.Add(placeholder); 

        yield return new WaitForSeconds(respawnDelay);
        
        activeJeeps.Remove(placeholder); // Burada senin listeden siliyoruz
        Destroy(placeholder);
        
        SpawnJeep();
    }

    private void SpawnJeep()
    {
        Vector3 spawnPos = SpawnPoint.GetAvailableJeepPos();
        
        if (spawnPos != Vector3.zero)
        {
            GameObject jeep = Instantiate(jeepPrefab, spawnPos, Quaternion.identity);
            jeep.GetComponent<NetworkObject>().Spawn();
            activeJeeps.Add(jeep);
        }
    }
}