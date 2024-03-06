#pragma once
#include "Action.h"
#include "Scene.h"
#include <memory>
#include <string>

class Scene_Play : Scene {

public:
    void Update() override;

private:
    std::string m_levelPath;
    std::shared_ptr<Entity> m_player;
    //PlayerConfig m_playerConfig;

    void Init();

    //Systems
    void sAnimation();
    void sMovement();
    void sEnemySpawner();
    void sCollision();
 
    void sRender() override;
    void sDoAction(Action action) override;   

    void sDebug();
};
