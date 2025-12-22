using System;
using System.Text; // Encoding için
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HostGameManager
{
    private const int MaxConnections = 20;
    private const string GameSceneName = "Game";
    private Allocation _allocation;
    private string _joinCode;
    private Lobby _hostLobby; // Kurulan lobiyi tutmak için

    public NetworkServer NetworkServer { get; private set; }

    public async Task StartHostAsync()
    {
        try
        {
            // 1️⃣ Relay Allocation oluştur
            _allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections);

            // 2️⃣ Join Code al
            _joinCode = await RelayService.Instance.GetJoinCodeAsync(_allocation.AllocationId);

            // 3️⃣ Lobi Oluştur ve Join Code'u içine göm
            await CreateLobby(_joinCode);

            Debug.Log($"Join Code: {_joinCode}");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return;
        }

        // --- QUEST 6: HOST PAYLOAD PREPARATION ---
        // Host da bir "oyuncu" olduğu için kendi kimlik kartını hazırlamalı
        UserData userData = new UserData
        {
            username = PlayerPrefs.GetString("player name", "missing name"),
            userAuthId = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId
        };
        byte[] payloadBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(userData));
        
        // Host'un kendi verisini de NetworkConfig'e koyuyoruz
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;
        // ------------------------------------------

        // 4️⃣ Transport setup (Kendi ayarların)
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetHostRelayData(
            _allocation.RelayServer.IpV4,
            (ushort)_allocation.RelayServer.Port,
            _allocation.AllocationIdBytes,
            _allocation.Key,
            _allocation.ConnectionData,
            isSecure: false // şu anda udp'de, sonra dtls'e al
        );

        // --- NETWORK SERVER BAŞLATMA ---
        NetworkServer = new NetworkServer(NetworkManager.Singleton);

        // Host'u başlat
        bool success = NetworkManager.Singleton.StartHost();
        
        // Eğer host başarıyla başladıysa sahneyi yükle
        if (success)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
        }
    }

    // QUEST 6.7 & 6.8: Temiz Shutdown
    public async void Shutdown()
    {
        // 1. Lobi silme (Quest 6.8.3)
        if (_hostLobby != null)
        {
            try {
                await LobbyService.Instance.DeleteLobbyAsync(_hostLobby.Id);
                _hostLobby = null;
            } catch (Exception e) { Debug.Log(e); }
        }

        // 2. NetworkServer temizliği
        NetworkServer?.Dispose();

        // 3. Netcode kapatma
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }

    private async Task CreateLobby(string relayJoinCode)
    {
        try
        {
            // Lobi seçeneklerini ayarla
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    {
                        "join code", new DataObject(
                            visibility: DataObject.VisibilityOptions.Member, // Sadece lobinin içine girmiş olanlar bu veriyi görebilir
                            value: relayJoinCode)
                    }
                }
            };

            // Bootstrap'te PlayerPrefs'e kaydettiğimiz ismi al
            string playerName = PlayerPrefs.GetString("PlayerName", "Unknown Host");
            string lobbyName = $"{playerName}'s Lobby";

            // Lobiyi oluştur
            _hostLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MaxConnections, options);

            // Heartbeat (Kalp Atışı) Coroutine'ini başlat
            HostSingleton.Instance.StartCoroutine(HeartbeatLobby(15f));
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Lobi hatası: {e}");
        }
    }

    private IEnumerator HeartbeatLobby(float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);
        while (_hostLobby != null)
        {
            // Servise "Hala buradayım" sinyali gönder
            LobbyService.Instance.SendHeartbeatPingAsync(_hostLobby.Id);
            yield return delay;
        }
    }
}