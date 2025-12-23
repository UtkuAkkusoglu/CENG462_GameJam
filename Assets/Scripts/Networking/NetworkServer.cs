using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

public class NetworkServer : IDisposable
{
    private NetworkManager _networkManager;
    private Dictionary<ulong, string> _clientIdToAuthId = new Dictionary<ulong, string>();
    private Dictionary<string, UserData> _authIdToUserData = new Dictionary<string, UserData>();

    public NetworkServer(NetworkManager networkManager)
    {
        _networkManager = networkManager;

        // ÖNCE TEMİZLE, SONRA ABONE OL (Double-check stratejisi)
        _networkManager.ConnectionApprovalCallback -= ApprovalCheck; 
        _networkManager.ConnectionApprovalCallback += ApprovalCheck;
        
        _networkManager.OnServerStarted -= OnServerStarted;
        _networkManager.OnServerStarted += OnServerStarted;
        
        _networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        _networkManager.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // 1. Gelen veriyi çöz (Decode -> JSON -> UserData)
        string payloadJson = Encoding.UTF8.GetString(request.Payload);
        UserData userData = JsonUtility.FromJson<UserData>(payloadJson);

        // 2. Sunucu Logları (Hocanın kriteri)
        Debug.Log($"[NetworkServer] Approved: {userData.username} (AuthID: {userData.userAuthId})");

        // 3. Veriyi sakla (Presence - Quest 7.1)
        _clientIdToAuthId[request.ClientNetworkId] = userData.userAuthId;
        _authIdToUserData[userData.userAuthId] = userData;

        // 4. Onay ve Spawn (Quest 6.5.2)
        response.Approved = true;
        response.CreatePlayerObject = true; // Oyuncu nesnesi oluşturulsun
        response.Pending = false;
        
        // (İsteğe bağlı) Başlangıç konumu ayarı burada yapılabilir
    }

    private void OnServerStarted()
    {
        Debug.Log("Network Server Started!");
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (_clientIdToAuthId.TryGetValue(clientId, out string authId))
        {
            _clientIdToAuthId.Remove(clientId);
            _authIdToUserData.Remove(authId);
            Debug.Log($"[NetworkServer] Client {clientId} (AuthID: {authId}) disconnected.");

            // TODO: Liderlik tablosunu burada güncelle (Quest 12)
        }
    }

    public void Dispose()
    {
        if (_networkManager == null) return;
        _networkManager.ConnectionApprovalCallback -= ApprovalCheck;
        _networkManager.OnServerStarted -= OnServerStarted;
        _networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    public UserData GetUserDataByClientId(ulong clientId)
    {
        if (_clientIdToAuthId.TryGetValue(clientId, out string authId))
        {
            if (_authIdToUserData.TryGetValue(authId, out UserData userData))
            {
                return userData;
            }
        }
        return null;
    }
}