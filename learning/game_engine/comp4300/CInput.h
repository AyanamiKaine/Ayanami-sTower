#pragma once

#include "Component.h"

class CInput : public Component
{
public:
	bool up		= false;
	bool left	= false;
	bool right	= false;
	bool down	= false;
	bool shoot	= false;

	CInput(){};
};
