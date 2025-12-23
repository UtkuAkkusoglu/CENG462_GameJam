using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine.SceneManagement;
using System; // Exception için
using System.Text; // Encoding için
using Unity.Services.Relay; // RelayService ve Allocation için
using Unity.Services.Relay.Models; // Allocation modeli için
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP; // UnityTransport için
using Unity.Services.Authentication; // AuthId için
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
        // Eğer kullanıcı kodu girmemişse veya varsayılan yazı duruyorsa işlemi durdur
        if (string.IsNullOrEmpty(joinCode) || joinCode == "Enter Join Code")
        {
            Debug.LogWarning("Lütfen geçerli bir Join Code girin!");
            return;
        }
        
        try
        {
            _allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);  // Relay’e katıl
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return;
        }

        // --- QUEST 6: PAYLOAD PREPARATION (Hocanın İstediği) ---
        UserData userData = new UserData
        {
            username = PlayerPrefs.GetString("player name", "missing name"),
            userAuthId = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId
        };

        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(userData));
        // ------------------------------------
        
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

        // QUEST 6.7: İstemci tarafında kopma olayını dinle
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

        // Client’ı başlat
        NetworkManager.Singleton.StartClient();
    }

    private void OnClientDisconnect(ulong clientId)
    {
        // Eğer kopan kişi bizsek (clientId 0 veya LocalClientId)
        if (clientId == NetworkManager.Singleton.LocalClientId || clientId == 0)
        {
            Disconnect();
        }
    }

    public void Disconnect()
    {
        // QUEST 6.7: Ağ iletişimini temiz kapat ve menüye dön
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            NetworkManager.Singleton.Shutdown();
        }
        
        GoToMenu(); // MainMenu sahnesine uçur
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene(MenuSceneName);
    }
}