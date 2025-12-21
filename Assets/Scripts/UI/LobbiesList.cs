using System.Collections.Generic;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbiesList : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LobbyItem lobbyItemPrefab; // Hazırladığın prefab
    [SerializeField] private Transform container; // Scroll View -> Content

    private bool _isRefreshing;

    // Panel açıldığında veya Refresh butonuna basıldığında çağrılır 
    public async void RefreshList()
    {
        if (_isRefreshing) return;
        _isRefreshing = true;

        try
        {
            // --- QUEST 6.4.2.0: CLEANUP ---
            // Yeni lobileri eklemeden önce içerideki eski prefabları yok et
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
            // ------------------------------
            
            // Quest 4.2: Sadece boş yer olan lobileri filtrele
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);

            // Önceki listeyi temizle
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }

            // Yeni listeyi oluştur
            foreach (Lobby lobby in response.Results)
            {
                LobbyItem item = Instantiate(lobbyItemPrefab, container); 
                item.Initialize(this, lobby);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Lobi listesi çekilemedi: {e}");
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    //  Join Flow
    public async void JoinAsync(Lobby lobby)
    {
        try
        {
            // 1. Servis üzerinden lobiye katıl
            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);

            // 2. Relay kodunu oku 
            if (joinedLobby.Data.TryGetValue("join code", out var joinCodeData))
            {
                string joinCode = joinCodeData.Value;
                Debug.Log($"Lobiye girildi! Join Code: {joinCode}");

                // 3. ClientGameManager üzerinden bağlantıyı başlat
                await ClientSingleton.Instance.GameManager.StartClientAsync(joinCode);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Lobiye girilemedi: {e}");
        }
    }
}