#pragma once
#include <memory>
#include <string>

#include "CCollision.h"
#include "CInput.h"
#include "CLifespan.h"
#include "CScore.h"
#include "CShape.h"
#include "CTransform.h"

class Entity
{
	friend class EntityManager;

	const size_t		m_id	= 0;
	const std::string	m_tag	= "Default";
	bool				m_alive = true;


	Entity(size_t id, const std::string& tag);

public:

	std::shared_ptr<CTransform> cTransform;
	std::shared_ptr<CScore>		cScore;
	std::shared_ptr<CCollision> cCollision;
	std::shared_ptr<CInput>		cInput;
	std::shared_ptr<CShape>		cShape;
	std::shared_ptr<CLifespan>  cLifespan;

	const std::string& Tag() const;
	bool IsActive() const;
	const size_t Id() const;
	void Destroy();
};

inline Entity::Entity(size_t id, const std::string& tag) :
	m_id(id), m_tag(tag)
{

}

inline const std::string& Entity::Tag() const
{
	return m_tag;
}

inline bool Entity::IsActive() const
{
	return m_alive;
}

inline void Entity::Destroy()
{
	m_alive = false;
}
