#include "pti.h"

int main(){
    // Mallard with default behaviors
    Duck duck; 

    duck.performQuack();
    duck.performFly();


    RubberDuck rubberDuck;
    rubberDuck.performQuack();
    rubberDuck.performFly();

    auto FlyRocketPoweredBehavior = std::make_unique<FlyRocketPowered>();
    rubberDuck.setFlyBehavior(std::move(FlyRocketPoweredBehavior)); // Move ownership 
    
    rubberDuck.performFly();
    return 0;
};