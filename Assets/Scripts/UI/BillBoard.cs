using UnityEngine;

public class BillBoard : MonoBehaviour
{
    private Transform mainCamTransform;

    void Start()
    {
        if (Camera.main != null) mainCamTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (mainCamTransform != null)
        {
            // Barýn rotasyonunu kameraya eþitle (Hep düz dursun)
            transform.rotation = mainCamTransform.rotation;
        }
    }
}