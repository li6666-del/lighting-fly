using UnityEngine;

public static class DifficultyManager
{
    public static int Score => Mathf.Max(0, ScoreManager.score);
    public static int Level => Score / 100;

    public static float SpawnIntervalMultiplier => Mathf.Max(0.38f, 1f - Level * 0.07f);
    public static int EnemyWaveBonus => Mathf.Min(Level / 2, 4);
    public static float ExtraEnemyChanceBonus => Mathf.Min(Level * 0.06f, 0.45f);

    public static float EnemySpeedMultiplier => 1f + Mathf.Min(Level * 0.12f, 2f);
    public static float EnemyFireIntervalMultiplier => Mathf.Max(0.42f, 1f - Level * 0.06f);

    public static float BossHealthMultiplier => 1f + Mathf.Min((Score / 200) * 0.18f, 1.8f);
    public static float BossMoveMultiplier => 1f + Mathf.Min(Level * 0.06f, 0.7f);
    public static float BossFireIntervalMultiplier => Mathf.Max(0.45f, 1f - Level * 0.045f);
    public static int BossFanBulletBonus => Mathf.Min((Score / 200) * 2, 14);

    public static float GetBossOverflowHealthMultiplier(int requestedBossCount, int maxBossCount)
    {
        int overflow = Mathf.Max(0, requestedBossCount - maxBossCount);
        return 1f + overflow * 0.35f;
    }

    public static int GetRequiredKillsForCharge(int baseKills)
    {
        return Mathf.Max(baseKills, baseKills + Mathf.Min(Level, 14));
    }

    public static int GetKillHealAmount(int baseHeal)
    {
        float multiplier = Mathf.Max(0.25f, 1f - Level * 0.08f);
        return Mathf.Max(1, Mathf.RoundToInt(baseHeal * multiplier));
    }
}
