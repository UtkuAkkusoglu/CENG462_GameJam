using System;
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

        // 5️⃣ Host başlat + sahne yükle
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
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