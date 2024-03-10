#include "GameEngine.FWD.h"
#include "Scene_Menu.h"
#include "GameEngine.h"

#include <SFML/Graphics/RenderWindow.hpp>
#include <SFML/Window.hpp>
#include <SFML/Window/Event.hpp>
#include <memory>

GameEngine::GameEngine(const std::string& path)
{
    Init(path);
}

void GameEngine::Init(const std::string& path) 
{
    m_assets.LoadFromFile(path);
    m_window.create(sf::VideoMode(1280, 786), "Comp4300");
    m_window.setFramerateLimit(60);

    ChangeScene("MENU", std::make_shared<Scene_Menu>(this));
}

std::shared_ptr<Scene> GameEngine::CurrentScene() 
{
    return m_scenes[m_CurrentScene];
}

sf::RenderWindow& GameEngine::GetWindow() 
{
    return m_window;
}

void GameEngine::ChangeScene(const std::string& sceneName, std::shared_ptr<Scene> scene)
{
    m_CurrentScene = sceneName;
    m_scenes[sceneName] = scene;
}

void GameEngine::Run()
{
    while (IsRunning)
    {
        sUserInput();
        Update();
        m_window.display();
    }
}

void GameEngine::sUserInput()
{
    sf::Event event;
    while (m_window.pollEvent(event)) {
        if (event.type == sf::Event::Closed)
        {
            Quit();
        }
    }
}

void GameEngine::Quit()
{
    IsRunning = false;
    m_window.close();
}

// An update in the GameEngine is nothing more than the update function defined in a scene.
void GameEngine::Update()
{
    CurrentScene()->Update();
}


const Assets& GameEngine::GetAssets() const
{
    return m_assets;
}
