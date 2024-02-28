#include <string>
#include <memory>
#include <utility>

//*** Decorator Pattern Explained ***//

// We attach addtional responsibilities to an object dynamically.

// Decorators provide a flexible alternatve to subclassing for 
// extending functionality

class Beverage {
protected:
    std::string description;
public:
    virtual std::string getDescription(); 
    virtual double cost() = 0;
    virtual ~Beverage() = default; 
};

class CondimentDecorator : public Beverage {
protected:
    std::unique_ptr<Beverage> beverage; 
public:
    CondimentDecorator(std::unique_ptr<Beverage> beverage) 
        : beverage(std::move(beverage)) {} 

    virtual std::string getDescription() = 0; 
};

class Expresso : public Beverage {
public:
    Expresso();
    double cost() override;
};

class HouseBlend : public Beverage {
public:
    HouseBlend();
    double cost() override;
};

class DarkRoast : public Beverage {
public:
    DarkRoast();
    double cost() override;
};

class Mocha : public CondimentDecorator {
public:
    Mocha(std::unique_ptr<Beverage> beverage) 
        : CondimentDecorator(std::move(beverage)) {}

    std::string getDescription() override;  
    double cost() override;  
};

class Soy : public CondimentDecorator {
public:
  Soy(std::unique_ptr<Beverage> beverage)
    : CondimentDecorator(std::move(beverage)) {}

  std::string getDescription() override;
  double cost() override;
};
