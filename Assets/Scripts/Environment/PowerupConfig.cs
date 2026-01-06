using UnityEngine;

namespace Environment
{
    [CreateAssetMenu(menuName = "Game/PowerupConfig")]
    public class PowerupConfig : ScriptableObject
    {
        public string powerupName;
        public float extraScoreMultiplier = 0f;
        public float playerSpeedBonus = 0f;
        public float icebergResistBonus = 0f;

        public void Apply()
        {
            if (extraScoreMultiplier != 0f)
                ScoringSystem.GlobalScoreMultiplier += extraScoreMultiplier;
            // if (playerSpeedBonus != 0f)
                // PlayerStats.Instance.ModifySpeed(playerSpeedBonus);
            // if (icebergResistBonus != 0f)
                // GameModifiers.Instance.ModifyIcebergResistance(icebergResistBonus);
        }
    }
}