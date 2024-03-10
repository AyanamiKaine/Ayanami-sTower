#pragma once

#include "Entity.h"
#include "Vector2D.h"
#include <memory>
class Physics {
public:
    bool IsCollision(std::shared_ptr<Entity> entityA, std::shared_ptr<Entity> entityB);
    //bool IsIntersect(line, line);
    //bool IsInside(Vec2D, line);
    Vector2D GetOverlap(
        std::shared_ptr<Entity> entityA,
        std::shared_ptr<Entity> entityB
    );
};
