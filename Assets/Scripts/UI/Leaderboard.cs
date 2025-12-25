using Unity.Netcode;
using UnityEngine;
using System.Linq;
using Unity.Collections;
using System;
using System.Collections.Generic;

// IEquatable ekle
public struct LeaderboardEntityState : INetworkSerializable, IEquatable<LeaderboardEntityState>
{
    public ulong ClientId;
    public FixedString64Bytes PlayerName;
    public float Score;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref Score);
    }

    // IEquatable implementasyonu
    public bool Equals(LeaderboardEntityState other)
    {
        return ClientId == other.ClientId && 
               PlayerName.Equals(other.PlayerName) && 
               Score == other.Score;
    }

    public override bool Equals(object obj)
    {
        return obj is LeaderboardEntityState other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ClientId, PlayerName, Score);
    }
}

public class Leaderboard : NetworkBehaviour
{
    [SerializeField] private Transform container;
    [SerializeField] private LeaderboardEntry entryPrefab;

    private NetworkList<LeaderboardEntityState> leaderboardEntities;

    private void Awake()
    {
        leaderboardEntities = new NetworkList<LeaderboardEntityState>();
    }

    public override void OnNetworkSpawn()
    {
        leaderboardEntities.OnListChanged += (changeEvent) => RefreshUI();
        RefreshUI();

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += (id) => ScanForPlayers();
            ScanForPlayers();
        }
    }

    private void ScanForPlayers()
    {
        if (!IsServer) return;
        leaderboardEntities.Clear();
        
        foreach (var player in FindObjectsByType<PlayerStats>(FindObjectsSortMode.None))
        {
            AddPlayerToLeaderboard(player);
        }
    }

    public void AddPlayerToLeaderboard(PlayerStats player)
    {
        if (!IsServer) return;

        // --- KRİTİK KONTROL: Oyuncu listede zaten var mı? ---
        foreach (var entity in leaderboardEntities)
        {
            if (entity.ClientId == player.OwnerClientId)
            {
                // Eğer oyuncu zaten varsa tekrar ekleme, metoddan çık.
                return; 
            }
        }

        var nameDisplay = player.GetComponent<PlayerNameDisplay>();
        string pName = nameDisplay != null ? nameDisplay.playerName.Value.ToString() : "Player " + player.OwnerClientId;

        leaderboardEntities.Add(new LeaderboardEntityState {
            ClientId = player.OwnerClientId,
            PlayerName = pName,
            Score = player.Score.Value
        });

        // Event'i bağla (Burada += kullanırken dikkat, her spawn'da tekrar bağlanmasın diye 
        // PlayerStats tarafında OnDestroy'da -= yapmak en sağlıklısıdır)
        player.Score.OnValueChanged += (oldVal, newVal) => UpdateScore(player.OwnerClientId, newVal);
    }

    private void UpdateScore(ulong clientId, int newScore)
    {
        for (int i = 0; i < leaderboardEntities.Count; i++)
        {
            if (leaderboardEntities[i].ClientId == clientId)
            {
                var state = leaderboardEntities[i];
                state.Score = newScore;
                leaderboardEntities[i] = state;
                break;
            }
        }
    }

    private void RefreshUI()
    {
        if (container == null) return;

        // 1. Mevcut UI elemanlarını temizle
        foreach (Transform child in container) 
        {
            Destroy(child.gameObject);
        }

        // 2. NetworkList'teki verileri standart bir listeye kopyala
        // Bu yöntem 'Cast' veya 'ToList' hatalarını tamamen ortadan kaldırır.
        List<LeaderboardEntityState> tempList = new List<LeaderboardEntityState>();
        foreach (var entity in leaderboardEntities)
        {
            tempList.Add(entity);
        }

        // 3. Standart liste üzerinden sıralama yap
        var sorted = tempList
            .OrderByDescending(x => x.Score)
            .ToList();

        // 4. UI'da göster
        foreach (var entity in sorted)
        {
            LeaderboardEntry entry = Instantiate(entryPrefab, container);
            // Entity içindeki verileri gönderiyoruz. 
            // localClientId ile karşılaştırarak 'isMe' durumunu belirliyoruz.
            entry.Display(
                entity.PlayerName.ToString(), 
                (int)entity.Score, 
                entity.ClientId == NetworkManager.Singleton.LocalClientId
            );
        }
    }
}