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
    [SerializeField] private int scoreToWin = 500;
    
    [Header("UI References")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TMP_Text winnerNameText;

    [Header("Relay Info")]
    [SerializeField] private TMP_Text joinCodeText;

    public static bool IsMatchOver { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        IsMatchOver = false;
    }

    public override void OnNetworkSpawn()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);

        // SADECE HOST (Server) İÇİN ÇALIŞIR
        if (IsServer)
        {
            // Yazıyı aktif et (Eğer kapalıysa)
            if (joinCodeText != null) joinCodeText.gameObject.SetActive(true);
            
            string savedCode = PlayerPrefs.GetString("LastJoinCode", "------");
            SetJoinCode(savedCode);
        }
        else
        {
            // Client isen yazıyı komple gizle
            if (joinCodeText != null) joinCodeText.gameObject.SetActive(false);
        }
    }

    public void SetJoinCode(string code)
    {
        if (joinCodeText != null)
        {
            joinCodeText.text = "Join Code: " + code;
        }
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