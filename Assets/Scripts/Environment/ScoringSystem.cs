using UnityEngine;

public class ScoringSystem : MonoBehaviour
{
    public static float GlobalScoreMultiplier = 1f;
    private int totalScore = 0;

    public void EvaluateStage(StageConfig stage, int stageIndex)
    {
        // placeholder: calcular según tiempo restante, objetivos, daño, etc.
        int baseScore = Mathf.RoundToInt(stage.duration * 10f) + stage.icebergCount * 50;
        int stageScore = Mathf.RoundToInt(baseScore * GlobalScoreMultiplier);
        totalScore += stageScore;
        Debug.Log($"Stage {stageIndex} scored {stageScore}. Total: {totalScore}");
    }
}