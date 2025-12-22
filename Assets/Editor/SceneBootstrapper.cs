#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class SceneBootstrapper
{
    private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity"; // Sahnenin gerçek yolunu buraya yaz

    static SceneBootstrapper()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Play'e basıldığı an mevcut sahneyi kaydet (Değişiklikler gitmesin)
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            // Eğer Play moduna girildiyse ve aktif sahne Bootstrap (index 0) değilse
            if (SceneManager.GetActiveScene().buildIndex != 0)
            {
                // Bootstrap sahnesini yükle
                // Not: Build Settings -> Scenes in Build listesinde Bootstrap'in en üstte (0. sırada) olduğundan emin ol!
                SceneManager.LoadScene(0);
            }
        }
    }
}
#endif