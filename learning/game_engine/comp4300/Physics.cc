#include "Physics.h"
#include "Entity.h"
#include "Vector2D.h"
#include <memory>

Vector2D Physics::GetOverlap(
    std::shared_ptr<Entity> entityA,
    std::shared_ptr<Entity> entityB) 
{
    // todo: return the overlap rectangle size of the bouding boxes of enetity a and b
    Vector2D posA = entityA->GetComponent<CTransform>().Position;
    Vector2D sizeA = entityA->GetComponent<CBoundingBox>().HalfSize;
    Vector2D posB = entityB->GetComponent<CTransform>().Position;
    Vector2D sizeB = entityB->GetComponent<CBoundingBox>().HalfSize;
    Vector2D delta{ std::abs(posA.x - posB.x), std::abs(posA.y - posB.y) };
    float ox = sizeA.x + sizeB.x - delta.x;
    float oy = sizeA.y + sizeB.y - delta.y;
    return Vector2D(ox, oy);
}
