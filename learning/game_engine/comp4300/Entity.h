#pragma once
#include <memory>
#include <string>
#include <tuple>
#include <utility>

#include "CCollision.h"
#include "CInput.h"
#include "CLifespan.h"
#include "CScore.h"
#include "CShape.h"
#include "CTransform.h"
#include "CBoundingBox.h"
#include "CAnimation.h"
#include "CGravity.h"
#include "CState.h"



typedef std::tuple<
    CTransform,
    CLifespan,
    CInput,
    CBoundingBox,
    CAnimation,
    CGravity,
    CState
> ComponentTuple;


class Entity
{
private:
	friend class EntityManager;

	const size_t		m_id	      {0};
	const std::string	m_tag	      {"Default"};
	bool				m_alive       {true};
    ComponentTuple      m_components;

	Entity(size_t id, const std::string& tag);

public:
	const std::string& Tag() const;
	bool IsActive() const;
	const size_t Id() const;
	void Destroy();

    template<typename Type>
    bool HasComponent() const
    {
        return GetComponent<Type>().has;    
    }
    
    template<typename Type, typename... TypeArgs>
    Type& AddComponent(TypeArgs&&... mArgs)
    {
        auto& component = GetComponent<Type>();
        component = Type(std::forward<TypeArgs>(mArgs)...);
        component.has = true;
        return component;
    }

    template<typename Type>
    Type& GetComponent()
    {
        return std::get<Type>(m_components);    
    }

    template<typename Type>
    void removeComponent()
    {
        GetComponent<Type>() = Type();    
    }
};
