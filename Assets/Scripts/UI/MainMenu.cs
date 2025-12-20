using UnityEngine;
using TMPro; // TMP_InputField için

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeField;

    // Host başlatma metodu
    public async void StartHost()
    {
        await HostSingleton.Instance.GameManager.StartHostAsync();
    }

    // Client bağlanma metodu
    public async void StartClient()
    {
        await ClientSingleton.Instance.GameManager.StartClientAsync(joinCodeField.text);
    }

    public void QuitGame()
    {
        // Eğer Unity Editor içindeysek Play modunu durdur
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // Eğer Build alınmış gerçek uygulamadaysak oyunu kapat
            Application.Quit();
        #endif
        
        Debug.Log("Oyundan çıkış yapıldı.");
    }
}