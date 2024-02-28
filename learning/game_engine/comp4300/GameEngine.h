#pragma once
#include <SFML/Graphics/RenderWindow.hpp>
#include <string>
// Stores top level game data (Assets, sf::Window, Scenes)
// Performs top level functionality (changing Scenes, handling input)
class GameEngine {
public:
    std::string CurrentScene {""};
    
    sf::RenderWindow window;
    
    bool IsRunning {false};  
};
