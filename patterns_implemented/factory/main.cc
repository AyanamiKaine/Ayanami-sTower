#include "factory.h"
#include <memory>

int main() {
    auto nyStore = std::make_unique<NYPizzaStore>();
    auto chessePizza = nyStore->orderPizza("cheese"); 
    auto defaultPizaa = nyStore->orderPizza("");
}