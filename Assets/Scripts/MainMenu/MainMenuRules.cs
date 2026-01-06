using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuRules : MonoBehaviour
{
    [Tooltip("Indice de la escena según Build Settings")]
    [SerializeField] private int sceneIndex;

    // Llama a esta función (por ejemplo desde un botón) para cambiar a la escena indicada en el inspector
    public void LoadSceneByIndex()
    {
        int maxIndex = SceneManager.sceneCountInBuildSettings - 1;
        if (sceneIndex < 0 || sceneIndex > maxIndex)
        {
            Debug.LogWarning($"Índice de escena {sceneIndex} fuera de rango (0..{maxIndex}).");
            return;
        }

        SceneManager.LoadScene(sceneIndex);
    }
}