using UnityEngine;

[CreateAssetMenu(menuName = "Game/StageConfig")]
public class StageConfig : ScriptableObject
{
    public string stageName;
    [Tooltip("Duración en segundos")]
    public float duration = 60f;

    public int icebergCount;
    public IcebergBehavior icebergBehavior = IcebergBehavior.Static;

    [Header("Sun (melting)")]
    public bool hasSun;
    public float sunHeat = 0f; // intensidad que acelera el derretimiento

    [Header("Tide (movement)")]
    public bool hasTide;
    public float tideAmplitude = 0f;
    public float tideFrequency = 0f;
}