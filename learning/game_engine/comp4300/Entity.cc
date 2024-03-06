#include "Entity.h"

Entity::Entity(size_t id, const std::string& tag) :
    m_id(id), 
    m_tag(tag)
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

const size_t Entity::Id() const
{
	return m_id;
}

void Entity::Destroy()
{
	m_alive = false;
}
