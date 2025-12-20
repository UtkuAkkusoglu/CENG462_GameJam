using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class ClientNetworkTransform : NetworkTransform
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Sadece sahibi hareket ettirebilir
        CanCommitToTransform = IsOwner;
    }

    protected override bool OnIsServerAuthoritative()
    {
        // Client authoritative mod aktif
        return false;
    }
}
