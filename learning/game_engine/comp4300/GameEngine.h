#pragma once


#include "GameEngine.FWD.h"
#include "Scene.h"
#include "Assets.h"

#include <SFML/Graphics/RenderWindow.hpp>
#include <SFML/Window/Window.hpp>
#include <map>
#include <memory>
#include <string>
// Stores top level game data (Assets, sf::Window, Scenes)
// Performs top level functionality (changing Scenes, handling input)
class GameEngine {
public:
    void Update();
    void Quit();
    void Run();
    void ChangeScene(const std::string& sceneName ,std::shared_ptr<Scene> scene);
    sf::RenderWindow& GetWindow();
    void sUserInput();

    const Assets& GetAssets() const;

    GameEngine(const std::string& path);
protected:
    std::string m_CurrentScene {""};
    std::map<std::string, std::shared_ptr<Scene>> m_scenes;
    sf::RenderWindow m_window;
    Assets m_assets; 
    bool IsRunning {true};

    void Init(const std::string& path);
    std::shared_ptr<Scene> CurrentScene();
            
};
