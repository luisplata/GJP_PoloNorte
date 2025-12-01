using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ProjectileGuideCameraAligned : MonoBehaviour
{
    public Camera cam;          // La cámara principal
    public Transform muzzle;    // La punta del arma (offset)
    public float maxDistance = 100f;
    public LayerMask hitLayers;

    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = 0.04f;
        lr.endWidth = 0.04f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.cyan;
        lr.endColor = Color.cyan;
    }

    void Update()
    {
        // 1) Hacemos un ray desde el centro de la pantalla (cámara)
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        Vector3 targetPoint = ray.origin + ray.direction * maxDistance;

        // 2) Si golpeamos algo, ajustamos el punto final
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, hitLayers))
        {
            targetPoint = hit.point;
        }

        // 3) Dibujamos la línea desde el arma → hacia ese punto
        lr.SetPosition(0, muzzle.position);
        lr.SetPosition(1, targetPoint);
    }
}