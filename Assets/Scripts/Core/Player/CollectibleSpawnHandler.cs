using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CollectibleSpawnHandler : NetworkBehaviour
{
    [Header("Doğma Ayarları")]
    [SerializeField] private GameObject[] collectiblePrefabs;
    [SerializeField] private float spawnInterval = 10f; 
    [SerializeField] private int maxItemsOnMap = 15;

    [Header("Yaşam Süresi Ayarları")]
    [SerializeField] private bool useLifeTime = true; // Bu özelliği açıp kapatabilmen için
    [SerializeField] private float itemLifeTime = 15f; // Senin istediğin 15 saniye

    private List<GameObject> activeItems = new List<GameObject>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(SpawnRoutine());
        }
    }

    private IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(2f);

        while (true)
        {
            activeItems.RemoveAll(item => item == null);

            if (activeItems.Count < maxItemsOnMap)
            {
                SpawnEachType();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnEachType()
    {
        foreach (var prefab in collectiblePrefabs)
        {
            Vector3 spawnPos = SpawnPoint.GetAvailableItemPos();

            if (spawnPos != Vector3.zero)
            {
                GameObject item = Instantiate(prefab, spawnPos, Quaternion.identity);
                item.GetComponent<NetworkObject>().Spawn();
                activeItems.Add(item);

                // EĞER ÖZELLİK AÇIKSA: Belirlenen süre sonra silinmesi için Coroutine başlat
                if (useLifeTime)
                {
                    StartCoroutine(DestroyAfterTime(item, itemLifeTime));
                }
            }
        }
    }

    private IEnumerator DestroyAfterTime(GameObject item, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Obje hala sahnede mi (toplanmadı mı?) ve biz Server mıyız kontrol et
        if (item != null && item.GetComponent<NetworkObject>().IsSpawned)
        {
            Debug.Log($"[Spawner] {item.name} süresi dolduğu için imha edildi.");
            item.GetComponent<NetworkObject>().Despawn(true); // Ağdan ve sahneden sil
        }
    }
}