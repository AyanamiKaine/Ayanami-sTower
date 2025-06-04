using System;

namespace AyanamisTower.StellaDB.Model;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public interface IComponent
{
    public long EntityId { get; set; }
}

public class Entity
{
    public long? Id { get; set; }
    public long? ParentId { get; set; }
    public long? OwnerId { get; set; }
}


public class Name : IComponent
{
    public long EntityId { get; set; }
    public required string Value { get; set; }
}

public class Position2D : IComponent
{
    public long EntityId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}

public class Position3D : IComponent
{
    public long EntityId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}

public class Age : IComponent
{
    public long EntityId { get; set; }
    public long Value { get; set; }
}

public class Prestige : IComponent
{
    public long EntityId { get; set; }
    public double Value { get; set; }
}

public class Size : IComponent
{
    public long EntityId { get; set; }
}

public class BaseStat : IComponent
{
    public long EntityId { get; set; }
    public long DIPLOMACY { get; set; }
    public long MARTIAL { get; set; }
    public long STEWARDSHIP { get; set; }
    public long INTRIGUE { get; set; }
    public long LEARNING { get; set; }
}

public class Health : IComponent
{
    public long EntityId { get; set; }
    public double Value { get; set; }
}

public class Good
{
    public long? Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double BaseWeight { get; set; }
    public double BaseVolume { get; set; }
    public string IsAbstract { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class GoodCategory
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double? TransportModifier { get; set; }
    public double? StorageModifier { get; set; }
    public string TaxCategory { get; set; } = string.Empty;
}

public class GoodSubstitution
{
    public long? Id { get; set; }
    public long GoodId { get; set; }
    public long SubstituteGoodId { get; set; }
    public double SubstitutionRate { get; set; }
}

public class ProductionRecipe
{
    public long? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long OutputGoodId { get; set; }
    public double OutputQuantity { get; set; }
}

public class RecipeInput
{
    public long? Id { get; set; }
    public long RecipeId { get; set; }
    public long InputGoodId { get; set; }
    public double Quantity { get; set; }
    public string IsOptional { get; set; } = string.Empty;
    public double? EfficiencyImpact { get; set; }
}

public class FeatureDefinition : IComponent
{
    public long EntityId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class EntityFeature : IComponent
{
    public long EntityId { get; set; }
    public long FeatureId { get; set; }
}

public class Population : IComponent
{
    public long EntityId { get; set; }
    public double? Consciousness { get; set; }
    public double? Militancy { get; set; }
    public double? Literacy { get; set; }
}

public class Velocity2D : IComponent
{
    public long EntityId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}

public class Velocity3D : IComponent
{
    public long EntityId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}

public class Trait : IComponent
{
    public long EntityId { get; set; }
    public string IsPositive { get; set; } = string.Empty;
}

public class Ideology : IComponent
{
    public long EntityId { get; set; }
}

public class StarSystem : IComponent
{
    public long EntityId { get; set; }
}

public class Galaxy : IComponent
{
    public long EntityId { get; set; }
}

public class Planet : IComponent
{
    public long EntityId { get; set; }
}

public class Character : IComponent
{
    public long EntityId { get; set; }
}

public class Specie : IComponent
{
    public long EntityId { get; set; }
    public double BaseFertility { get; set; }
}

public class Religion : IComponent
{
    public long EntityId { get; set; }
}

public class Culture : IComponent
{
    public long EntityId { get; set; }
}

public class Province : IComponent
{
    public long EntityId { get; set; }
}

public class Star : IComponent
{
    public long EntityId { get; set; }
}

public class Asteroid : IComponent
{
    public long EntityId { get; set; }
}

public class Polity : IComponent
{
    public long EntityId { get; set; }
    public long? SeatOfPowerLocationID { get; set; }
    public string Abbreviation { get; set; } = string.Empty;
    public string LeaderTitle { get; set; } = string.Empty;
}

public class CharacterTrait
{
    public long CharacterId { get; set; }
    public long TraitId { get; set; }
}

public class ConnectedTo
{
    public long SystemId1 { get; set; }
    public long SystemId2 { get; set; }
    public double? Distance { get; set; }
}

