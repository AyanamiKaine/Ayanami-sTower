#include <iostream>
#include <memory>
#include <optional>

// Product (Abstract Base Pizza)
class Pizza {
public:
    virtual void prepare() = 0;
    virtual ~Pizza() = default; 
};

// Concrete Products
class CheesePizza : public Pizza {
public:
    void prepare() override;
};

class VeggiePizza : public Pizza {
public:
    void prepare() override;
};

// Creator (Abstract Pizza Store)
class PizzaStore {
public:
    std::unique_ptr<Pizza> orderPizza(const std::string& type);
    virtual std::unique_ptr<Pizza> createPizza(const std::string& type) = 0;
};

// Concrete Creators 
class NYPizzaStore : public PizzaStore {
public:
    std::unique_ptr<Pizza> createPizza(const std::string& type) override;
};