#include "Scene.h"
#include "GameEngine.FWD.h"
#include <cstddef>

Scene::Scene(GameEngine* gameEngine) 
:m_game(gameEngine) {}

Scene::~Scene() {}

void Scene::registerAction(int inputKey, const std::string& actionName)
{
    m_actionMap[inputKey] = actionName;
}

void Scene::doAction(const Action& action)
{
    sDoAction(action);
}

std::map<int, std::string> Scene::GetActionMap()
{
    return m_actionMap;
}

size_t Scene::Width() const 
{
    return m_game->GetWindow().getSize().x;  
}

size_t Scene::Height() const
{
    return m_game->GetWindow().getSize().y;
}

size_t Scene::CurrentFrame() const
{
    return m_currentFrame;
}
