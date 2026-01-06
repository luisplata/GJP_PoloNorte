using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Environment
{
    public class StageManager : MonoBehaviour
    {
        public List<StageConfig> stages;
        public PowerupSelector powerupSelector;
        public ScoringSystem scoringSystem;

        [Tooltip("Intervalo en segundos para hacer logs de progreso de la etapa")]
        public float stageProgressLogInterval = 5f;

        public static event Action<StageConfig> OnStageStart;
        public static event Action<StageConfig> OnStageEnd;
        public static event Action OnAllStagesComplete;

        private int _currentIndex = 0;

        private void Configure()
        {
            Debug.Log($"[StageManager] Iniciado. Total stages: {stages?.Count ?? 0}");
            StartCoroutine(RunStages());
        }

        private IEnumerator RunStages()
        {
            for (_currentIndex = 0; _currentIndex < stages.Count; _currentIndex++)
            {
                var stage = stages[_currentIndex];
                Debug.Log($"[StageManager] Preparando stage {_currentIndex + 1}/{stages.Count}: {stage.stageName} - Duración: {stage.duration}s, IcebergCount: {stage.icebergCount}");
                OnStageStart?.Invoke(stage);

                float elapsed = 0f;
                float nextLogTime = stageProgressLogInterval;

                while (elapsed < stage.duration)
                {
                    elapsed += Time.deltaTime;

                    if (elapsed >= nextLogTime)
                    {
                        float remaining = Mathf.Max(0f, stage.duration - elapsed);
                        Debug.Log($"[StageManager] Stage {_currentIndex + 1} progreso: {elapsed:F1}s / {stage.duration:F1}s (restan {remaining:F1}s)");
                        nextLogTime += Mathf.Max(0.1f, stageProgressLogInterval);
                    }

                    yield return null;
                }

                Debug.Log($"[StageManager] Stage {_currentIndex + 1} finalizado.");
                OnStageEnd?.Invoke(stage);

                // calcular puntuación para la etapa
                Debug.Log($"[StageManager] Evaluando puntuación para stage {_currentIndex + 1}...");
                scoringSystem.EvaluateStage(stage, _currentIndex);
                Debug.Log($"[StageManager] Evaluación de puntuación completada para stage {_currentIndex + 1}.");

                // Si no es la última etapa, abrir selector y esperar selección
                if (_currentIndex < stages.Count - 1)
                {
                    Debug.Log($"[StageManager] Abriendo PowerupSelector antes de la siguiente etapa...");
                    yield return StartCoroutine(powerupSelector.OpenAndWait());
                    Debug.Log($"[StageManager] PowerupSelector cerrado. Continuando a la siguiente etapa.");
                }
            }

            Debug.Log("[StageManager] Todas las etapas completadas.");
            OnAllStagesComplete?.Invoke();
        }
    }
}