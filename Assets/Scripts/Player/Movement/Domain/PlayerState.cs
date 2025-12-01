using UnityEngine;

namespace GJP_PoloNorte.Domain
{
    // Estado simple del jugador mantenido por el dominio
    public class PlayerState
    {
        public Vector3 Position;
        public float Yaw;
        public float Pitch;
        public Vector3 Velocity;
        public bool IsGrounded;
    }
}

