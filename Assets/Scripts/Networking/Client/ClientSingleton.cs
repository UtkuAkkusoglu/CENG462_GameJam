using System.Threading.Tasks;
using UnityEngine;

public class ClientSingleton : MonoBehaviour
{
    private static ClientSingleton instance;
    public static ClientSingleton Instance
    {
        get
        {
            if(instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<ClientSingleton>();

            if(instance == null)
            {
                Debug.LogError("There is no ClientSingleton in the scene!");
                return null;
            }

            return instance;
        }
    }
    public ClientGameManager GameManager { get; private set; }

    private ClientGameManager gameManager;

    public async Task<bool> CreateClient()
    {
        GameManager = new ClientGameManager();
        bool success = await GameManager.InitAsync();
        return success;
    }

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
 
}
