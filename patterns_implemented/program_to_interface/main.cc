#include "pti.h"

int main(){
    Duck duck; 

    duck.performQuack();
    duck.performFly();


    RubberDuck rubberDuck;
    rubberDuck.performQuack();
    rubberDuck.performFly();

    auto FlyRocketPoweredBehavior = std::make_unique<FlyRocketPowered>();
    rubberDuck.setFlyBehavior(std::move(FlyRocketPoweredBehavior));
    
    rubberDuck.performFly();
    return 0;
};