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

        _networkManager.ConnectionApprovalCallback -= ApprovalCheck; 
        _networkManager.ConnectionApprovalCallback += ApprovalCheck;
        
        _networkManager.OnServerStarted -= OnServerStarted;
        _networkManager.OnServerStarted += OnServerStarted;
        
        _networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        _networkManager.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // 1. Gelen veriyi çöz
        string payloadJson = Encoding.UTF8.GetString(request.Payload);
        UserData userData = JsonUtility.FromJson<UserData>(payloadJson);

        // 2. Sunucu Logları 
        Debug.Log($"[NetworkServer] Approved: {userData.username} (AuthID: {userData.userAuthId})");

         // 3. Veriyi sakla
        _clientIdToAuthId[request.ClientNetworkId] = userData.userAuthId;
        _authIdToUserData[userData.userAuthId] = userData;

        // Quest 11.2 & 11.3: Statik metot üzerinden rastgele konum alıyoruz
        // Bu sayede oyuncular (0,0,0) noktasında doğmaz.
        Vector3 spawnPos = SpawnPoint.GetRandomPlayerPos();
        Debug.Log($"[Spawn Test] {userData.username} için seçilen konum: {spawnPos}");

        // 4. Onay ve Spawn
        response.Approved = true;
        response.CreatePlayerObject = true; 
        response.Position = spawnPos;
        response.Rotation = Quaternion.identity;
        // --------------------------------------------------

        response.Pending = false;
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
            Debug.Log($"[NetworkServer] Client {clientId} disconnected.");

            // TODO: Liderlik tablosunu burada güncelle
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