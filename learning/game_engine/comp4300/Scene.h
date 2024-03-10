#pragma once
#include "Scene.FWD.h"
#include "GameEngine.FWD.h"

#include "Action.h"
#include "EntityManager.h"
#include "GameEngine.h"

#include <cstddef>
#include <map>
#include <memory>
#include <string>

class Scene {
protected:
    std::map<int, std::string> m_actionMap;
    GameEngine* m_game;
    size_t m_currentFrame;
    EntityManager m_entities;
    bool m_paused;
    bool m_hasEnded;

public:

    Scene(GameEngine* gameEngine);
    
    virtual ~Scene();
    virtual void Update()     = 0;
    virtual void sDoAction(Action action)  = 0;
    virtual void sRender()    = 0;
   
    void simulate(int value);
    void doAction(const Action& action);
    void registerAction(int inputKey, const std::string& actionName);
    std::map<int, std::string> GetActionMap();

    size_t Width() const;
    size_t Height() const;
    size_t CurrentFrame() const;
};
