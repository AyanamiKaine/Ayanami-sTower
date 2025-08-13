using AyanamisTower.StellaEcs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
namespace AyanamisTower.StellaEcs.Api
{
    // Extension methods that adapt the neutral World API to REST needs.
    /// <summary>
    /// REST-facing extension methods for the ECS World. Keeps web/API concerns outside the ECS core.
    /// </summary>
    public static class WorldRestExtensions
    {
        /// <summary>
        /// Starts the REST API server for the given world.
        /// </summary>
        /// <param name="w">The ECS world instance.</param>
        /// <param name="url">The URL the API server should listen on.</param>
        public static void EnableRestApi(this World w, string url = "http://localhost:5123")
        {
            RestApiServer.Start(w, url);
        }

        /// <summary>
        /// Stops the REST API server if it is running.
        /// </summary>
        /// <param name="w">The ECS world instance.</param>
        public static Task DisableRestApi(this World w)
        {
            return RestApiServer.Stop();
        }

        /// <summary>
        /// Returns a snapshot of world status values for diagnostics.
        /// </summary>
        /// <param name="w">The ECS world instance.</param>
        /// <returns>An object describing world status counters.</returns>
        public static WorldStatusDto GetWorldStatus(this World w)
        {
            return new WorldStatusDto
            {
                MaxEntities = w.MaxEntities,
                RecycledEntityIds = w.RecycledEntityIdCount,
                RegisteredSystems = w.RegisteredSystemCount,
                ComponentTypes = w.RegisteredComponentTypeCount,
                Tick = w.Tick,
                DeltaTime = w.LastDeltaTime,
                IsPaused = w.IsPaused
            };
        }

        /// <summary>
        /// Returns a list of registered systems with minimal metadata for display.
        /// </summary>
        /// <param name="w">The ECS world instance.</param>
        /// <returns>System info items.</returns>
        public static IEnumerable<SystemInfoDto> GetSystems(this World w)
        {
            // Return with group and order; use current sorted lists
            return w.GetSystemsWithOrder().Select(tuple => new SystemInfoDto
            {
                Name = tuple.system.Name,
                Enabled = tuple.system.Enabled,
                PluginOwner = (w.GetSystemOwner(tuple.system.GetType())?.Prefix) ?? "World",
                Group = tuple.group.Name,
                Order = tuple.orderIndex
            });
        }

        /// <summary>
        /// Returns all components attached to an entity in a DTO form suitable for JSON.
        /// </summary>
        /// <param name="w">The ECS world instance.</param>
        /// <param name="entity">The entity to inspect.</param>
        /// <returns>List of component info items.</returns>
        public static List<ComponentInfoDto> GetAllComponentsForEntity(this World w, Entity entity)
        {
            var list = new List<ComponentInfoDto>();
            foreach (var (type, data) in w.GetComponentsForEntityAsObjects(entity))
            {
                // Try to serialize component data to a JSON element. If it contains unsupported types
                // (e.g., IntPtr from GPU handles), fall back to null so the frontend can still list/remove it.
                object? safeData = TrySerializeToElement(data, type);
                list.Add(new ComponentInfoDto
                {
                    TypeName = type.Name,
                    Data = safeData,
                    PluginOwner = (w.GetComponentOwner(type)?.Prefix) ?? "World",
                    IsDynamic = false
                });
            }
            // Append dynamic components
            foreach (var (name, data) in w.GetDynamicComponentsForEntity(entity))
            {
                object? safeData = data is null ? null : TrySerializeToElement(data, data.GetType());
                list.Add(new ComponentInfoDto
                {
                    TypeName = name,
                    Data = safeData,
                    PluginOwner = "World",
                    IsDynamic = true
                });
            }
            return list;
        }

        // Cache of type serializability checks to avoid repeated reflection and exceptions.
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, bool> s_canSerializeCache = new();

        private static object? TrySerializeToElement(object? value, Type valueType)
        {
            if (value is null) return null;
            // Fast-path: if the type graph likely contains unsupported members (e.g., IntPtr), skip serialization entirely.
            if (!IsProbablySerializable(valueType))
            {
                return null;
            }
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    IncludeFields = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                return JsonSerializer.SerializeToElement(value, valueType, options);
            }
            catch (NotSupportedException)
            {
                // Contains members like IntPtr; skip payload
                return null;
            }
            catch
            {
                // Any other unexpected serialization issue: be safe and omit data
                return null;
            }
        }

