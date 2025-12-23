using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine.SceneManagement;
using System;
using System.Text;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Lobbies; // Lobi servisi için ekledik

public class ClientGameManager
{
    private const string MenuSceneName = "MainMenu";
    private JoinAllocation _allocation;
    private string _joinedLobbyId; // Katıldığın lobinin ID'si burada saklanacak

    public async Task<bool> InitAsync()
    {
        await UnityServices.InitializeAsync();
        var authState = await AuthenticationWrapper.DoAuth();
        return authState == AuthenticationWrapper.AuthState.Authenticated;
    }

    // Parametre olarak lobbyId ekledik ki ayrılırken kullanabilelim
    public async Task StartClientAsync(string joinCode, string lobbyId = null)
    {
        if (string.IsNullOrEmpty(joinCode) || joinCode == "Enter Join Code")
        {
            Debug.LogWarning("Lütfen geçerli bir Join Code girin!");
            return;
        }

        _joinedLobbyId = lobbyId; // Lobby ID'yi kaydet

        try
        {
            _allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return;
        }

        UserData userData = new UserData
        {
            username = PlayerPrefs.GetString("player name", "missing name"),
            userAuthId = AuthenticationService.Instance.PlayerId
        };

        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(userData));
        
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();     
        transport.SetClientRelayData(
            _allocation.RelayServer.IpV4,
            (ushort)_allocation.RelayServer.Port,
            _allocation.AllocationIdBytes,
            _allocation.Key,
            _allocation.ConnectionData,
            _allocation.HostConnectionData,
            isSecure: false 
        );

        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        NetworkManager.Singleton.StartClient();
    }

    // QUEST 6.8: Lobiden ayrılma fonksiyonu
    public async Task LeaveLobbyAsync()
    {
        if (string.IsNullOrEmpty(_joinedLobbyId)) return;

        try
        {
            // Servise "ben bu lobiden çıkıyorum" diyoruz
            await LobbyService.Instance.RemovePlayerAsync(_joinedLobbyId, AuthenticationService.Instance.PlayerId);
            _joinedLobbyId = null;
            Debug.Log("Lobiden başarıyla ayrıldık.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Lobiden ayrılırken hata: {e.Message}");
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId || clientId == 0)
        {
            Disconnect();
        }
    }

    public async void Disconnect()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            NetworkManager.Singleton.Shutdown();
        }
        
        // Önce lobiden ayrılmayı bekle, sonra menüye dön
        await LeaveLobbyAsync(); 
        GoToMenu();
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene(MenuSceneName);
    }
}