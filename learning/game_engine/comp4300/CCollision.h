#pragma once

#include "Component.h"

class CCollision : public Component
{
public:
	float Radius {1};

    CCollision(){};
	CCollision(float radius):
	Radius(radius) {}
};
