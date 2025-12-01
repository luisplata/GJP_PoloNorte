using UnityEngine;
using Adapters;
using GJP_PoloNorte.Domain.UseCases;

namespace Presentation
{
    // Componente que conecta adaptadores con el caso de uso
    public class PlayerBootstrapper : MonoBehaviour
    {
        [Header("Adapters")]
        public UnityInputAdapter inputAdapter;
        public UnityCharacterControllerAdapter characterAdapter;

        [Header("Movement Parameters")]
        public float walkSpeed = 5f;
        public float sprintMultiplier = 1.8f;
        public float mouseSensitivity = 2f;
        public float jumpSpeed = 5f;
        public float gravity = 9.81f;

        [Header("Behavior")]
        public bool autoAssignAdapters = true;

        PlayerMovementUseCase _useCase;

        void Start()
        {
            if (autoAssignAdapters)
            {
                if (inputAdapter == null) inputAdapter = UnityEngine.Object.FindFirstObjectByType<UnityInputAdapter>();
                if (characterAdapter == null) characterAdapter = UnityEngine.Object.FindFirstObjectByType<UnityCharacterControllerAdapter>();
            }

            if (inputAdapter != null && characterAdapter != null)
            {
                _useCase = new PlayerMovementUseCase(inputAdapter, characterAdapter, walkSpeed, sprintMultiplier, mouseSensitivity, jumpSpeed, gravity);
            }
            else
            {
                Debug.LogWarning("PlayerBootstrapper: falta asignar inputAdapter o characterAdapter en la escena.");
            }
        }

        void Update()
        {
            _useCase?.Tick(Time.deltaTime);
        }
    }
}
