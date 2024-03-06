#pragma once

#include "Component.h"

class CCollision : public Component
{
public:
	float radius {1};

    CCollision(){};
	CCollision(float radius):
	radius(radius) {}
};
