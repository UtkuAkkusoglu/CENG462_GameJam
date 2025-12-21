using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine.SceneManagement;
using System; // Exception için
using Unity.Services.Relay; // RelayService ve Allocation için
using Unity.Services.Relay.Models; // Allocation modeli için
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP; // UnityTransport için
// using Unity.Networking.Transport.Relay; // RelayServerData için

public class ClientGameManager
{
    private const string MenuSceneName = "MainMenu";

    private JoinAllocation _allocation;


    public async Task<bool> InitAsync()
    {
        // 1. Initialise Unity Services
        await UnityServices.InitializeAsync();

        // 2. Try to authenticate the player
        var authState = await AuthenticationWrapper.DoAuth();

        if(authState == AuthenticationWrapper.AuthState.Authenticated)
        {
            return true;  // Successfully authenticated
        }
        else
        {
            return false;  // Authentication failed
        }
    }

    public async Task StartClientAsync(string joinCode)
    {
        try
        {
            _allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);  // Relay’e katıl
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return;
        }
        
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>(); // UnityTransport’u RelayServerData ile ayarla      
        transport.SetClientRelayData(
            _allocation.RelayServer.IpV4,
            (ushort)_allocation.RelayServer.Port,
            _allocation.AllocationIdBytes,
            _allocation.Key,
            _allocation.ConnectionData,
            _allocation.HostConnectionData,
            isSecure: false  // dtls'i kapatıp udp'ye geçtim çünkü clientlar bağlanamıyordu
        );

        // Client’ı başlat
        NetworkManager.Singleton.StartClient();
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene(MenuSceneName);
    }
}