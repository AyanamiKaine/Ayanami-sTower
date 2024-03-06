#pragma once
#include "Component.h"
#include <SFML/Graphics/CircleShape.hpp>
#include <SFML/Graphics/Color.hpp>

class CShape : Component
{
public:
	sf::CircleShape circle;
    
    CShape(){};
	CShape(float radius, int points, const sf::Color& fill, const sf::Color& outline, float thickness)
		:circle(radius, points)
	{
		circle.setFillColor(fill);
		circle.setOutlineColor(outline);
		circle.setOutlineThickness(thickness);
		circle.setOrigin(radius, radius);
	}
};
