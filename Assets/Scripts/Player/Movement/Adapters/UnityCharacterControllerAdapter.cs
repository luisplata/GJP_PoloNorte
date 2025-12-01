using UnityEngine;
using GJP_PoloNorte.Domain.Ports;

namespace Adapters
{
    [RequireComponent(typeof(CharacterController))]
    public class UnityCharacterControllerAdapter : MonoBehaviour, ICharacterControllerPort
    {
        public Transform cameraTransform;
        CharacterController _controller;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        public Transform CharacterTransform => transform;
        public Transform CameraTransform => cameraTransform;

        public void Move(Vector3 worldDisplacement)
        {
            _controller.Move(worldDisplacement);
        }

        public void SetRotation(float yaw, float pitch)
        {
            // Apply yaw to the root/player transform
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // If cameraTransform is assigned, apply pitch as local rotation so the camera looks up/down
            if (cameraTransform != null)
            {
                // Warn if camera is not child of the player; localRotation will behave unexpectedly
                if (cameraTransform.parent != transform)
                {
                    Debug.LogWarning($"UnityCharacterControllerAdapter: cameraTransform ({cameraTransform.name}) is not a child of the player root ({name}). Pitch will be applied as localRotation but may not behave as expected.");
                }

                cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                // Debug.Log($"SetRotation called: yaw={yaw} pitch={pitch} -> player.eulerAngles={transform.eulerAngles} camera.localEulerAngles={cameraTransform.localEulerAngles}");
            }
        }

        public bool IsGrounded()
        {
            return _controller.isGrounded;
        }
    }
}
