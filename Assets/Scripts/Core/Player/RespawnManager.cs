using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class RespawnManager : NetworkBehaviour
{
    public static RespawnManager Instance;
    
    [Header("Respawn Settings")]
    [SerializeField] private GameObject playerPrefab; // Tank Prefab'ını buraya sürükle
    [SerializeField] private float respawnDelay = 2.0f; // 2 saniye beklemek iyidir

    public override void OnNetworkSpawn()
    {
        // Sunucu başladığında bu manager kendini tanıtsın
        if (IsServer)
        {
            Instance = this;
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void RespawnPlayer(ulong clientId)
    {
        if (!IsServer) return;
        StartCoroutine(RespawnCoroutine(clientId));
    }

    private IEnumerator RespawnCoroutine(ulong clientId)
    {
        // Ölüm mesajının ağda yayılması için bekle
        yield return new WaitForSeconds(respawnDelay);

        // Görev 11'deki güvenli spawn noktasını al
        Vector3 spawnPos = SpawnPoint.GetRandomPlayerPos();

        // KABUL KRİTERİ: Prefab'ı Instantiate et
        GameObject newPlayerTank = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

        // KABUL KRİTERİ: SpawnAsPlayerObject metodunu kullan
        // Bu metot tankı o clientId'ye tekrar teslim eder
        newPlayerTank.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        
        Debug.Log($"[Second Chance] Client {clientId} başarıyla yeniden doğduruldu!");
    }
}