using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private TMP_Text playerCountText;

    private Lobby _lobby;
    private LobbiesList _mainList;

    // Prefab oluştuğunda bu metod çağrılacak
    public void Initialize(LobbiesList mainList, Lobby lobby)
    {
        _mainList = mainList;
        _lobby = lobby;

        // Görsel verileri doldur
        lobbyNameText.text = lobby.Name;
        playerCountText.text = $"Players Count: {lobby.Players.Count}/{lobby.MaxPlayers}";
    }

    // Butonun OnClick() olayına Inspector'dan bu metodu bağla
    public void OnJoinButtonClicked()
    {
        if (_mainList != null && _lobby != null)
        {
            _mainList.JoinAsync(_lobby);
        }
    }
}