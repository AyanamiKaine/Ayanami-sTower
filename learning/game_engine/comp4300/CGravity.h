#pragma once

#include "Component.h"

class CGravity : public Component {
public:
    int value {1};

    CGravity(){};
    CGravity(int value) :
    value(value) {}
};
