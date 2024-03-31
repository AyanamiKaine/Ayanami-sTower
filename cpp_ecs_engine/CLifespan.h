#pragma once

class CLifespan
{
public:
	int remaining	= { 0 };
	int total		= { 0 };
	CLifespan(int total)
		: remaining(total), total(total) {}
		
};
