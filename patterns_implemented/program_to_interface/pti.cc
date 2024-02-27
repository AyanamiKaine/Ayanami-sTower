#include "pti.h"
#include <iostream>

void FlyWithWings::fly() {
    std::cout << "Flying" << "\n";   
};

void FlyNoWay::fly() {
    std::cout << "Cant Fly" << "\n";   
};

void FlyRocketPowered::fly() {
    std::cout << "FLYING WITH A ROCKET" << "\n";
};

void Quack::quack() {
    std::cout << "Quack" << "\n";
};

void Squeak::quack() {
    std::cout << "Squeak" << "\n";
};

void MuteQuack::quack() {
    std::cout << "..." << "\n";
};


void Duck::performFly() {
    flyBehavior->fly();
};

void Duck::performQuack() {
    quackBehavior->quack();
};

void Duck::setFlyBehavior(std::unique_ptr<FlyBehavior> fb) {
    flyBehavior = std::move(fb);
};

void Duck::setQuackBehavior(std::unique_ptr<QuackBehavior> fb) {
    quackBehavior = std::move(fb);
};