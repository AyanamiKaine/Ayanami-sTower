#pragma once

#include "Action.h"
#include "Scene.h"
#include <SFML/Graphics/Text.hpp>
#include <string>
#include <vector>

class Scene_Menu : Scene {
public:
    void Update() override;

private:
    std::vector<std::string> m_menuStrings;
    sf::Text m_menuText;
    std::vector<std::string> m_levelPaths;
    int m_menuIndex;


    void Init();

    // Systems
    void sRender() override;
    void sDoAction(Action action) override;
};
