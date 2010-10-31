using System;
using BirdhouseManor;

namespace CastleRavenloft
{

// TODO: see if we can script these

#region AidSkill
public sealed class AidSkill : Skill
{
  public AidSkill()
  {
    Name = "Aid";
    DescriptionText = "You know healing techniques. At the end of your hero phase, if you did not attack, one other hero on " +
                      "your tile regains 1 hit point.";
  }
}
#endregion

#region CriticalStrikeSkill
public sealed class CriticalStrikeSkill : Skill
{
  public CriticalStrikeSkill()
  {
    Name = "Critical Strike";
    DescriptionText = "If you roll a natural 20 on any attack roll, you gain a +1 damage bonus on the attack.";
  }
}
#endregion

#region DefenderSkill
public sealed class DefenderSkill : Skill
{
  public DefenderSkill()
  {
    Name = "Defender";
    DescriptionText = "You protect your friends. While another hero is on the same tile as you, he or she gains a +1 bonus to " +
                      "armor class.";
  }
}
#endregion

#region LoreSkill
public sealed class LoreSkill : Skill
{
  public LoreSkill()
  {
    Name = "Lore";
    DescriptionText = "You know the secrets of monsters. While another hero is on the same tile as you, he or she gains a +1 " +
                      "bonus to attack rolls.";
  }
}
#endregion

#region ScoutSkill
public sealed class ScoutSkill : Skill
{
  public ScoutSkill()
  {
    Name = "Scout";
    DescriptionText = "You are a master explorer. During your Exploration Phase, you can explore one unexplored edge on " +
                       "your tile, even if you aren't adjacent to it.";
  }
}
#endregion

#region SqueezingSkill
public sealed class SqueezingSkill : Skill
{
  public SqueezingSkill()
  {
    Name = "Squeezing";
    DescriptionText = "Can be placed over walls, but the center square that the miniature is on must be open.";
  }
}
#endregion

#region TrapExpertSkill
public sealed class TrapExpertSkill : Skill
{
  public TrapExpertSkill()
  {
    Name = "Trap Expert";
    DescriptionText = "You are an expert at finding and disabling Traps. You gain a +5 bonus to rolls to disable traps.";
  }
}
#endregion

} // namespace CastleRavenloft
