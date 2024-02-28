#include "decorator.h"

std::string Beverage::getDescription(){
    return description;
};

Expresso::Expresso(){
    description = "Espresso";
};

double Expresso::cost(){
    return 1.99;
};

HouseBlend::HouseBlend(){
    description = "House Blend Coffe";
};

double HouseBlend::cost(){
  return 0.89;
};

DarkRoast::DarkRoast() {
  description = "Dark Roast Coffe";
}

double DarkRoast::cost() {
  return 1.20;
}

std::string Mocha::getDescription(){
    return beverage->getDescription() + ", Mocha";
};

double Mocha::cost() {
    return beverage->cost() + 0.20;
};

std::string Soy::getDescription() {
  return beverage->getDescription() + ", Soy";
};

double Soy::cost() {
  return beverage->cost() + 0.15;
};
