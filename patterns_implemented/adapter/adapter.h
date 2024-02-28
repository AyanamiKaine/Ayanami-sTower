#include <iostream>
#include <memory>
#include <utility>

// Target Interface 
class Target {
public:
    virtual void request() = 0;
};

// Adaptee (incompatible interface)
class Adaptee {
public:
    void specificRequest();
};

// Our adapter converts the Incompatible interface into one that is compatible.
class Adapter : public Target {
private:
    std::unique_ptr<Adaptee> adaptee;

public:
    Adapter(std::unique_ptr<Adaptee> adaptee) 
    : adaptee(std::move(adaptee)) {}

    void request() override;
};
