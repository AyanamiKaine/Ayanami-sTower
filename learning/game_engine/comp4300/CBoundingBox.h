#pragma once

#include "Vector2D.h"
#include "Component.h"

class CBoundingBox : public Component{
public:
    Vector2D Size;
    Vector2D HalfSize;

    CBoundingBox(){};
    CBoundingBox(const Vector2D& size):
        Size(size),
        HalfSize(size.x / 2.0, size.y / 2.0) {}

};
