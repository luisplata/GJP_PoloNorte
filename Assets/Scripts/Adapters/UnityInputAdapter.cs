using UnityEngine;
using GJP_PoloNorte.Domain.Ports;
using UnityEngine.InputSystem;

namespace Adapters
{
    // Adapter que implementa IInputPort usando el nuevo Input System
    [RequireComponent(typeof(PlayerInput))]
    public class UnityInputAdapter : MonoBehaviour, IInputPort
    {
        public float lookSmoothing = 1f;
        public bool lockCursor = true;
        public Key toggleCursorKey = Key.Escape;

        PlayerInput _playerInput;
        InputAction _moveAction;
        InputAction _lookAction;
        InputAction _jumpAction;
        InputAction _sprintAction;

        Vector2 _lookThisFrame;
        bool _jumpDownThisFrame;

        void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
            {
                Debug.LogError("UnityInputAdapter requiere un PlayerInput en el mismo GameObject.");
                enabled = false;
                return;
            }

            // Usar FindAction para localizar acciones por nombre
            _moveAction = _playerInput.actions.FindAction("Move");
            _lookAction = _playerInput.actions.FindAction("Look");
            _jumpAction = _playerInput.actions.FindAction("Jump");
            _sprintAction = _playerInput.actions.FindAction("Sprint");
        }

        void OnEnable()
        {
            // Habilitar acciones relevantes y suscribirse a eventos
            _moveAction?.Enable();
            _lookAction?.Enable();
            _jumpAction?.Enable();
            _sprintAction?.Enable();

            if (_jumpAction != null) _jumpAction.performed += OnJumpPerformed;

            if (lockCursor) Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = !lockCursor;
        }

        void OnDisable()
        {
            if (_jumpAction != null) _jumpAction.performed -= OnJumpPerformed;

            _moveAction?.Disable();
            _lookAction?.Disable();
            _jumpAction?.Disable();
            _sprintAction?.Disable();
        }

        void OnJumpPerformed(InputAction.CallbackContext ctx)
        {
            _jumpDownThisFrame = true;
        }

        void Update()
        {
            // Toggle cursor with key
            if (Keyboard.current != null && Keyboard.current[toggleCursorKey].wasPressedThisFrame)
            {
                lockCursor = !lockCursor;
                Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !lockCursor;
            }

            if (_lookAction != null) _lookThisFrame = _lookAction.ReadValue<Vector2>();
            // _jumpDownThisFrame now manejado por callback
        }

        public Vector2 Move
        {
            get
            {
                if (_moveAction == null) return Vector2.zero;
                return _moveAction.ReadValue<Vector2>();
            }
        }

        public Vector2 Look
        {
            get
            {
                return _lookThisFrame * lookSmoothing;
            }
        }

        public bool JumpPressed
        {
            get
            {
                var v = _jumpDownThisFrame;
                _jumpDownThisFrame = false;
                return v;
            }
        }

        public bool Sprint
        {
            get
            {
                if (_sprintAction == null)
                {
                    return Keyboard.current != null && Keyboard.current[Key.LeftShift].isPressed;
                }
                return _sprintAction.ReadValue<float>() > 0.5f;
            }
        }
    }
}
