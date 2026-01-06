using UnityEngine;
using GJP_PoloNorte.Domain.Ports;
using Player.Movement.Domain;

namespace GJP_PoloNorte.Domain.UseCases
{
    public class PlayerMovementUseCase
    {
        readonly IInputPort _input;
        readonly ICharacterControllerPort _actor;

        readonly float _mouseSensitivity;
        readonly float _jumpSpeed;
        readonly float _gravity;

        float _verticalVelocity;
        float _pitch;
        float _yaw;

        private PlayerState _playerState;

        public PlayerMovementUseCase(PlayerState playerState, IInputPort input, ICharacterControllerPort actor,
            float walkSpeed = 5f, float sprintMultiplier = 1.8f, float mouseSensitivity = 2f,
            float jumpSpeed = 5f, float gravity = 9.81f)
        {
            _input = input;
            _actor = actor;
            _mouseSensitivity = mouseSensitivity;
            _jumpSpeed = jumpSpeed;
            _gravity = gravity;

            _playerState = playerState;
            _playerState.WalkSpeed = walkSpeed;
            _playerState.SprintMultiplier = sprintMultiplier;

            // Inicializar yaw al valor actual del actor para acumular correctamente
            if (_actor?.CharacterTransform != null) _yaw = _actor.CharacterTransform.eulerAngles.y;
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;

            // Rotation: aplicar pitch (mirada vertical) y también yaw (mirada horizontal)
            var look = _input.Look * _mouseSensitivity;
            // Pitch
            _pitch -= look.y;
            _pitch = Mathf.Clamp(_pitch, -89f, 89f);

            // Yaw acumulado
            _yaw += look.x;
            // Aplicar rotación: yaw en el actor, pitch en la cámara
            _actor.SetRotation(_yaw, _pitch);

            // Movement in local space: usar la cámara si está disponible para orientar el movimiento
            var move = _input.Move;
            var basis = _actor.CameraTransform != null ? _actor.CameraTransform : _actor.CharacterTransform;
            var forward = basis.forward;
            var right = basis.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            var speed = _playerState.WalkSpeed * (_input.Sprint ? _playerState.SprintMultiplier : 1f);
            var horizontalVelocity = (forward * move.y + right * move.x) * speed;

            // Ground check
            var grounded = _actor.IsGrounded();
            if (grounded && _verticalVelocity < 0f) _verticalVelocity = -1f;

            // Jump
            if (_input.JumpPressed && grounded)
            {
                _verticalVelocity = _jumpSpeed;
            }

            // Gravity
            _verticalVelocity -= _gravity * dt;

            var finalVelocity = horizontalVelocity + Vector3.up * _verticalVelocity;
            _actor.Move(finalVelocity * dt);
        }
    }
}