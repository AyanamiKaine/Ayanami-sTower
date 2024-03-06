#pragma once

#include "Component.h"

class CLifespan : public Component
{
public:
	int remaining	= { 0 };
	int total		= { 0 };
	
    CLifespan(){};
    CLifespan(int total)
		: remaining(total), total(total) {}
		
};
