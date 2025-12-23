using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameField;
    [SerializeField] private Button connectButton;
    [SerializeField] private int minNameLength = 1;
    [SerializeField] private int maxNameLength = 12;

    private const string NetBootstrapSceneName = "NetBootstrap";

    private void Start()
    {
        // Varsa eski ismi getir, bizde şu anda Utku - Burak. Sıfırlamak için: PlayerPrefs.DeleteAll();
        nameField.text = PlayerPrefs.GetString("player name", "");
        OnNameChanged(nameField.text); 
    }

    public void OnNameChanged(string newName)
    {
        // İsim uzunluğu geçerliyse butonu aç
        connectButton.interactable = newName.Length >= minNameLength && newName.Length <= maxNameLength;
    }

    public void Connect()
    {
        // İsmi kaydet
        PlayerPrefs.SetString("player name", nameField.text);
        
        // NetBootstrap sahnesine geç
        SceneManager.LoadScene(NetBootstrapSceneName);
    }
}