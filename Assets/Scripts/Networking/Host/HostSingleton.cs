using UnityEngine;

public class HostSingleton : MonoBehaviour
{
    private static HostSingleton instance;
    public static HostSingleton Instance
    {
        get
        {
            if(instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<HostSingleton>();

            if(instance == null)
            {
                Debug.LogError("There is no HostSingleton in the scene!");
                return null;
            }

            return instance;
        }
    }
    
    public HostGameManager GameManager { get; private set; }

    public void CreateHost()
    {
        GameManager = new HostGameManager();
    }

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    
}
