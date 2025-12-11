using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ProjectileGuideCameraAligned : MonoBehaviour
{
    public Camera cam; // La cámara principal
    public Transform muzzle; // La punta del arma (offset)
    public float maxDistance = 100f;
    public LayerMask hitLayers;

    [Header("UI Target (optional)")]
    [Tooltip("Si se habilita, el script usará este Transform como objetivo en lugar de raycast desde la cámara.")]
    public bool useTargetFromUI = false;

    public Transform uiTargetTransform; // apunta al puntero de la UI en el mundo (si lo tienes)

    [Tooltip(
        "Si false, se seguirá usando Physics.Raycast entre segmentos; si true, se omitirá la detección por raycast.")]
    public bool disablePhysicsCollisions = false;

    [Header("Trajectory Settings")] public float initialSpeed = 15f; // velocidad inicial del lanzamiento
    public int quality = 20; // Número de segmentos de la línea
    public float gravityScale = 1f; // multiplicador de gravedad para la simulación

    public enum TrajectoryMode
    {
        Physics,
        Linear
    }

    [Tooltip("Physics = incluye gravedad. Linear = sin gravedad (trayectoria recta).")]
    public TrajectoryMode trajectoryMode = TrajectoryMode.Physics;

    [Header("Wave Settings")] public float waveSize = 0.2f; // Amplitud de la onda
    public float waveFrequency = 2f; // Velocidad de oscilación
    public float straightTime = 0.2f; // Tiempo para volverse recta al final

    private LineRenderer _lr;
    private float _waveProgress;

    void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        _lr.positionCount = quality;
        _lr.startWidth = 0.04f;
        _lr.endWidth = 0.04f;
        _lr.material = new Material(Shader.Find("Sprites/Default"));
        _lr.startColor = Color.cyan;
        _lr.endColor = Color.cyan;
    }

    void Update()
    {
        if (cam == null || muzzle == null)
        {
            _lr.enabled = false;
            return;
        }

        if (!_lr.enabled) _lr.enabled = true;

        // Aumenta la onda mientras se está apuntando
        _waveProgress = Mathf.Clamp01(_waveProgress + Time.deltaTime);

        // Direccion objetivo: rayo desde el centro de la pantalla
        Vector3 aimDir;
        float targetDistance = maxDistance;
        Vector3 explicitTarget = Vector3.zero;

        if (useTargetFromUI && uiTargetTransform != null)
        {
            explicitTarget = uiTargetTransform.position;
            aimDir = (explicitTarget - muzzle.position).normalized;
            targetDistance = Mathf.Min(maxDistance, Vector3.Distance(muzzle.position, explicitTarget));
        }
        else
        {
            Ray aimRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            aimDir = aimRay.direction.normalized;
        }

        // Velocidad inicial de la simulación
        Vector3 initialVelocity = aimDir * initialSpeed;

        // Estimamos un tiempo máximo para la simulación (para no iterar indefinidamente)
        float totalTime = Mathf.Max(0.1f, targetDistance / initialSpeed);

        Vector3 prevPos = muzzle.position;
        bool hitDetected = false;
        Vector3 hitPoint = Vector3.zero;

        // Aseguramos el count
        if (_lr.positionCount != quality) _lr.positionCount = quality;

        for (int i = 0; i < quality; i++)
        {
            float t = i / (float)(quality - 1);
            float time = t * totalTime;

            // Calculamos la posición según el modo de trayectoria (extraído en función)
            Vector3 pos = CalculateTrajectoryPoint(muzzle.position, initialVelocity, time);

            // Dirección local (derivada aproximada) para calcular perpendicular
            Vector3 velocityAtT = CalculateVelocityAtTime(initialVelocity, time);

            // Offset de onda perpendicular a la dirección de vuelo
            Vector3 offsetDir = Vector3.Cross(velocityAtT.normalized, Vector3.up);
            if (offsetDir.sqrMagnitude < 0.001f)
            {
                // Si está cerca de vertical, usamos la derecha de la cámara para mayor estabilidad
                offsetDir = cam.transform.right;
            }

            offsetDir.Normalize();

            // Atenuación de la onda hacia la punta (para que quede recta al final)
            float attenuation = Mathf.Lerp(1f, 0f, t);

            float wave = Mathf.Sin(t * waveFrequency * Mathf.PI * 2f + Time.time * waveFrequency) * waveSize *
                         attenuation * _waveProgress;
            pos += offsetDir * wave;

            // Detección de colisiones entre prevPos y pos (opcional si usas UI target)
            Vector3 segmentDir = pos - prevPos;
            if (!hitDetected)
            {
                float segmentLen = segmentDir.magnitude;
                if (segmentLen > 1e-6f)
                {
                    if (!disablePhysicsCollisions)
                    {
                        Vector3 segmentDirNorm = segmentDir / segmentLen;
                        if (Physics.Raycast(prevPos, segmentDirNorm, out RaycastHit hit, segmentLen, hitLayers))
                        {
                            pos = hit.point;
                            hitDetected = true;
                            hitPoint = hit.point;
                        }
                    }

                    // Si se está usando target UI, comprobamos llegada aproximada al objetivo y lo marcamos como 'impacto'
                    if (useTargetFromUI && uiTargetTransform != null && Vector3.Distance(pos, explicitTarget) <= 0.05f)
                    {
                        pos = explicitTarget;
                        hitDetected = true;
                        hitPoint = explicitTarget;
                    }
                }
            }

            _lr.SetPosition(i, pos);

            prevPos = pos;

            if (hitDetected)
            {
                // Si ya impactamos, rellenamos el resto de puntos con el punto de impacto
                for (int j = i + 1; j < quality; j++)
                {
                    _lr.SetPosition(j, hitPoint);
                }

                break;
            }
        }

        // Si golpeamos un objetivo, reducimos la onda hacia 0
        if (hitDetected)
        {
            _waveProgress = Mathf.MoveTowards(_waveProgress, 0f, Time.deltaTime / Mathf.Max(0.0001f, straightTime));
        }
    }

    // Calcula la posición en el tiempo `time` desde `start` con `initialVelocity`.
    // Si la modalidad es Linear, no aplica gravedad.
    private Vector3 CalculateTrajectoryPoint(Vector3 start, Vector3 initialVelocity, float time)
    {
        if (trajectoryMode == TrajectoryMode.Linear)
        {
            return start + initialVelocity * time;
        }

        Vector3 grav = Physics.gravity * gravityScale;
        float halfT2 = 0.5f * time * time; // optimización para evitar multiplicaciones repetidas
        return start + initialVelocity * time + grav * halfT2;
    }

    // Calcula la velocidad en el tiempo `time` (útil para orientación/offset de la onda).
    private Vector3 CalculateVelocityAtTime(Vector3 initialVelocity, float time)
    {
        if (trajectoryMode == TrajectoryMode.Linear)
        {
            return initialVelocity; // constante si no hay gravedad
        }

        Vector3 grav = Physics.gravity * gravityScale;
        return initialVelocity + grav * time;
    }
}