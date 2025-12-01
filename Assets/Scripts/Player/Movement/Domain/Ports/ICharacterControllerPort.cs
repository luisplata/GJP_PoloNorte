using UnityEngine;

namespace GJP_PoloNorte.Domain.Ports
{
    public interface ICharacterControllerPort
    {
        Transform CharacterTransform { get; }
        Transform CameraTransform { get; }

        void Move(Vector3 worldDisplacement);
        void SetRotation(float yaw, float pitch);
        bool IsGrounded();
    }
}

