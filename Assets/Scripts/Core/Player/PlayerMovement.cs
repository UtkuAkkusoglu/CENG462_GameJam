using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform bodyTransform;  // treads/body pivot
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerStats stats;

    [Header("Settings")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float turningRate = 120f;  // degrees per second

    // Eğer Editörden atamayı unutursan diye otomatik bulma garantisi
    public override void OnNetworkSpawn()
    {
        if (stats == null) stats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Her frame property üzerinden hareket inputunu alıyoruz
        Vector2 movementInput = inputReader.Move; 

        // A/D or Left/Right are X on the movement input
        float zRotation = -movementInput.x * turningRate * Time.deltaTime;
        bodyTransform.Rotate(0f, 0f, zRotation);
    }
    
    private void FixedUpdate()
    {
        if (!IsOwner) return;

        Vector2 movementInput = inputReader.Move; 

        // Move forward/back along the tank's facing (bodyTransform.up)
        rb.linearVelocity = (Vector2)bodyTransform.up * movementInput.y * (movementSpeed * stats.SpeedBoostMultiplier); 
    }
}
