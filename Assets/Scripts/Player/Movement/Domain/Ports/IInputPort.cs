using UnityEngine;

namespace GJP_PoloNorte.Domain.Ports
{
    public interface IInputPort
    {
        Vector2 Move { get; }      // x = strafe, y = forward
        Vector2 Look { get; }      // x = mouseX, y = mouseY
        bool JumpPressed { get; }  // pressed this frame
        bool Sprint { get; }       // sustained
    }
}

