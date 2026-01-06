using System.Collections;
using Environment;
using UnityEngine;

public class PowerupSelector : MonoBehaviour
{
    public PowerupConfig[] options;

    private bool selectionMade = false;
    private PowerupConfig selected;

    public IEnumerator OpenAndWait()
    {
        gameObject.SetActive(true);
        selectionMade = false;
        // TODO: bind UI buttons a Choose(index)

        while (!selectionMade)
            yield return null;

        Apply(selected);
        gameObject.SetActive(false);
    }

    public void Choose(int index)
    {
        if (index < 0 || index >= options.Length) return;
        selected = options[index];
        selectionMade = true;
    }

    private void Apply(PowerupConfig cfg)
    {
        cfg.Apply();
    }
}