#pragma once
#include "Component.h"
#include "Vector2D.h"

class CTransform :public Component
{
public:
	Vector2D position	= { 0.0, 0.0 };
	Vector2D velocity	= { 0.0, 0.0 };
	Vector2D scale		= { 1.0, 1.0 };
	double angle		= { 0 };

    CTransform(){};
    CTransform(const Vector2D& position): 
        position(position) {}

    CTransform(const Vector2D& position, const Vector2D& velocity, float angle)
		: position(position), velocity(velocity), angle(angle) {}
};
