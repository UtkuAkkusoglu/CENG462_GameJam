using UnityEngine;
using Unity.Netcode;

public class ProjectileLauncher : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] GameObject serverProjectilePrefab;
    [SerializeField] GameObject clientProjectilePrefab;
    [SerializeField] Transform projectileSpawnPoint;  // where the projectile appears (tip of barrel)
    [SerializeField] InputReader inputReader; // for reading the fire input event
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private Collider2D playerCollider; // to ignore self-collisions

    [Header("Settings")]
    [SerializeField] float projectileSpeed;
    [SerializeField] private float fireRate; // saniyedeki atış sayısı
    [SerializeField] private float muzzleFlashDuration; // flash'ın görünür kalma süresi (saniye)

    private bool shouldFire;
    private float previousFireTime;
    private float muzzleFlashTimer;

    private void Update()
    {
        if (muzzleFlashTimer > 0)     // herhalde dummy projectile aksine server onayı beklemez, herkes aynı anda görür
        {
            muzzleFlashTimer -= Time.deltaTime;
        }
        else
        {
            muzzleFlash.SetActive(false);
        }
        
        if (!IsOwner) return;
        if (!shouldFire) return;

        float timeBetweenShots = 1f / fireRate;
        if (Time.time < previousFireTime + timeBetweenShots)
            return; // yeterli süre geçmedi, henüz ateş edemez
        
        Vector3 spawnPos = projectileSpawnPoint.position;
        Vector3 direction = projectileSpawnPoint.up;

        SpawnDummyProjectile(spawnPos, direction);
        PrimaryFireServerRpc(spawnPos, direction);
        previousFireTime = Time.time;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        inputReader.PrimaryFireEvent += HandlePrimaryFire;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        inputReader.PrimaryFireEvent -= HandlePrimaryFire;
    }

    private void HandlePrimaryFire(bool isPressed)
    {
        shouldFire = isPressed;
    }

    private void SpawnDummyProjectile(Vector3 spawnPos, Vector3 direction)
    {
        GameObject projectileInstance = Instantiate(clientProjectilePrefab, spawnPos, Quaternion.identity);
        projectileInstance.transform.up = direction;

        // --- GÜVENLİ ÇARPIŞMA İPTALİ ---
        // Önce mermide Collider var mı diye bakıyoruz. Yoksa hata vermeden geçiyoruz.
        Collider2D mermiCollider = projectileInstance.GetComponent<Collider2D>();
        if (mermiCollider != null && playerCollider != null)
        {
            Physics2D.IgnoreCollision(playerCollider, mermiCollider);
        }

        // --- HIZ VERME ---
        if (projectileInstance.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = projectileInstance.transform.up * projectileSpeed;
        }

        // Efektleri aç
        muzzleFlash.SetActive(true);
        muzzleFlashTimer = muzzleFlashDuration;
    }

    // Owner client çağırıyor, ama metodun gövdesi sunucuda çalışıyor çünkü [ServerRpc] o yüzden içindeki metot tüm clientlar için çağırılır.
    [ServerRpc]
    private void PrimaryFireServerRpc(Vector3 spawnPos, Vector3 direction, ServerRpcParams serverRpcParams = default)
    {
        // 1. Mermiyi Yarat
        GameObject projectileInstance = Instantiate(serverProjectilePrefab, spawnPos, Quaternion.identity);
        projectileInstance.transform.up = direction;

        // 2. Fiziksel Çarpışmayı Yoksay (Tank kendine çarpmasın)
        Physics2D.IgnoreCollision(playerCollider, projectileInstance.GetComponent<Collider2D>());

        // 3. Hız Ver
        if (projectileInstance.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = projectileInstance.transform.up * projectileSpeed;
        }

        // 4. KRİTİK NOKTA: Mermiyi Ağa Tanıt ve SAHİBİNİ BELİRLE
        // serverRpcParams.Receive.SenderClientId -> Bu fonksiyonu çağıran (ateş eden) kişinin ID'sidir.
        var netObj = projectileInstance.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(serverRpcParams.Receive.SenderClientId);

        // 5. Diğerlerine de dummy mermi yaratmasını söyle
        SpawnDummyProjectileClientRpc(spawnPos, direction);
    }

    // Bu attribute([ClientRpc]), methodun sunucudan tüm client’lara çağrılacağını belirtiyor.
    [ClientRpc]
    private void SpawnDummyProjectileClientRpc(Vector3 spawnPos, Vector3 direction)
    {
        if (IsOwner)
        {
            return;  // Ateşleyen client zaten kendi dummy’sini spawnladı.
        }
        
        SpawnDummyProjectile(spawnPos, direction);
    }
}
