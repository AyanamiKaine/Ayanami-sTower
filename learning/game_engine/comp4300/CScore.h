#pragma once

#include "Component.h"

class CScore : public Component
{
public:
	int score {0};
    
    CScore(){};
	CScore(int score) : score(score){}
};
