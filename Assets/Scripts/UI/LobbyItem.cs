using TMPro;
using UnityEngine;
using Unity.Services.Lobbies.Models;

public class LobbyItem : MonoBehaviour 

{
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private TMP_Text playerCountText;

    private Lobby _lobby;

    public void Initialize(Lobby lobby) 

    {
        _lobby = lobby;

        // Lobi bilgilerini UI elemanlarına ata
        lobbyNameText.text = lobby.Name;
        playerCountText.text = $"Players Count: {lobby.Players.Count}/{lobby.MaxPlayers}";
    }

    public void Join() 
    {   
        /* 
        TODO: Lobiye katılma akışını buraya yazacağız.
        TODO: 1. JoinLobbyByIdAsync(lobby.Id) ile lobiye katıl.
        TODO: 2. Lobi verisinden "join code" bilgisini oku.
        TODO: 3. StartClientAsync(joinCode) metodunu çağırarak oyuna bağlan.
        */
    }
}