#pragma once
#include <map>
#include <memory>
#include <ranges>
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

inline std::shared_ptr<Entity> EntityManager::AddEntity(const std::string& tag)
{
	// Create a new Entity Object
	auto entity = std::shared_ptr<Entity>(new Entity (m_totalEntities++, tag));

	m_toAdd.push_back(entity);

	// return the shared pointer pointing to that entity
	return entity;
}

inline EntityVector& EntityManager::GetEntities()
{
	return m_entities;
}

inline EntityVector& EntityManager::GetEntities(const std::string& tag)
{
	return m_entityMap[tag];
}

inline void EntityManager::RemoveDeadEntities(EntityVector& vector) {
	for (auto it = vector.begin(); it != vector.end(); ) {
		if (!(*it)->IsActive()) {
			it = vector.erase(it); // Erase and get next iterator
		}
		else {
			++it;
		}
	}
}

// When we modify our entity vector we only do the modifications in
// this update loop to avoid iterator invalidation, this function
// should run after all iterations of the vectors is finished
inline void EntityManager::Update()
{
	for (auto e : m_toAdd)
	{
		m_entities.push_back(e);
		m_entityMap[e->Tag()].push_back(e);
	}


	RemoveDeadEntities(m_entities);

	for (auto& entityVec : m_entityMap | std::views::values)
	{
		RemoveDeadEntities(entityVec);
	}

	m_toAdd.clear();
}
