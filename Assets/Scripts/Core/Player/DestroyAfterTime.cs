using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    [SerializeField] private float timeToDestroy = 1f; // 1 saniye sonra yok olsun (Kýsa süreli)

    private void Start()
    {
        Destroy(gameObject, timeToDestroy);
    }
}