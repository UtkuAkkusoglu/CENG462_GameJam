using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;

public class ClientPlayerCamera : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int ownerPriority = 15;

    [SerializeField] private CinemachineCamera cinemachineCamera;

    public override void OnNetworkSpawn()
    {
        if (cinemachineCamera == null) return;

        if (IsOwner)
        {
            // 1. Kendi ekranımızda önceliği artır
            cinemachineCamera.Priority = ownerPriority;

            Debug.Log($"[Camera] 2D Kamera {gameObject.name} için aktif edildi!");
        }
        else
        {
            // 3. Diğer oyuncuların kamerasını devre dışı bırak
            cinemachineCamera.Priority = 10;
        }
    }
}