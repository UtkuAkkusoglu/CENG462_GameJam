using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [SerializeField] private Texture2D crosshairTexture; // PNG'yi buraya sürükle

    void Start()
    {
        // Vector2.zero imlecin tam ortasından nişan almasını sağlar
        Vector2 hotSpot = new Vector2(crosshairTexture.width / 2, crosshairTexture.height / 2);
        Cursor.SetCursor(crosshairTexture, hotSpot, CursorMode.Auto);
    }
}