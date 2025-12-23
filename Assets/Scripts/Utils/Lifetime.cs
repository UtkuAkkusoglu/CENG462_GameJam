using Unity.Netcode;
using UnityEngine;

public class Lifetime : NetworkBehaviour
{
    [SerializeField] private float lifeTime = 3f; // Kaç saniye yaþasýn?

    // Start yerine OnNetworkSpawn kullanmak Netcode için daha güvenlidir
    public override void OnNetworkSpawn()
    {
        // Sadece SUNUCU bu sayacý çalýþtýrsýn
        if (IsServer)
        {
            // Unity'nin Destroy fonksiyonu yerine özel bir Coroutine (zamanlayýcý) baþlatýyoruz
            Invoke(nameof(YokEt), lifeTime);
        }
    }

    private void YokEt()
    {
        // Eðer obje hala hayattaysa
        if (IsSpawned)
        {
            // NetworkObject'i DESPAWN ediyoruz (Bu, tüm clientlardan siler)
            GetComponent<NetworkObject>().Despawn();
        }
    }
}