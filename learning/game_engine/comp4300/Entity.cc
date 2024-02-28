#include "Entity.h"

Entity::Entity(size_t id, const std::string& tag) :
	m_id(id), m_tag(tag)
{

}

const std::string& Entity::Tag() const
{
	return m_tag;
}

bool Entity::IsActive() const
{
	return m_alive;
}

void Entity::Destroy()
{
	m_alive = false;
}
