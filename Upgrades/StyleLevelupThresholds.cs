using OVERKILL.UI.Options;

namespace OVERKILL.Upgrades;

public class StyleLevelupThresholds
{
    private static readonly long[] thresholds = new long[] {0, 200, 500, 900, 1400, 2000, 2700, 3500, 4500, 5700, 7000, 8500, 10500, 13000, 16000};

    public static int GetLevelAtXP()
    {
        return GetLevelAtXP(PlayerUpgradeStats.Instance.stylePoints);
    }
    
    //gets level at this xp value
    public static int GetLevelAtXP(long p)
    {
        var max = thresholds[thresholds.Length - 1] * Options.config.XpRequiredMultiplier;
        var maxStep = max - thresholds[thresholds.Length - 2] * Options.config.XpRequiredMultiplier;

        if (p < max)
        {
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (p < thresholds[i] * Options.config.XpRequiredMultiplier)
                    return i;
            }
        }
        else
            return (int)(thresholds.Length + (p - max) / maxStep);

        return 1;
    }

    public static long GetXPAtLevel(int level)
    {
        if (level <= 0)
            return 0;
        
        if (level < thresholds.Length)
            return (long)(thresholds[level] * Options.config.XpRequiredMultiplier);
        
        var max = thresholds[thresholds.Length - 1] * Options.config.XpRequiredMultiplier;
        var maxStep = max - thresholds[thresholds.Length - 2] * Options.config.XpRequiredMultiplier;
        
        return (long)(thresholds[thresholds.Length - 1] + (level - thresholds.Length + 1) * maxStep);
    }
}
