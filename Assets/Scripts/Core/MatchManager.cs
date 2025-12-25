using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance;

    [Header("Match Settings")]
    [SerializeField] private int scoreToWin = 1000;
    
    [Header("UI References")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TMP_Text winnerNameText;

    public static bool IsMatchOver { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        IsMatchOver = false;
    }

    public override void OnNetworkSpawn()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    public void CheckWinCondition(ulong clientId, int currentScore)
    {
        if (!IsServer || IsMatchOver) return;

        if (currentScore >= scoreToWin)
        {
            string winnerName = GetPlayerName(clientId);
            AnnounceWinnerClientRpc(winnerName);
        }
    }

    private string GetPlayerName(ulong clientId)
    {
        foreach (var player in FindObjectsByType<PlayerStats>(FindObjectsSortMode.None))
        {
            if (player.OwnerClientId == clientId)
            {
                var nameDisplay = player.GetComponent<PlayerNameDisplay>();
                return nameDisplay != null ? nameDisplay.playerName.Value.ToString() : "Player " + clientId;
            }
        }
        return "Unknown Player";
    }

    [ClientRpc]
    private void AnnounceWinnerClientRpc(string winnerName)
    {
        IsMatchOver = true;

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            if (winnerNameText != null) winnerNameText.text = winnerName + " KAZANDI!";
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(WaitAndReturnToMenu(2.0f));
    }

    private IEnumerator WaitAndReturnToMenu(float delay)
    {
        yield return new WaitForSeconds(delay);
        BackToMenu(); 
    }

    public async void BackToMenu()
    {
        if (IsServer) 
        {
            // Host isen Relay ve Lobby oturumlarını güvenli kapat
            if (HostSingleton.Instance?.GameManager != null)
                await HostSingleton.Instance.GameManager.ShutdownAsync();
        }
        else 
        {
            // Client isen sadece ağdan kop
            NetworkManager.Singleton?.Shutdown();
        }
        
        IsMatchOver = false;
        SceneManager.LoadScene("MainMenu"); 
    }
}