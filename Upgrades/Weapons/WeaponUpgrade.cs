using OVERKILL.HakitaPls;

namespace OVERKILL.Upgrades;

public abstract class WeaponUpgrade : LeveledUpgrade
{
    public abstract bool AffectsWeapon(WeaponTypeComponent wtype);

}
