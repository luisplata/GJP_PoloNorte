using Presentation;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

namespace Environment
{
    public class GameController : MonoBehaviour
    {
        public StageManager stageManager;
        public PlayableDirector director;
        public CinemachineCamera cameraSecondary;
        public PlayerBootstrapper playerBootstrapper;

        private void Start()
        {
            director.Play();

            director.stopped += OnDirectorStopped;
        }

        private void OnDirectorStopped(PlayableDirector obj)
        {
            // Debug.Log($"Director stopped: {obj.name}, starting stage configuration.");
            cameraSecondary.gameObject.SetActive(false);
            stageManager.Configure(this);
            playerBootstrapper.Config();
        }

        public void GameOver()
        {
            Debug.Log("Game Over triggered in GameController.");
            // Aquí puedes agregar la lógica para finalizar el juego
        }
    }
}