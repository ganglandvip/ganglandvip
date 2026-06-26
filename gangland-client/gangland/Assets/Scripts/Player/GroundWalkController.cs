using UnityEngine;
using UnityEngine.InputSystem;

namespace Gangland.Player
{
    public sealed class GroundWalkController : MonoBehaviour
    {
        [SerializeField] float eyeHeightMeters = 1.75f;
        [SerializeField] float walkSpeedMetersPerSecond = 5.5f;
        [SerializeField] float sprintSpeedMetersPerSecond = 11f;
        [SerializeField] float mouseSensitivity = 0.12f;
        [SerializeField] float minPitch = -75f;
        [SerializeField] float maxPitch = 75f;

        float yaw;
        float pitch;

        void Awake()
        {
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = NormalizePitch(euler.x);
            SetGroundHeight();
        }

        void Update()
        {
            HandleCursorLock();
            HandleLook();
            HandleMovement();
            SetGroundHeight();
        }

        void HandleCursorLock()
        {
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        void HandleLook()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, minPitch, maxPitch);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        void HandleMovement()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                input.y += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                input.y -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                input.x += 1f;
            }

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                input.x -= 1f;
            }

            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 right = transform.right;
            right.y = 0f;
            right.Normalize();

            float speed = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed
                ? sprintSpeedMetersPerSecond
                : walkSpeedMetersPerSecond;

            Vector3 movement = (forward * input.y + right * input.x) * (speed * Time.deltaTime);
            transform.position += movement;
        }

        void SetGroundHeight()
        {
            Vector3 position = transform.position;
            position.y = eyeHeightMeters;
            transform.position = position;
        }

        static float NormalizePitch(float value)
        {
            return value > 180f ? value - 360f : value;
        }
    }
}