        private static bool IsProbablySerializable(Type t)
        {
            if (s_canSerializeCache.TryGetValue(t, out var cached)) return cached;
            var result = ComputeSerializable(t, 0, new HashSet<Type>());
            s_canSerializeCache[t] = result;
            return result;
        }

        private static bool ComputeSerializable(Type t, int depth, HashSet<Type> visiting)
        {
            // Depth cap to avoid pathological graphs; assume okay beyond this
            if (depth > 3) return true;

            if (t.IsPointer || t == typeof(IntPtr) || t == typeof(UIntPtr)) return false;
            if (t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal)) return true;
            if (t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(Guid)) return true;
            if (t == typeof(System.Numerics.Vector2) || t == typeof(System.Numerics.Vector3) || t == typeof(System.Numerics.Vector4) || t == typeof(System.Numerics.Quaternion)) return true;

            // Arrays/collections: check element types
            if (t.IsArray)
            {
                var elem = t.GetElementType();
                return elem is not null && ComputeSerializable(elem, depth + 1, visiting);
            }
            if (t.IsGenericType)
            {
                var args = t.GetGenericArguments();
                foreach (var a in args)
                {
                    if (!ComputeSerializable(a, depth + 1, visiting)) return false;
                }
            }

            // Avoid cycles
            if (!visiting.Add(t)) return true;

            // If the type is from a known native/graphics namespace, assume not serializable (heuristic)
            var ns = t.Namespace ?? string.Empty;
            if (ns.StartsWith("MoonWorks", StringComparison.Ordinal) || ns.StartsWith("SDL", StringComparison.Ordinal))
            {
                visiting.Remove(t);
                return false;
            }

