using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    // Hocanın istediği statik liste kaydı
    private static List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

    // Awake, OnNetworkSpawn'dan daha önce çalıştığı için kaydı buraya aldık
    private void Awake()
    {
        if (!spawnPoints.Contains(this))
        {
            spawnPoints.Add(this);
        }
    }

    private void OnDestroy()
    {
        spawnPoints.Remove(this);
    }

    public static Vector3 GetRandomSpawnPos()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("Hiç SpawnPoint bulunamadı! Liste boş.");
            return Vector3.zero; 
        }
        
        int randomIndex = UnityEngine.Random.Range(0, spawnPoints.Count);
        return spawnPoints[randomIndex].transform.position;
    }

    // KABUL KRİTERİ: Editor'de mavi küreler görünmeli
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, 1.5f); // Daha belirgin olması için büyüttük
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + transform.up * 2f);
    }
}