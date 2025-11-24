using Adapters;
using UnityEngine;
using UnityEditor;

namespace Editor
{
    public static class PlayerCreator
    {
        [MenuItem("GameObject/Create Other/Player FPS Hexagonal", false, 10)]
        public static void CreatePlayer()
        {
            var root = new GameObject("Player");
            root.transform.position = Vector3.zero;

            // CharacterController
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            // Adapters
            var characterAdapter = root.AddComponent<UnityCharacterControllerAdapter>();
            var inputAdapter = root.AddComponent<UnityInputAdapter>();

            // Camera child
            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(root.transform);
            camGo.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            camGo.transform.localRotation = Quaternion.identity;
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";

            characterAdapter.cameraTransform = camGo.transform;

            // Bootstrapper
            var bootstrap = root.AddComponent<Presentation.PlayerBootstrapper>();
            bootstrap.inputAdapter = inputAdapter;
            bootstrap.characterAdapter = characterAdapter;

            Selection.activeGameObject = root;
        }
    }
}
