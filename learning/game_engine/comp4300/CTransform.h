#pragma once
#include "Component.h"
#include "Vector2D.h"

class CTransform :public Component
{
public:
	Vector2D Position	= { 0.0, 0.0 };
	Vector2D Velocity	= { 0.0, 0.0 };
	Vector2D Scale		= { 1.0, 1.0 };
	double Angle		= { 0 };

    CTransform(){};
    CTransform(const Vector2D& position): 
        Position(position) {}

    CTransform(const Vector2D& position, const Vector2D& velocity, float angle)
		: Position(position), Velocity(velocity), Angle(angle) {}
};
