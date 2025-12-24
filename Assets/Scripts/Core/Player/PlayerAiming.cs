using UnityEngine;
using Unity.Netcode;

public class PlayerAiming : NetworkBehaviour
{
    [SerializeField] private Transform turretTransform; // rotating pivot
    [SerializeField] private InputReader inputReader;   // provides AimPosition

    private void LateUpdate()
    {

        if (PauseController.IsMenuOpen) return;

        if (!IsOwner) return; // Only owner aims locally

        // 1) Screen-space cursor
        Vector2 aimScreen = inputReader.AimPosition;

        // 2) Screen → world (for orthographic 2D, z=0 is fine), ScreenToWorldPoint 3D vektör istermiş
        Vector3 aimWorld3 = Camera.main.ScreenToWorldPoint(new Vector3(aimScreen.x, aimScreen.y, 0f)); 
        Vector2 aimWorld = (Vector2)aimWorld3;

        // 3) Direction from turret to cursor
        Vector2 turretPos = (Vector2)turretTransform.position;
        Vector2 dir = aimWorld - turretPos;

        // 4) Rotate turret to face cursor
        turretTransform.up = dir; // use .right if your art faces +X
    }
}
