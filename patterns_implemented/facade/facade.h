#include <memory>

// Complex Subsystem
class SubsystemA {
public:
    void operation1();
};

// Complex Subsystem
class SubsystemB {
public:
    void operation1();
};

// The Facade
// This is also an example of the rule of 0 as we dont explicitly define a constructor or destructor.
class Facade {
private:
    std::unique_ptr<SubsystemA> subSystemA {std::make_unique<SubsystemA>()};
    std::unique_ptr<SubsystemB> subSystemB {std::make_unique<SubsystemB>()};
public:
    void operation();
};