            // Inspect instance fields (public + non-public) and public properties
            const BindingFlags FB = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var f in t.GetFields(FB))
            {
                var ft = f.FieldType;
                if (ft.IsPointer || ft == typeof(IntPtr) || ft == typeof(UIntPtr)) { visiting.Remove(t); return false; }
                if (!ComputeSerializable(ft, depth + 1, visiting)) { visiting.Remove(t); return false; }
            }
            foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!p.CanRead) continue;
                var pt = p.PropertyType;
                if (pt.IsPointer || pt == typeof(IntPtr) || pt == typeof(UIntPtr)) { visiting.Remove(t); return false; }
                if (!ComputeSerializable(pt, depth + 1, visiting)) { visiting.Remove(t); return false; }
            }

            visiting.Remove(t);
            return true;
        }

        private static JsonElement NormalizeComponentJson(Type componentType, JsonElement input)
        {
            if (input.ValueKind != JsonValueKind.Object)
                return input;

            // If it already has a 'value' property (any case), keep as-is
            if (HasPropertyCaseInsensitive(input, "value"))
                return input;

            // If the component has a field/property named 'Value' of type Vector3 and the payload
            // looks like { x: number, y: number, z: number }, wrap it into { value: { x,y,z } }
            var valueMember = componentType.GetMember("Value", BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
            Type? valueType = valueMember switch
            {
                PropertyInfo pi => pi.PropertyType,
                FieldInfo fi => fi.FieldType,
                _ => null
            };
            if (valueType == typeof(System.Numerics.Vector3) && TryGetXYZ(input, out var x, out var y, out var z))
            {
                var wrapped = new { value = new { x, y, z } };
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                return JsonSerializer.SerializeToElement(wrapped, options);
            }

            // No changes
            return input;
        }

        private static bool HasPropertyCaseInsensitive(JsonElement obj, string name)
        {
            foreach (var prop in obj.EnumerateObject())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool TryGetXYZ(JsonElement obj, out float x, out float y, out float z)
        {
            x = y = z = 0f;
            bool hasX = false, hasY = false, hasZ = false;
            foreach (var prop in obj.EnumerateObject())
            {
                if (prop.Value.ValueKind != JsonValueKind.Number) continue;
                if (prop.NameEquals("x") || prop.NameEquals("X")) { hasX = prop.Value.TryGetSingle(out x); }
                else if (prop.NameEquals("y") || prop.NameEquals("Y")) { hasY = prop.Value.TryGetSingle(out y); }
                else if (prop.NameEquals("z") || prop.NameEquals("Z")) { hasZ = prop.Value.TryGetSingle(out z); }
            }
            return hasX && hasY && hasZ;
        }

        /// <summary>
        /// Returns a list of registered component types and their owners.
        /// </summary>
        /// <param name="w">The ECS world instance.</param>
        /// <returns>Component info items.</returns>
        public static IEnumerable<ComponentInfoDto> GetComponentTypes(this World w)
        {
            return w.GetRegisteredComponentTypes().Select(t => new ComponentInfoDto
            {
                TypeName = t.Name,
                PluginOwner = (w.GetComponentOwner(t)?.Prefix) ?? "World",
                Data = null
            });
        }

        /// <summary>
        /// Returns a list of registered services and their public methods.
        /// </summary>
        /// <param name="w">The ECS world instance.</param>
        /// <returns>Service info items.</returns>
        public static IEnumerable<ServiceInfoDto> GetServices(this World w)
        {
            return w.GetRegisteredServices().Select(kvp => new ServiceInfoDto
            {
                TypeName = kvp.type.FullName ?? kvp.type.Name,
                Methods = kvp.type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                   .Where(m => !m.IsSpecialName)
                                   .Select(m => m.Name),
                PluginOwner = (w.GetServiceOwner(kvp.type)?.Prefix) ?? "World"
            });
        }

        /// <summary>
        /// Returns a list of loaded plugins with basic metadata and URL.
        /// </summary>
        /// <param name="w">The ECS world instance.</param>
        /// <returns>Plugin info items.</returns>
        public static IEnumerable<PluginInfoDto> GetPlugins(this World w)
        {
            return w.GetRegisteredPlugins().Select(plugin => new PluginInfoDto
            {
                Name = plugin.Name,
                Version = plugin.Version,
                Author = plugin.Author,
                Description = plugin.Description,
                Prefix = plugin.Prefix,
                Url = $"/api/plugins/{plugin.Prefix}"
            });
        }

        /// <summary>
        /// Returns details for a specific plugin, or null if not found.
        /// </summary>
        /// <param name="w">The ECS world instance.</param>
        /// <param name="pluginPrefix">The plugin prefix identifier.</param>
        /// <returns>Detailed plugin info or null.</returns>
        public static PluginDetailDto? GetPluginDetails(this World w, string pluginPrefix)
        {
            if (!w.TryGetPluginByPrefix(pluginPrefix, out var plugin))
            {
                return null;
            }

            return new PluginDetailDto
            {
                Name = plugin.Name,
                Version = plugin.Version,
                Author = plugin.Author,
                Description = plugin.Description,
                Prefix = plugin.Prefix,
                Url = $"/api/plugins/{plugin.Prefix}",
                Systems = plugin.ProvidedSystems.Select(t => $"{plugin.Prefix}.{t.Name}"),
                Services = plugin.ProvidedServices.Select(t => t.FullName ?? t.Name),
                Components = plugin.ProvidedComponents.Select(t => t.Name)
            };
        }

        /// <summary>
        /// Sets or replaces a component on an entity using a JSON payload and a type name.
        /// </summary>
        /// <param name="w">The ECS world instance.</param>
        /// <param name="entity">Target entity.</param>
        /// <param name="componentTypeName">Component type name or full name.</param>
        /// <param name="componentData">JSON data representing the component.</param>
        /// <returns>True on success.</returns>
        public static bool SetComponentFromJson(this World w, Entity entity, string componentTypeName, JsonElement componentData)
        {
            if (!w.IsEntityValid(entity))
            {
                throw new ArgumentException($"Entity {entity} is no longer valid");
            }

            var componentType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .FirstOrDefault(type => type.Name == componentTypeName || type.FullName == componentTypeName)
                ?? throw new ArgumentException($"Component type '{componentTypeName}' not found.");

            if (!componentType.IsValueType)
            {
                throw new ArgumentException($"Component type '{componentTypeName}' must be a struct (value type).");
            }

            try
            {
                var normalized = NormalizeComponentJson(componentType, componentData);
                var component = JsonSerializer.Deserialize(normalized, componentType, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    IncludeFields = true
                }) ?? throw new JsonException($"Failed to deserialize component data for type '{componentTypeName}'. JSON: {componentData.GetRawText()}");

                var method = (typeof(World).GetMethod(nameof(World.SetComponent))?.MakeGenericMethod(componentType))
                             ?? throw new InvalidOperationException($"Could not create generic SetComponent method for type '{componentTypeName}'.");
                method.Invoke(w, new object?[] { entity, component });
                Console.WriteLine($"[World] Successfully set component '{componentTypeName}' on entity {entity}");
                return true;
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"JSON deserialization failed for component '{componentTypeName}': {ex.Message}. JSON: {componentData.GetRawText()}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set component '{componentTypeName}' on entity {entity}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Removes a component from an entity by component type name.
        /// </summary>
        /// <param name="w">The ECS world instance.</param>
        /// <param name="entity">Target entity.</param>
        /// <param name="componentTypeName">Component type name or full name.</param>
        /// <returns>True if removed.</returns>
        public static bool RemoveComponentByName(this World w, Entity entity, string componentTypeName)
        {
            if (!w.IsEntityValid(entity))
            {
                throw new ArgumentException($"Entity {entity} is no longer valid");
            }

            var componentType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .FirstOrDefault(type => type.Name == componentTypeName || type.FullName == componentTypeName)
                ?? throw new ArgumentException($"Component type '{componentTypeName}' not found.");

            if (!componentType.IsValueType)
            {
                throw new ArgumentException($"Component type '{componentTypeName}' must be a struct (value type).");
            }

            try
            {
                var method = (typeof(World).GetMethod(nameof(World.RemoveComponent))?.MakeGenericMethod(componentType))
                             ?? throw new InvalidOperationException($"Could not create generic RemoveComponent method for type '{componentTypeName}'.");
                method.Invoke(w, new object?[] { entity });
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[World] Failed to remove component '{componentTypeName}' from entity {entity}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Invokes a method on a registered service using a dynamic parameter map coming from JSON.
        /// </summary>
        /// <param name="w">The ECS world instance.</param>
        /// <param name="serviceTypeName">Full name of the service type.</param>
        /// <param name="methodName">Name of the method to invoke.</param>
        /// <param name="parameters">Case-insensitive map of parameter names to values (JsonElement or primitives).</param>
        /// <returns>The return value or null for void methods.</returns>
        public static object? InvokeServiceMethod(this World w, string serviceTypeName, string methodName, Dictionary<string, object> parameters)
        {
            var serviceEntry = w.GetRegisteredServices().FirstOrDefault(s => s.type.FullName == serviceTypeName);
            if (serviceEntry.type == null)
            {
                throw new KeyNotFoundException($"Service of type '{serviceTypeName}' has not been registered.");
            }

            var serviceType = serviceEntry.type;
            var serviceInstance = serviceEntry.instance;

            var method = serviceType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)
                         ?? throw new MissingMethodException(serviceTypeName, methodName);

            var methodParams = method.GetParameters();
            var invokeArgs = new object?[methodParams.Length];
            var caseInsensitiveParams = new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < methodParams.Length; i++)
            {
                var paramInfo = methodParams[i];
                if (paramInfo.Name != null &&
                    caseInsensitiveParams.TryGetValue(paramInfo.Name, out var paramValue) &&
                    paramValue is JsonElement jsonElement)
                {
                    invokeArgs[i] = jsonElement.Deserialize(paramInfo.ParameterType, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                else if (paramInfo.HasDefaultValue)
                {
                    invokeArgs[i] = paramInfo.DefaultValue;
                }
                else
                {
                    throw new ArgumentException($"Missing required parameter: '{paramInfo.Name}' for method '{methodName}'.");
                }
            }

            return method.Invoke(serviceInstance, invokeArgs);
        }
    }

    /// <summary>
    /// A DTO for exposing basic entity information, typically in a list.
    /// </summary>
    public class EntitySummaryDto
    {
        /// <summary>
        /// The unique identifier for the entity.
        /// </summary>
        public uint Id { get; set; }
        /// <summary>
        /// The generation of the entity.
        /// </summary>
        public int Generation { get; set; }
        /// <summary>
        /// A direct link to the detailed view of this entity.
        /// </summary>
        public required string Url { get; set; }
    }

    /// <summary>
    /// A DTO for exposing the full details of a single entity.
    /// </summary>
    public class EntityDetailDto
    {
        /// <summary>
        /// The unique identifier for the entity.
        /// </summary>
        public uint Id { get; set; }
        /// <summary>
        /// The list of components attached to the entity.
        /// </summary>
        public required List<ComponentInfoDto> Components { get; set; }
    }

    /// <summary>
    /// A DTO for exposing the world's status.
    /// </summary>
    public class WorldStatusDto
    {
        /// <summary>
        /// The maximum number of entities that can be created in this world.
        /// </summary>
        public uint MaxEntities { get; set; }
        /// <summary>
        /// The number of entity IDs that have been recycled and can be reused.
        /// </summary>
        public int RecycledEntityIds { get; set; }
        /// <summary>
        /// The number of systems currently registered in the world.
        /// </summary>
        public int RegisteredSystems { get; set; }
        /// <summary>
        /// The number of component types currently registered in the world.
        /// </summary>
        public int ComponentTypes { get; set; }
        /// <summary>
        /// The current world tick.
        /// </summary>
        public uint Tick { get; set; }
        /// <summary>
        /// The most recent update's delta time in seconds.
        /// </summary>
        public float DeltaTime { get; set; }
        /// <summary>
        /// Whether the world is paused.
        /// </summary>
        public bool IsPaused { get; set; }
    }

    /// <summary>
    /// A DTO for exposing system information.
    /// </summary>
    public class SystemInfoDto
    {
        /// <summary>
        /// The name of the system.
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// Indicates whether the system is currently enabled.
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// The owner of the plugin that provides this system.
        /// </summary>
        public required string PluginOwner { get; set; }
        /// <summary>
        /// The group this system runs in: InitializationSystemGroup, SimulationSystemGroup, or PresentationSystemGroup.
        /// </summary>
        public string? Group { get; set; }
        /// <summary>
        /// The zero-based order index within its group according to the current sort.
        /// </summary>
        public int Order { get; set; }
    }

    /// <summary>
    /// A DTO for exposing component information.
    /// </summary>
    public class ComponentInfoDto
    {
        /// <summary>
        /// The name of the component type.
        /// </summary>
        public required string TypeName { get; set; }
        /// <summary>
        /// The data associated with the component, if applicable.
        /// </summary>
        public object? Data { get; set; }
        /// <summary>
        /// The owner of the plugin that provides this component.
        /// </summary>
        public string? PluginOwner { get; set; }

        /// <summary>
        /// Indicates whether this component entry represents a dynamic component (identified by name rather than type).
        /// </summary>
        public bool IsDynamic { get; set; }
    }

    /// <summary>
    /// A DTO for exposing service information.
    /// </summary>
    public class ServiceInfoDto
    {
        /// <summary>
        /// The full type name of the service, used for invoking methods.
        /// </summary>
        public required string TypeName { get; set; }
        /// <summary>
        /// A list of public methods available on the service.
        /// </summary>
        public required IEnumerable<string> Methods { get; set; }
        /// <summary>
        /// The owner of the plugin that provides this service.
        /// </summary>
        public required string PluginOwner { get; set; }
    }

    /// <summary>
    /// A DTO for exposing detailed plugin information.
    /// </summary>
    public class PluginDetailDto : PluginInfoDto
    {
        /// <summary>
        /// A list of systems provided by the plugin.
        /// </summary>
        public required IEnumerable<string> Systems { get; set; }
        /// <summary>
        /// A list of services provided by the plugin.
        /// </summary>
        public required IEnumerable<string> Services { get; set; }
        /// <summary>
        /// A list of components provided by the plugin.
        /// </summary>
        public required IEnumerable<string> Components { get; set; }
    }

    /// <summary>
    /// A DTO for exposing plugin information.
    /// </summary>
    public class PluginInfoDto
    {
        /// <summary>
        /// The name of the plugin.
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// The version of the plugin.
        /// </summary>
        public required string Version { get; set; }
        /// <summary>
        /// The author of the plugin.
        /// </summary>
        public required string Author { get; set; }
        /// <summary>
        /// A description of what the plugin does.
        /// </summary>
        public required string Description { get; set; }
        /// <summary>
        /// The unique prefix used for this plugin's systems and services.
        /// </summary>
        public required string Prefix { get; set; }
        /// <summary>
        /// The URL for accessing this plugin's API endpoints.
        /// </summary>
        public required string Url { get; set; }
    }
}
