using Unity.Netcode;
using UnityEngine;

public class JeepAI : NetworkBehaviour
{
    [Header("Hareket Ayarlarý")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float turningRate = 180f; // Jeep daha kývrak olsun
    [SerializeField] private float stopDistance = 6f;

    [Header("Akýllý Radar (Sudan Kaçýþ)")]
    [SerializeField] private float obstacleCheckDistance = 3f;
    [SerializeField] private int rayCount = 5; // Önünü tarayan ýþýn sayýsý
    [SerializeField] private float fovAngle = 120f; // Tarama açýsý
    [SerializeField] private LayerMask avoidLayers; // SU KATMANI BURAYA SEÇÝLECEK

    [Header("Saldýrý Ayarlarý")]
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float fireRate = 1.5f;

    [Header("Referanslar")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject serverProjectilePrefab;
    [SerializeField] private GameObject clientProjectilePrefab;

    private Transform currentTarget;
    private float lastFireTime;
    private Rigidbody2D rb;

    public override void OnNetworkSpawn() => rb = GetComponent<Rigidbody2D>();

    private void Update()
    {
        if (!IsServer) return;
        FindNearestPlayer();
        if (currentTarget != null) TryShoot();
    }

    private void FixedUpdate()
    {
        if (rb == null || !IsServer) return;

        // Hedef yoksa dur (Sonsuza gitmesin)
        if (currentTarget == null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            return;
        }

        // --- SUYA GÝRDÝM MÝ KONTROLÜ (FAIL-SAFE) ---
        // Eðer yanlýþlýkla suya girerse (OverlapCircle)
        Collider2D inWater = Physics2D.OverlapCircle(transform.position, 0.5f, avoidLayers);
        if (inWater != null)
        {
            rb.linearVelocity = Vector2.zero; // Dur!
            // Geriye doðru hafif itme
            Vector2 pushBack = (Vector2.zero - (Vector2)transform.position).normalized;
            rb.AddForce(pushBack * 5f, ForceMode2D.Impulse);
            return;
        }

        HandleMovement();
    }

    private void HandleMovement()
    {
        Vector2 dir = (currentTarget.position - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, currentTarget.position);

        // 1. DÖNME
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, 0, targetAngle), turningRate * Time.fixedDeltaTime);

        // 2. RADAR KONTROLÜ (Önümde Su Var mý?)
        bool dangerAhead = CheckFrontalObstacles();

        // 3. HAREKET
        if (dangerAhead)
        {
            rb.linearVelocity = Vector2.zero; // Su var, dur.
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

    // --- YELPAZE RADAR SÝSTEMÝ ---
    private bool CheckFrontalObstacles()
    {
        float angleStep = fovAngle / (rayCount - 1);
        float startAngle = -fovAngle / 2f;

        for (int i = 0; i < rayCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Vector3 direction = Quaternion.Euler(0, 0, currentAngle) * transform.up;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, obstacleCheckDistance, avoidLayers);

            if (hit.collider != null)
            {
                // Tanka veya baþka Jeep'e çarpmadýðýmýz sürece dur
                if (hit.collider.GetComponent<TankHealth>() == null && hit.collider.GetComponent<JeepAI>() == null)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        float angleStep = fovAngle / (rayCount - 1);
        float startAngle = -fovAngle / 2f;
        for (int i = 0; i < rayCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Vector3 direction = Quaternion.Euler(0, 0, currentAngle) * transform.up;
            Gizmos.DrawRay(transform.position, direction * obstacleCheckDistance);
        }
    }

    // ... (Ateþ etme kodlarý ayný) ...
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
        IgnoreCollisionWithJeep(serverProj.GetComponent<Collider2D>());
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