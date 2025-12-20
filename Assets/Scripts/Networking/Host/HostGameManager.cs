using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public class HostGameManager
{
    private const int MaxConnections = 20;
    private const string GameSceneName = "Game";

    private Allocation _allocation;
    private string _joinCode;

    public async Task StartHostAsync()
    {
        try
        {
            // 1️⃣ Allocation oluştur
            _allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections);

            // 2️⃣ Join Code al
            _joinCode = await RelayService.Instance.GetJoinCodeAsync(_allocation.AllocationId);

            Debug.Log($"Join Code: {_joinCode}");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return;
        }

        // 3️⃣ Transport setup
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetHostRelayData(
            _allocation.RelayServer.IpV4,
            (ushort)_allocation.RelayServer.Port,
            _allocation.AllocationIdBytes,
            _allocation.Key,
            _allocation.ConnectionData,
            isSecure: false  // dtls'i kapatıp udp'ye geçtim çünkü clientlar bağlanamıyordu
        );

        // 4️⃣ Host başlat + sahne yükle
        NetworkManager.Singleton.StartHost();

        NetworkManager.Singleton.SceneManager.LoadScene(
            GameSceneName,
            LoadSceneMode.Single
        );
    }
}