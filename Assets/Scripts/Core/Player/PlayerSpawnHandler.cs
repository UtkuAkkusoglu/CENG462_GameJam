using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawnHandler : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        StartCoroutine(SpawnWhenReady());
    }

    private IEnumerator SpawnWhenReady()
    {
        // Liste dolana kadar bekle (Maksimum 5 saniye boyunca dene)
        float timer = 0;
        while (SpawnPoint.GetRandomSpawnPos() == Vector3.zero && timer < 5f)
        {
            timer += Time.deltaTime;
            yield return null; // Bir sonraki frame'e kadar bekle
        }

        Vector3 finalPos = SpawnPoint.GetRandomSpawnPos();

        if (finalPos != Vector3.zero)
        {
            transform.position = finalPos;
            Debug.Log($"[Spawn Success] {timer} saniye sonra gerçek noktaya gidildi: {finalPos}");
            
            // Eğer NetworkTransform varsa zorla ışınla
            if(TryGetComponent(out Unity.Netcode.Components.NetworkTransform netTransform))
            {
                netTransform.Teleport(finalPos, transform.rotation, transform.localScale);
            }
        }
        else
        {
            Debug.LogError("5 saniye geçti ama hala SpawnPoint bulunamadı! Sahneyi kontrol et.");
        }
    }
}