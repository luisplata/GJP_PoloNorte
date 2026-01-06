using UnityEngine;
using Adapters;
using Environment;
using GJP_PoloNorte.Domain.UseCases;
using Player.Movement.Domain;

namespace Presentation
{
    // Componente que conecta adaptadores con el caso de uso
    public class PlayerBootstrapper : MonoBehaviour
    {
        [Header("Adapters")] public UnityInputAdapter inputAdapter;
        public UnityCharacterControllerAdapter characterAdapter;

        [Header("Movement Parameters")] public float walkSpeed = 5f;
        public float sprintMultiplier = 1.8f;
        public float mouseSensitivity = 2f;
        public float jumpSpeed = 5f;
        public float gravity = 9.81f;

        [Header("Behavior")] public bool autoAssignAdapters = true;

        PlayerMovementUseCase _useCase;

        private PlayerState _playerState = new();

        public void Config()
        {
            if (autoAssignAdapters)
            {
                if (inputAdapter == null) inputAdapter = FindFirstObjectByType<UnityInputAdapter>();
                if (characterAdapter == null)
                    characterAdapter = FindFirstObjectByType<UnityCharacterControllerAdapter>();
            }

            if (inputAdapter != null && characterAdapter != null)
            {
                _useCase = new PlayerMovementUseCase(_playerState, inputAdapter, characterAdapter, walkSpeed,
                    sprintMultiplier, mouseSensitivity, jumpSpeed, gravity);
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