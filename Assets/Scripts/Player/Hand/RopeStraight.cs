using UnityEngine;

public class RopeStraight : MonoBehaviour
{
    public LineRenderer lr;
    public Transform origin; // Mano o arma
    public Transform hook;   // Objeto del gancho en movimiento
    public bool isActive;

    void Awake()
    {
        lr.positionCount = 2;
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

        lr.SetPosition(0, origin.position);
        lr.SetPosition(1, hook.position);
    }
}