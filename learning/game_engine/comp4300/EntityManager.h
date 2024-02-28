#pragma once
#include <map>
#include <memory>
#include <vector>
#include "Entity.h"

// Here we trade memory for functionality, we could store entities
// directly in the vector, but if we removed an entity in the middle,
// every element in the vector would shift, its easier to work
// with a shared pointer
typedef std::vector<std::shared_ptr<Entity>> EntityVector;


// Separate vectors for entity objects with the same tag.
// std::map<std::string, EntityVector>
// "Enemies", EntityVector (Stores all entities with the enemy tag)
typedef std::map<std::string, EntityVector> EntityMap;


// The Entity Manager is an example of the factory pattern, as the constructor in an entity is private and the entity manager
// is a friend it is the only object that has the ability to construct an entity.
class EntityManager
{
	EntityVector	m_entities;
	EntityVector	m_toAdd;
	EntityMap		m_entityMap;
	size_t			m_totalEntities = 0;

public:
	EntityManager() = default;
	std::shared_ptr<Entity> AddEntity(const std::string& tag);
	EntityVector& GetEntities();
	EntityVector& GetEntities(const std::string& tag);

	void RemoveDeadEntities(EntityVector& vector);

	void Update();
};

