using UnityEngine;

public class RopeWithWave : MonoBehaviour
{
    public LineRenderer lr;
    public Transform origin;
    public Transform hook;
    public bool isActive = false;

    [Header("Wave Settings")] public int quality = 20; // Número de segmentos
    public float waveSize = 0.2f; // Amplitud de la onda
    public float waveFrequency = 2f; // Velocidad de oscilación
    public float straightTime = 0.2f; // Tiempo para volverse recta al final

    float waveProgress = 0f; // 0 = recta, 1 = onda máxima

    void Awake()
    {
        lr.positionCount = quality;
        lr.enabled = false;
    }

    void Update()
    {
        if (!isActive)
        {
            lr.enabled = false;
            return;
        }

        if (!lr.enabled)
            lr.enabled = true;

        // Aumenta la onda mientras está viajando
        waveProgress = Mathf.Clamp01(waveProgress + Time.deltaTime);

        Vector3 start = origin.position;
        Vector3 end = hook.position;
        
        if (lr.positionCount != quality)
        {
            lr.positionCount = quality;
        }

        for (int i = 0; i < quality; i++)
        {
            float t = i / (float)(quality - 1);
            Vector3 pos = Vector3.Lerp(start, end, t);

            // Onda perpendicular al trayecto
            Vector3 offset = Vector3.Cross((end - start).normalized, Vector3.up);

            pos += offset * (Mathf.Sin(t * waveFrequency + Time.time * 10f) * waveSize * waveProgress);

            lr.SetPosition(i, pos);
        }

        // Cuando ya llegó al destino => recta
        if (Vector3.Distance(hook.position, end) < 0.1f)
        {
            waveProgress = Mathf.MoveTowards(waveProgress, 0f, Time.deltaTime / straightTime);
        }
    }
}