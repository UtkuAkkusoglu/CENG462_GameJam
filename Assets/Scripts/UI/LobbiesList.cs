using System.Collections.Generic;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbiesList : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LobbyItem lobbyItemPrefab; 
    [SerializeField] private Transform container; 

    private bool _isRefreshing;

    public async void RefreshList()
    {
        if (_isRefreshing) return;
        _isRefreshing = true;

        try
        {
            // Eski prefabları temizle
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
            
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);

            // Listeyi oluştur
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
            // 429 hatasını önlemek için yenileme bittikten sonra kısa bir süre bekle
            Invoke(nameof(ResetRefresh), 1.5f); 
        }
    }

    private void ResetRefresh()
    {
        _isRefreshing = false;
    }

    // Join Flow - Güncellendi
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

                // --- KRİTİK NOKTA ---
                // Bu sayede çıkarken hangi lobiden ayrılacağımızı bileceğiz.
                await ClientSingleton.Instance.GameManager.StartClientAsync(joinCode, joinedLobby.Id);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Lobiye girilemedi: {e}");
        }
    }
}