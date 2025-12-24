using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private GameObject pauseCanvas; 
    [SerializeField] private Button backToMenuButton;
    
    // Sadece UI'ın açık olup olmadığını diğerleri bilsin diye bırakıyoruz
    public static bool IsMenuOpen { get; private set; }

    private void Awake()
    {
        IsMenuOpen = false;
        if (pauseCanvas != null) pauseCanvas.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        inputReader.PauseEvent += HandlePause;
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(BackToMenu);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        inputReader.PauseEvent -= HandlePause;
        IsMenuOpen = false;
    }

    private void HandlePause()
    {
        IsMenuOpen = !IsMenuOpen;
        pauseCanvas.SetActive(IsMenuOpen);

        // Fareyi menü için serbest bırak, oyun için kilitle
        Cursor.lockState = IsMenuOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = IsMenuOpen;
    }

    public async void BackToMenu()
    {
        if (IsServer) 
        {
            if (HostSingleton.Instance?.GameManager != null)
                await HostSingleton.Instance.GameManager.ShutdownAsync();
        }
        else 
        {
            NetworkManager.Singleton?.Shutdown();
        }
        
        IsMenuOpen = false;
        SceneManager.LoadScene("MainMenu"); 
    }
}