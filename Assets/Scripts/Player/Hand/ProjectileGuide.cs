using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ProjectileGuide : MonoBehaviour
{
    [Header("Ray Settings")]
    public Transform origin;           // Desde donde sale el disparo
    public float distance = 20f;       // Alcance del trayecto
    public LayerMask hitLayers;        // Layers que pueden bloquear el raycast

    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();

        // Configuración básica del LineRenderer
        lr.positionCount = 2;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;

        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.cyan;
        lr.endColor = Color.cyan;
    }

    void Update()
    {
        Vector3 start = origin.position;
        Vector3 direction = origin.forward;
        Vector3 end = start + direction * distance;

        // Si colisiona con algo, termina allí
        if (Physics.Raycast(start, direction, out RaycastHit hit, distance, hitLayers))
        {
            end = hit.point;
        }

        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }
}