using Unity.Netcode;
using UnityEngine;

public class PlayerServerManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerNameDisplay nameDisplay;

    public override void OnNetworkSpawn()
    {
        // Bu kodun sadece sunucu tarafında çalışmasını garanti ediyoruz
        if (!IsServer) return;

        // 1. NetworkServer üzerinden bu oyuncunun (OwnerClientId) verilerini çekelim
        // Not: NetworkServer referansına senin proje yapına göre (HostSingleton üzerinden) erişiyoruz
        var networkServer = HostSingleton.Instance.GameManager.NetworkServer;
        var userData = networkServer.GetUserDataByClientId(OwnerClientId);

        if (userData != null)
        {
            // 2. İsmi NetworkVariable'a sahip olan script'e gönderelim
            if (nameDisplay != null)
            {
                nameDisplay.SetPlayerName(userData.username);
                Debug.Log($"[Server] {userData.username} ismi başarıyla atandı (ID: {OwnerClientId})");
            }
        }
        else
        {
            Debug.LogWarning($"[Server] ClientId {OwnerClientId} için UserData bulunamadı!");
        }
    }
}