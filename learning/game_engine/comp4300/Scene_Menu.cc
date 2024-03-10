#include "Scene_Menu.h"
#include "GameEngine.FWD.h"
#include <SFML/System/Clock.hpp>
#include <SFML/System/Vector2.hpp>
#include <iostream>
#include <memory>

Scene_Menu::Scene_Menu(GameEngine* gameEngine) 
    :Scene(gameEngine) // Assuming your base 'Scene' class has the right constructor
{
    Init();
}

void Scene_Menu::Init()
{    

    m_menuText.setString("Hello, World");
    m_menuText.setFont(m_game->GetAssets().GetFont("Mario"));
    m_menuText.setCharacterSize(24);
    m_menuText.setFillColor(sf::Color::Black);
    m_menuText.setPosition(20,20);
}

void Scene_Menu::Update()
{
    sRender();
    sMoveTextAround();
}

void Scene_Menu::sRender()
{
    m_game->GetWindow().clear(sf::Color::White);
    m_game->GetWindow().draw(m_menuText);
}

void Scene_Menu::sDoAction(Action action) 
{

}

