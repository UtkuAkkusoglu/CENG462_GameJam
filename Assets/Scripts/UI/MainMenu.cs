using UnityEngine;
using TMPro;
using System.Threading.Tasks;

public class MainMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_InputField joinByCodeField;
    [SerializeField] private GameObject mainButtonsLayout; // Ana Menü İlk Sekmesi
    [SerializeField] private GameObject lobbiesBackground; // Lobbies Background Paneli 
    [SerializeField] private LobbiesList lobbiesListManager;

    // Host başlatma metodu
    public async void StartHost()
    {
        // TODO: StartHostAsync içerisinde artık Lobby de oluşturulacak
        await HostSingleton.Instance.GameManager.StartHostAsync();
    }

    // Manuel Join Code ile Client bağlanma metodu
    public async void StartClient()
    {
        await ClientSingleton.Instance.GameManager.StartClientAsync(joinByCodeField.text);
    }

    // Lobi listesi panelini açar 
    public void OpenLobbiesLayout()
    {
        lobbiesBackground.SetActive(true);
        mainButtonsLayout.SetActive(false);
    }

    // Lobi listesi panelini açar
    public void CloseLobbiesLayout()
    {
        lobbiesBackground.SetActive(false);
        mainButtonsLayout.SetActive(true);
    }

    public void RefreshLobbiesList()
    {
        if (lobbiesListManager != null)
        {
            lobbiesListManager.RefreshList();
        }
        else
        {
            Debug.LogWarning("LobbiesList referansı MainMenu'ye atanmamış!");
        }
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
        
        Debug.Log("Oyundan çıkış yapıldı.");
    }
}