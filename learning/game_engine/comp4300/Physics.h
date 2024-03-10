#pragma once

#include "Entity.h"
class Physics {
public:
    bool IsCollision(Entity entityA, Entity entityB);
    //bool IsIntersect(line, line);
    //bool IsInside(Vec2D, line);
};
