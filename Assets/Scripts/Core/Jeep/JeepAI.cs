using Unity.Netcode;
using UnityEngine;

public class JeepAI : NetworkBehaviour
{
    [Header("Hareket Ayarlarý")]
    [SerializeField] private float moveSpeed = 6f;      // Jeep gemiden hýzlý olsun
    [SerializeField] private float turningRate = 150f;  // Daha kývrak dönsün
    [SerializeField] private float stopDistance = 6f;   // Tanka daha çok yaklaþsýn

    [Header("Sudan Kaçýþ (Radar)")]
    [SerializeField] private float obstacleCheckDistance = 3f;
    [SerializeField] private LayerMask avoidLayers;     // Neye çarpýnca dursun? (SU)

    [Header("Saldýrý Ayarlarý")]
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float fireRate = 1.5f;     // Daha seri ateþ etsin

    [Header("Referanslar")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject serverProjectilePrefab;
    [SerializeField] private GameObject clientProjectilePrefab;

    private Transform currentTarget;
    private float lastFireTime;
    private Rigidbody2D rb;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!IsServer) return;
        FindNearestPlayer();
        if (currentTarget != null) TryShoot();
    }

    private void FixedUpdate()
    {
        // Temel kontroller
        if (rb == null || !IsServer) return;

        // SORUNUN ÇÖZÜMÜ BURADA:
        // Eðer hedef yoksa (menzilden çýktýysa)
        if (currentTarget == null)
        {
            // Motorlarý kapat ve dur
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            return; // Þimdi çýkabilirsin
        }

        // Hedef varsa kovalamaya devam et
        HandleMovement();
    }

    private void HandleMovement()
    {
        Vector2 dir = (currentTarget.position - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, currentTarget.position);

        // 1. DÖNME
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, 0, targetAngle), turningRate * Time.fixedDeltaTime);

        // 2. RADAR (Önümde SU var mý?)
        bool dangerAhead = false;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.up, obstacleCheckDistance, avoidLayers);

        if (hit.collider != null)
        {
            // Tanka veya baþka Jeep'e deðil, sadece belirlediðimiz engele çarpýyorsa
            if (hit.collider.GetComponent<TankHealth>() == null && hit.collider.GetComponent<JeepAI>() == null)
            {
                dangerAhead = true;
            }
        }

        // 3. HAREKET
        if (dangerAhead)
        {
            rb.linearVelocity = Vector2.zero; // Su var! Fren yap.
        }
        else if (distance > stopDistance)
        {
            rb.linearVelocity = (Vector2)transform.up * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
        rb.angularVelocity = 0f;
    }

    // Gizmos: Kýrmýzý çizgiyi gör
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow; // Jeep'in radarý sarý olsun
        Gizmos.DrawRay(transform.position, transform.up * obstacleCheckDistance);
    }

    // --- BURADAN AÞAÐISI GEMÝ ÝLE AYNI (Kopyala/Yapýþtýr) ---
    private void FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float closestDist = attackRange; currentTarget = null;
        foreach (var p in players)
        {
            float d = Vector2.Distance(transform.position, p.transform.position);
            if (d < closestDist) { closestDist = d; currentTarget = p.transform; }
        }
    }
    private void TryShoot()
    {
        Vector2 dir = currentTarget.position - transform.position;
        if (Vector3.Angle(transform.up, dir) < 20f && Time.time >= lastFireTime + fireRate)
        {
            FireServerSide(); lastFireTime = Time.time;
        }
    }
    private void FireServerSide()
    {
        if (serverProjectilePrefab == null || firePoint == null) return;
        GameObject serverProj = Instantiate(serverProjectilePrefab, firePoint.position, firePoint.rotation);
        IgnoreCollisionWithJeep(serverProj.GetComponent<Collider2D>()); // Ýsim deðiþti
        serverProj.GetComponent<NetworkObject>().Spawn();
        SpawnDummyProjectileClientRpc(firePoint.position, firePoint.rotation);
    }
    [ClientRpc]
    private void SpawnDummyProjectileClientRpc(Vector3 pos, Quaternion rot)
    {
        if (clientProjectilePrefab != null)
        {
            GameObject dummyProj = Instantiate(clientProjectilePrefab, pos, rot);
            if (dummyProj.TryGetComponent(out Rigidbody2D rb)) rb.linearVelocity = dummyProj.transform.up * 15f;
            IgnoreCollisionWithJeep(dummyProj.GetComponent<Collider2D>());
            Destroy(dummyProj, 3f);
        }
    }
    private void IgnoreCollisionWithJeep(Collider2D bulletCol)
    {
        if (bulletCol == null) return;
        Collider2D[] myColliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in myColliders) Physics2D.IgnoreCollision(col, bulletCol);
    }
}