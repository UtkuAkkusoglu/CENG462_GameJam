using Unity.Netcode;
using UnityEngine;

public class ShipAI : NetworkBehaviour
{
    [Header("Hareket Ayarları")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float turningRate = 120f;
    [SerializeField] private float stopDistance = 8f;

    [Header("Engel Algılama (Radar)")]
    // İŞTE ARADIĞIMIZ AYAR BU:
    [SerializeField] private float obstacleCheckDistance = 4f;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Saldırı Ayarları")]
    [SerializeField] private float attackRange = 15f;
    [SerializeField] private float fireRate = 2f;

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
        if (rb == null || !IsServer || currentTarget == null) return;
        HandleMovement();
    }

    private void HandleMovement()
    {
        Vector2 dir = (currentTarget.position - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, currentTarget.position);

        // 1. DÖNME
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, 0, targetAngle), turningRate * Time.fixedDeltaTime);

        // 2. RADAR (ENGEL VAR MI?)
        bool wallAhead = false;

        // Seçtiğin katmanlara (Default, Island vs.) çarpıyor mu?
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.up, obstacleCheckDistance, obstacleLayers);

        if (hit.collider != null)
        {
            // Tanka (Player'a) çarpmadığı sürece her şeyi duvar say
            if (hit.collider.GetComponent<TankHealth>() == null)
            {
                wallAhead = true;
            }
        }

        // 3. HAREKET
        if (wallAhead)
        {
            rb.linearVelocity = Vector2.zero; // Duvar var, dur.
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

    // GİZMOS: Kırmızı çizgiyi görmek için
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.up * obstacleCheckDistance);
    }

    private void FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float closestDist = attackRange;
        currentTarget = null;
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
            FireServerSide();
            lastFireTime = Time.time;
        }
    }

    private void FireServerSide()
    {
        if (serverProjectilePrefab == null || firePoint == null) return;
        GameObject serverProj = Instantiate(serverProjectilePrefab, firePoint.position, firePoint.rotation);
        IgnoreCollisionWithShip(serverProj.GetComponent<Collider2D>());
        serverProj.GetComponent<NetworkObject>().Spawn();
        SpawnDummyProjectileClientRpc(firePoint.position, firePoint.rotation);
    }

    [ClientRpc]
    private void SpawnDummyProjectileClientRpc(Vector3 pos, Quaternion rot)
    {
        // Host isen mermiyi görmek için buradaki IsServer kontrolünü silebilirsin
        if (clientProjectilePrefab != null)
        {
            GameObject dummyProj = Instantiate(clientProjectilePrefab, pos, rot);
            if (dummyProj.TryGetComponent(out Rigidbody2D rb))
            {
                rb.linearVelocity = dummyProj.transform.up * 15f;
            }
            IgnoreCollisionWithShip(dummyProj.GetComponent<Collider2D>());
            Destroy(dummyProj, 3f);
        }
    }

    private void IgnoreCollisionWithShip(Collider2D bulletCol)
    {
        if (bulletCol == null) return;
        Collider2D[] myColliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in myColliders) Physics2D.IgnoreCollision(col, bulletCol);
    }
}