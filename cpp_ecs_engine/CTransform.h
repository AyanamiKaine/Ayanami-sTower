#pragma once
#include "Vector2.h"
class CTransform
{
public:
	Vector2 position	= { 0.0, 0.0 };
	Vector2 velocity	= { 0.0, 0.0 };
	Vector2 scale		= { 1.0, 1.0 };
	double angle		= { 0 };

	CTransform(const Vector2& position, const Vector2& velocity, float angle)
		: position(position), velocity(velocity), angle(angle) {}
};