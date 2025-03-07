using System.Numerics;

namespace InformationHidingAndECS;

/*
Behavior is decomposed in interfaces
*/

public interface IJump
{
    public void Jump();
}

public interface IAttack
{
    public void Attack(LivingEntity target);
}

/*
Shared fields between different game entities is decomposed into small hierarchies, this is similar to 
how its done in minecraft.


For example here is a abstracted view how the inheritance hierarchy looks like in minecraft:
Entity (base class)
  ├── LivingEntity
  │     ├── Player
  │     └── Mob
  │           ├── PathfinderMob
  │           │     ├── Animal
  │           │     │     ├── Cow
  │           │     │     ├── Sheep
  │           │     │     ├── Chicken
  │           │     │     └── ...
  │           │     ├── Monster
  │           │     │     ├── Zombie
  │           │     │     ├── Skeleton
  │           │     │     ├── Creeper
  │           │     │     └── ...
  │           │     └── Villager
  │           └── Flying Mobs (like Phantom)
  └── ItemEntity (dropped items)
*/

public class BaseEntity()
{
    public Vector3 Position3D { get; set; } = Vector3.Zero;
    public virtual void Tick()
    {
        //Render
    }

}

public class LivingEntity(double health = 10) : BaseEntity()
{
    public double Health { get; set; } = health;
    public bool IsDead => Health <= 0;
}

/// <summary>
/// Here we create a simple player class, you could see in a video game
/// </summary>
public class Player(double health = 10, double attackPower = 1.0, float jumpStrength = 1.0f) : LivingEntity(health), IJump, IAttack
{
    public string PlayerName { get; init; } = string.Empty;
    public double AttackPower { get; set; } = attackPower;
    public float JumpStrength { get; set; } = jumpStrength;

    public override void Tick()
    {
        base.Tick();
        // var input = GetPlayerInput();
        // HandlePlayerInput(input);
    }

    public void Attack(LivingEntity target)
    {
        target.Health -= AttackPower;
    }

    public void Jump()
    {
        Position3D *= new Vector3(JumpStrength);
    }
}

