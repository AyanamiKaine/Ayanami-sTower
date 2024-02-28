#include "facade.h"
#include <iostream>

void SubsystemA::operation1() {
    std::cout << "SubsystemA: Operation1" << "\n";
};

void SubsystemB::operation1() {
    std::cout << "SubsystemB: Operation1" << "\n";
};

void Facade::operation(){
    std::cout << "Facade: Simplified Operation" << "\n";
    subSystemA->operation1();
    subSystemB->operation1();
}
