using System.Threading.Tasks;
using UnityEngine;

public class ApplicationController : MonoBehaviour
{
    [SerializeField] private ClientSingleton clientPrefab;
    [SerializeField] private HostSingleton hostPrefab;

    private async void Awake()
    {
        DontDestroyOnLoad(this.gameObject);

        bool isDedicatedServer = SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;  // to check if the game is running as a “headless” dedicated server (no graphics device). Grafik cihazı yoksa dedicated server olarak çalışıyor demektir.
        await LaunchInMode(isDedicatedServer);
    }

    private async Task LaunchInMode(bool isDedicatedServer)
    {
        if(isDedicatedServer)
        {
            // TODO: dedicated server bootstrap
        }
        else
        {
            // Spawn host
            HostSingleton hostSingleton = Instantiate(hostPrefab);
            hostSingleton.CreateHost();
            
            // Spawn and initialise client
            ClientSingleton clientSingleton = Instantiate(clientPrefab);
            bool authenticated = await clientSingleton.CreateClient();

            if(authenticated)
            {
                clientSingleton.GameManager.GoToMenu();
            }
            else
            {
                // TODO: handle authentication failure (retry UI, message, etc.)
            }
        }
    }
}
