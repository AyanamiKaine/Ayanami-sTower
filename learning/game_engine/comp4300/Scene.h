#pragma once
#include "Scene.FWD.h"
#include "GameEngine.FWD.h"

#include "Action.h"
#include "EntityManager.h"
#include "GameEngine.h"

#include <map>
#include <memory>
#include <string>

class Scene {
private:
    std::map<int, std::string> m_actionMap;
    std::shared_ptr<GameEngine> m_game;
    int m_currentFrame;
    EntityManager m_entities;
    bool m_paused;
    bool m_hasEnded;

public:

    virtual void Update()     = 0;
    virtual void sDoAction(Action action)  = 0;
    virtual void sRender()    = 0;
   
    void simulate(int value);
    void doActon(Action action);
    void registerAction(Action action);
};
