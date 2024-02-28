#include "EntityManager.h"
#include <ranges>


std::shared_ptr<Entity> EntityManager::AddEntity(const std::string& tag)
{
	// Create a new Entity Object
	auto entity = std::shared_ptr<Entity>(new Entity (m_totalEntities++, tag));

	m_toAdd.push_back(entity);

	// return the shared pointer pointing to that entity
	return entity;
}

EntityVector& EntityManager::GetEntities()
{
	return m_entities;
}

EntityVector& EntityManager::GetEntities(const std::string& tag)
{
	return m_entityMap[tag];
}

void EntityManager::RemoveDeadEntities(EntityVector& vector) {
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
void EntityManager::Update()
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
