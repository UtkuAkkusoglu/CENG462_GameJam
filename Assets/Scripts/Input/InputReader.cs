using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputReader", menuName = "Input/Input Reader")]
public class InputReader : ScriptableObject, Controls.IPlayerActions
{
    private Controls _controls;

    public event Action<bool> PrimaryFireEvent;

    public Vector2 Move { get; private set; }
    public Vector2 AimPosition { get; private set; }

    private void OnEnable()
    {
        if (_controls == null)
        {
            _controls = new Controls();
            _controls.Player.SetCallbacks(this);
        }
        _controls.Player.Enable();
    }

    private void OnDisable()
    {
        if (_controls != null)
            _controls.Player.Disable();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        Move = context.ReadValue<Vector2>();
    }

    public void OnPrimaryFire(InputAction.CallbackContext context)
    {
        if (context.performed) PrimaryFireEvent?.Invoke(true);  // Kendisine subscribe olan metotları çağırıyor, eğer abone yoksa patlamasın diye null check yapıyoruz
        if (context.canceled) PrimaryFireEvent?.Invoke(false);
    }

    public void OnAim(InputAction.CallbackContext context) // Screen-space (pixel) position
    {
        AimPosition = context.ReadValue<Vector2>();
    }
}