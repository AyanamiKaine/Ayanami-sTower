#pragma once


#include "GameEngine.FWD.h"
#include "Scene.h"


#include <SFML/Graphics/RenderWindow.hpp>
#include <SFML/Window/Window.hpp>
#include <memory>
#include <string>
// Stores top level game data (Assets, sf::Window, Scenes)
// Performs top level functionality (changing Scenes, handling input)
class GameEngine {
public:
    void Updated();
    void Quit();
    void Run();
    void ChangeScene(Scene scene);
    sf::Window& GetWindow();
    void sUserInput();

private:
    std::string m_CurrentScene {""};
    
    sf::RenderWindow m_window;
    //Assets m_assets; 
    bool IsRunning {false};

    void Init();
    std::shared_ptr<Scene> CurrentScene();
            
};
