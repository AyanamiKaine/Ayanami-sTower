using System;

namespace AyanamisTower.StellaEcs.Attributes;

/// <summary>
/// Marks an extension method as requiring specific components from an entity.
/// The method signature should have Entity as the first parameter, followed by
/// parameters that match the required component types.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class EntityExtensionAttribute : Attribute
{
    /// <summary>
    /// The component types required by this extension method.
    /// If not specified, they will be inferred from the method parameters.
    /// </summary>
    public Type[]? RequiredComponents { get; set; }

    /// <summary>
    /// Whether to generate runtime checks for component existence.
    /// Default is true for safety.
    /// </summary>
    public bool GenerateRuntimeChecks { get; set; } = true;

    /// <summary>
    /// Whether to allow the method to be called even if some components are missing.
    /// If true, missing components will be passed as default values.
    /// </summary>
    public bool AllowPartial { get; set; } = false;
    /// <summary>
    /// Marks an extension method as requiring specific components from an entity.
    /// The method signature should have Entity as the first parameter, followed by
    /// parameters that match the required component types.
    /// </summary>
    public EntityExtensionAttribute() { }
    /// <summary>
    /// Marks an extension method as requiring specific components from an entity.
    /// The method signature should have Entity as the first parameter, followed by
    /// parameters that match the required component types.
    /// </summary>
    public EntityExtensionAttribute(params Type[] requiredComponents)
    {
        RequiredComponents = requiredComponents;
    }
}

/// <summary>
/// Marks a parameter in an entity extension method as being a component reference.
/// This helps the source generator understand which parameters should be populated
/// from entity components.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class ComponentAttribute : Attribute
{
    /// <summary>
    /// Whether this component parameter should be passed by mutable reference.
    /// If false, it will be passed as readonly reference.
    /// </summary>
    public bool Mutable { get; set; } = false;

    /// <summary>
    /// Whether this component is optional. If true and the component doesn't exist,
    /// the default value will be passed.
    /// </summary>
    public bool Optional { get; set; } = false;
}

/// <summary>
/// Declaratively specifies that an entity extension method requires specific components.
/// This generates automatic component existence checks and provides cleaner syntax.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class WithAttribute : Attribute
{
    /// <summary>
    /// The component type that must be present on the entity.
    /// </summary>
    public Type ComponentType { get; }

    /// <summary>
    /// Whether the component should be accessed mutably.
    /// If true, the method can modify the component.
    /// </summary>
    public bool Mutable { get; set; } = false;

    /// <summary>
    /// Whether this component is optional. If true and the component doesn't exist,
    /// the method execution will be skipped gracefully.
    /// </summary>
    public bool Optional { get; set; } = false;

    /// <summary>
    /// Initializes a new instance of the WithAttribute for the specified component type.
    /// </summary>
    /// <param name="componentType">The component type that must be present on the entity.</param>
    public WithAttribute(Type componentType)
    {
        ComponentType = componentType ?? throw new ArgumentNullException(nameof(componentType));
    }
}

/// <summary>
/// Generic version of WithAttribute for better type safety and intellisense.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class With<T> : WithAttribute where T : struct
{
    /// <summary>
    /// Initializes a new instance requiring the component T with readonly access.
    /// </summary>
    public With() : base(typeof(T)) { }

    /// <summary>
    /// Initializes a new instance requiring the component T with specified mutability.
    /// </summary>
    /// <param name="mutable">Whether the component should be accessed mutably.</param>
    public With(bool mutable) : base(typeof(T))
    {
        Mutable = mutable;
    }
}

/// <summary>
/// Marks a static class as containing entity extension methods that should be
/// processed by the source generator.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class EntityExtensionsAttribute : Attribute
{
    /// <summary>
    /// The namespace where generated extension methods should be placed.
    /// If not specified, uses the same namespace as the source class.
    /// </summary>
    public string? TargetNamespace { get; set; }

    /// <summary>
    /// Initializes a new instance of the EntityExtensionsAttribute.
    /// </summary>
    public EntityExtensionsAttribute() { }
}
