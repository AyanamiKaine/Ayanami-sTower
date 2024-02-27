#include "factory.h"
#include <memory>

void CheesePizza::prepare() { 
    std::cout << "Preparing Cheese Pizza\n"; 
}

void VeggiePizza::prepare() { 
    std::cout << "Preparing Veggie Pizza\n"; 
}
std::unique_ptr<Pizza> PizzaStore::orderPizza(const std::string& type) {
    std::unique_ptr<Pizza> pizza = createPizza(type);
    pizza->prepare();
    return pizza;
}

std::unique_ptr<Pizza> NYPizzaStore::createPizza(const std::string& type) {
    if (type == "cheese") {
            return std::make_unique<CheesePizza>();
    } else if (type == "veggie") {
            return std::make_unique<VeggiePizza>();
    } else {
        return std::make_unique<VeggiePizza>(); // Example default
        //return std::nullopt;  // Here we could use the optional to say 
                                // that maybe no object gets created
                                // another possiblity would be to instead
                                // return a default object "return std::make_unique<DefaultPizza>();" 
                                // With std::optional we move the handling of a possible empty object
                                // to the client
    }
}