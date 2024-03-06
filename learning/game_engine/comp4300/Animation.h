#pragma once
#include "Vector2D.h"

#include <string>
#include <SFML/Graphics/Sprite.hpp>


/* The Animation Class holds a sprite that contains all different animation states in one file, we simply map the size of the sprite we want to animation to the different animation states.
*/



class Animation {
private:
    sf::Sprite m_sprite;
    int m_frameCount;
    int m_currentFrame;
    // How fast the animation should play
    int m_speed; 
    // How big one sprite in the animation file is
    Vector2D m_size;
    std::string name;

public:
    // Move the current animation state by one (Usually this means moving the current selected sprite from left to right)
    void Update();
    // Check if we have the last possible sprite selected
    void HasEnded();
    std::string& GetName();
    Vector2D& GetSize();
    sf::Sprite& GetSprite();
};


