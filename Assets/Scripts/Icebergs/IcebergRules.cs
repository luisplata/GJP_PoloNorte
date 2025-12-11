using System.Collections.Generic;
using Icebergs;
using UnityEngine;

public class IcebergRules : MonoBehaviour
{
    [SerializeField] private float timeStepToCheck;
    [SerializeField] private float limitDepth;
    [SerializeField] private float maxRiseHeight;
    [SerializeField] private float velocityToFall;
    [SerializeField] private int numberOfAnimalsOnIceberg;
    [SerializeField] private List<Iceberg> icebergs;
    [SerializeField] private int icebergsToLose;

    private int _icebergsLost;

    private void Start()
    {
        foreach (var iceberg in icebergs)
        {
            //Random entre 1 y el numero máximo de animales
            int numAnimals = Random.Range(1, numberOfAnimalsOnIceberg + 1);
            iceberg.Configure(timeStepToCheck, limitDepth, maxRiseHeight, velocityToFall, numAnimals);
            iceberg.OnLoseIceberg += HandleIcebergLost;
        }
    }

    private void HandleIcebergLost()
    {
        _icebergsLost++;
        if (_icebergsLost >= icebergsToLose)
        {
            Debug.Log("Game Over: Too many icebergs lost!");
            // Aquí puedes agregar la lógica para finalizar el juego
        }
    }
}