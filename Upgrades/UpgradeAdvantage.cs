using OVERKILL.UI.Upgrades;

namespace OVERKILL.Upgrades;

public class UpgradeAdvantage : IUpgrade
{
    public int OptionsSortPriority => 0;
    public bool IsObtainable => !PlayerUpgradeStats.Instance.upgrades.ContainsKey(this.GetHashCode());

    public double AppearChanceWeighting => RarityChances.Overkill * 1.25d * AppearChanceWeightingOptionMultiplier;

    public double AppearChanceWeightingOptionMultiplier {get; set;} = 1d;
    public string Name => "Upgrade Advantage";

    public string Description =>
        "When a random Upgrade is being chosen, a second one is generated. The one with the higher Rarity becomes the final choice.";

    public Rarity Rarity
    {
        get => Rarity.Overkill;
        set
        {
        }
    }

    public Rarity MaxRarity => Rarity.Overkill;

    public void Apply()
    {
        UpgradeScreen.advantage++;
    }

    public void Absolve()
    {
        UpgradeScreen.advantage--;
    }
    
    public bool Equals(IUpgrade other)
    {
        return Name.Equals(other.Name);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}
