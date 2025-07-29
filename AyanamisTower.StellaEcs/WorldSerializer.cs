#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using AyanamisTower.StellaEcs.Serialization;

namespace AyanamisTower.StellaEcs
{
    /// <summary>
    /// Provides methods to serialize a World to a JSON string and deserialize it back.
    /// </summary>
    public class WorldSerializer
    {
        private readonly ISerializationHelper _helper;

        public WorldSerializer(ISerializationHelper? helper = null)
        {
            _helper = helper ?? new DefaultSerializationHelper();
        }

        public string Serialize(World world)
        {
            var snapshot = new WorldSnapshot();

            // --- Reflect into world to get private fields ---
            var maxEntitiesField = typeof(World).GetField("_maxEntities", BindingFlags.NonPublic | BindingFlags.Instance);
            var nextEntityIdField = typeof(World).GetField("_nextEntityId", BindingFlags.NonPublic | BindingFlags.Instance);
            var entityGenerationsField = typeof(World).GetField("_entityGenerations", BindingFlags.NonPublic | BindingFlags.Instance);
            var recycledEntityIdsField = typeof(World).GetField("_recycledEntityIds", BindingFlags.NonPublic | BindingFlags.Instance);
            var componentStoragesField = typeof(World).GetField("_componentStorages", BindingFlags.NonPublic | BindingFlags.Instance);
            var relationshipStoragesField = typeof(World).GetField("_relationshipStorages", BindingFlags.NonPublic | BindingFlags.Instance);

            // --- Capture World State ---
            snapshot.MaxEntities = (int)maxEntitiesField!.GetValue(world)!;
            snapshot.NextEntityId = (int)nextEntityIdField!.GetValue(world)!;
            snapshot.EntityGenerations = (int[])entityGenerationsField!.GetValue(world)!;
            snapshot.RecycledEntityIds = new Queue<int>(((Queue<int>)recycledEntityIdsField!.GetValue(world)!));

            var componentStorages = (Dictionary<Type, IComponentStorage>)componentStoragesField!.GetValue(world)!;
            var relationshipStorages = (Dictionary<Type, IRelationshipStorage>)relationshipStoragesField!.GetValue(world)!;

            // --- Capture Component Data ---
            foreach (var kvp in componentStorages)
            {
                var type = kvp.Key;
                var storage = kvp.Value;
                var storageSnapshot = new ComponentStorageSnapshot { ComponentTypeName = type.AssemblyQualifiedName! };

                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                snapshot.ComponentTypes.Add(new ComponentTypeInfo
                {
                    TypeName = type.AssemblyQualifiedName!,
                    IsRuntimeDefined = type.Assembly.IsDynamic,
                    Fields = type.Assembly.IsDynamic
                        ? fields.ToDictionary(f => f.Name, f => f.FieldType.AssemblyQualifiedName!)
                        : null
                });

                var packedEntities = storage.PackedEntities;
                for (int i = 0; i < packedEntities.Length; i++)
                {
                    var entityId = packedEntities[i];
                    var componentData = storage.GetAsObject(entityId);
                    storageSnapshot.Components[entityId] = _helper.Serialize(componentData);
                }
                snapshot.ComponentData.Add(storageSnapshot);
            }

            // --- Capture Relationship Data ---
            foreach (var kvp in relationshipStorages)
            {
                var type = kvp.Key;
                var storage = kvp.Value;
                var storageSnapshot = new RelationshipStorageSnapshot { RelationshipTypeName = type.AssemblyQualifiedName! };
                snapshot.RelationshipTypes.Add(type.AssemblyQualifiedName!);

                var forwardMapField = storage.GetType().GetField("_forwardMap", BindingFlags.NonPublic | BindingFlags.Instance);
                var forwardMap = (dynamic)forwardMapField!.GetValue(storage)!;

                foreach (var sourceEntry in forwardMap)
                {
                    int sourceId = sourceEntry.Key;
                    foreach (var targetEntry in sourceEntry.Value)
                    {
                        Entity targetEntity = targetEntry.Key;
                        object relData = targetEntry.Value;
                        storageSnapshot.Relationships.Add(new RelationshipEntry
                        {
                            SourceId = sourceId,
                            TargetId = targetEntity.Id,
                            Data = _helper.Serialize(relData)
                        });
                    }
                }
                snapshot.RelationshipData.Add(storageSnapshot);
            }

            return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true });
        }

        public World Deserialize(string json)
        {
            var options = new JsonSerializerOptions { IncludeFields = true };
            var snapshot = JsonSerializer.Deserialize<WorldSnapshot>(json, options)!;
            var world = new World(snapshot.MaxEntities);

            // --- Restore World State ---
            var nextEntityIdField = typeof(World).GetField("_nextEntityId", BindingFlags.NonPublic | BindingFlags.Instance);
            var entityGenerationsField = typeof(World).GetField("_entityGenerations", BindingFlags.NonPublic | BindingFlags.Instance);
            var recycledEntityIdsField = typeof(World).GetField("_recycledEntityIds", BindingFlags.NonPublic | BindingFlags.Instance);

            nextEntityIdField!.SetValue(world, snapshot.NextEntityId);
            Array.Copy(snapshot.EntityGenerations, (Array)entityGenerationsField!.GetValue(world)!, snapshot.EntityGenerations.Length);
            recycledEntityIdsField!.SetValue(world, new Queue<int>(snapshot.RecycledEntityIds));

            // --- Register Types ---
            var typeCache = new Dictionary<string, Type>();
            foreach (var typeInfo in snapshot.ComponentTypes)
            {
                Type componentType;
                if (typeInfo.IsRuntimeDefined)
                {
                    var fields = typeInfo.Fields!.ToDictionary(f => f.Key, f => Type.GetType(f.Value)!);
                    var simpleTypeName = typeInfo.TypeName.Split(',')[0].Trim();
                    componentType = ComponentFactory.CreateComponentType(simpleTypeName, fields);
                }
                else
                {
                    componentType = Type.GetType(typeInfo.TypeName)!;
                }
                world.RegisterComponent(componentType);
                typeCache[typeInfo.TypeName] = componentType;
            }

            foreach (var typeName in snapshot.RelationshipTypes)
            {
                var relType = Type.GetType(typeName)!;
                var method = typeof(World).GetMethod(nameof(World.RegisterRelationship))!.MakeGenericMethod(relType);
                method.Invoke(world, null);
                typeCache[typeName] = relType;
            }

            // --- Restore Components ---
            foreach (var storageSnapshot in snapshot.ComponentData)
            {
                var componentType = typeCache[storageSnapshot.ComponentTypeName];
                foreach (var compEntry in storageSnapshot.Components)
                {
                    var entityId = compEntry.Key;
                    var componentData = _helper.Deserialize(compEntry.Value, componentType);
                    var entity = world.GetEntityFromId(entityId);
                    world.SetComponent(entity, componentType, componentData);
                }
            }

            // --- Restore Relationships ---
            foreach (var storageSnapshot in snapshot.RelationshipData)
            {
                var relType = typeCache[storageSnapshot.RelationshipTypeName];

                // FIX: Correctly find the generic AddRelationship<T>(Entity, Entity, T) method.
                var genericAddMethod = typeof(World).GetMethods()
                    .Single(m =>
                        m.Name == nameof(World.AddRelationship) &&
                        m.IsGenericMethodDefinition &&
                        m.GetParameters().Length == 3);

                // Create a closed generic method, e.g., AddRelationship<IsChildOf>(...)
                var addMethod = genericAddMethod.MakeGenericMethod(relType);

                foreach (var relEntry in storageSnapshot.Relationships)
                {
                    var source = world.GetEntityFromId(relEntry.SourceId);
                    var target = world.GetEntityFromId(relEntry.TargetId);
                    var relData = _helper.Deserialize(relEntry.Data, relType);

                    // This should now invoke the correct, non-null method.
                    addMethod.Invoke(world, [source, target, relData]);
                }
            }

            return world;
        }
    }
}
