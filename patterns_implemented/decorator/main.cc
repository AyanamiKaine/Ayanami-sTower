#include "decorator.h"
#include <iostream>
#include <memory>

int main(){
  auto beverage = std::make_unique<Mocha>(std::make_unique<HouseBlend>()); 
  std::cout << beverage->getDescription() << ", Cost: $" 
            << beverage->cost() << "\n"; 

  // Here we create an onion/decorator layer of House Blend -> Soy -> Mocha
  auto beverage2 = std::make_unique<Mocha>(std::make_unique<Soy>(std::make_unique<HouseBlend>()));
  std::cout << beverage2->getDescription() << ", Cost: $"
            << beverage2->cost() << "\n";

  return 0;
}
