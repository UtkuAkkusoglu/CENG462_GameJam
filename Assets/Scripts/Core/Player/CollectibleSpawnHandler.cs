using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CollectibleSpawnHandler : NetworkBehaviour
{
    [Header("Ayarlar")]
    [SerializeField] private GameObject[] collectiblePrefabs; // Collectible item prefabları
    [SerializeField] private float spawnInterval = 10f; // 10 saniyede bir doğacaklar
    [SerializeField] private int maxItemsOnMap = 15; // Haritada aynı anda kaç item olabilir

    private List<GameObject> activeItems = new List<GameObject>();

    public override void OnNetworkSpawn()
    {
        // Sadece Sunucu (Server/Host) eşya doğurma yetkisine sahiptir
        if (IsServer)
        {
            StartCoroutine(SpawnRoutine());
        }
    }

    private IEnumerator SpawnRoutine()
    {
        // Oyuncuların yerleşmesi için ilk başta biraz bekle
        yield return new WaitForSeconds(2f);

        while (true)
        {
            // Önce listedeki silinmiş (toplanmış) objeleri temizle
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
        // Her türden (Coin, Speed, Shield) birer tane doğurur
        foreach (var prefab in collectiblePrefabs)
        {
            // SpawnPoint scriptindeki static metodu çağırıyoruz
            Vector3 spawnPos = SpawnPoint.GetRandomItemPos();

            if (spawnPos != Vector3.zero)
            {
                GameObject item = Instantiate(prefab, spawnPos, Quaternion.identity);
                item.GetComponent<NetworkObject>().Spawn(); // Ağda aktifleştir
                activeItems.Add(item);
                
                Debug.Log($"[Spawner] {prefab.name} doğuruldu: {spawnPos}");
            }
        }
    }
}