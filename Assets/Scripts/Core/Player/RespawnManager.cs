using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class RespawnManager : NetworkBehaviour
{
    public static RespawnManager Instance;
    
    [Header("Respawn Settings")]
    [SerializeField] private GameObject playerPrefab; 
    [SerializeField] private float respawnDelay = 2.0f; 
    [SerializeField] private int deathPenaltyAmount = 100; 

    public override void OnNetworkSpawn()
    {
        if (IsServer) Instance = this;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void RespawnPlayer(ulong clientId)
    {
        if (!IsServer) return;
        
        // 1. Önce cezayı kes (Oyuncu henüz silinmeden puanı düşürür)
        ApplyPenalty(clientId);
        
        // 2. Sonra doğurma sürecini başlat
        StartCoroutine(RespawnCoroutine(clientId));
    }

    private void ApplyPenalty(ulong clientId)
    {
        foreach (var player in FindObjectsByType<PlayerStats>(FindObjectsSortMode.None))
        {
            if (player.OwnerClientId == clientId)
            {
                player.DeductScoreOnDeath(deathPenaltyAmount);
                break;
            }
        }
    }

    private IEnumerator RespawnCoroutine(ulong clientId)
    {
        // 1. Önce mevcut puanı yedekle
        int savedScore = 0;
        foreach (var stats in FindObjectsByType<PlayerStats>(FindObjectsSortMode.None))
        {
            if (stats.OwnerClientId == clientId)
            {
                savedScore = stats.Score.Value; // Ceza düşülmüş halini alıyoruz
                break;
            }
        }

        yield return new WaitForSeconds(respawnDelay);

        Vector3 spawnPos = SpawnPoint.GetRandomPlayerPos();
        GameObject newPlayerTank = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

        // 2. Tankı spawn et
        var netObj = newPlayerTank.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId, true);

        // 3. KRİTİK ADIM: Eski puanı yeni tanka yükle
        if (newPlayerTank.TryGetComponent<PlayerStats>(out var newStats))
        {
            newStats.Score.Value = savedScore; // Puanı geri yükledik!
            Debug.Log($"[Migration] Client {clientId} puanı geri yüklendi: {savedScore}");
        }
        
        Debug.Log($"[Second Chance] Client {clientId} başarıyla yeniden doğduruldu!");
    }
}