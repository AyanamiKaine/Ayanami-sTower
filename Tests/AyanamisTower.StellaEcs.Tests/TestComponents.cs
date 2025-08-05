using System;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AyanamisTower.StellaEcs.Examples;

// Test component types for the declarative extension system
public struct Position
{
    public float X, Y, Z;
    public Position(float x, float y, float z) { X = x; Y = y; Z = z; }
}

public struct Velocity
{
    public float X, Y, Z;
    public Velocity(float x, float y, float z) { X = x; Y = y; Z = z; }
}

public struct Health
{
    public int Current, Max;
    public Health(int current, int max) { Current = current; Max = max; }
}

public struct Damage
{
    public int Amount;
    public Damage(int amount) { Amount = amount; }
}

public struct Enemy
{
    public int AttackPower;
    public float AggroRange;
    public Enemy(int attackPower, float aggroRange) { AttackPower = attackPower; AggroRange = aggroRange; }
}

public struct Collectible
{
    public int Value;
    public string Type;
    public Collectible(int value, string type) { Value = value; Type = type; }
}
