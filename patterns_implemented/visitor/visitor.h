#include <iostream>
#include <vector>

// Forward declaration because of circular dependencies
class ConcreteElementA;
class ConcreteElementB;

class Visitor {
public:
    virtual void visitConcreteElementA(ConcreteElementA& element) = 0;
    virtual void visitConcreteElementB(ConcreteElementB& element) = 0;
};

class Element {
public:
    virtual void accept(Visitor& visitor) = 0;
};

class ConcreteElementA : public Element {
public:
    void accept(Visitor& visitor) override;
};

class ConcreteElementB : public Element {
public:
    void accept(Visitor& visitor) override;
};

class ConcreteVisitor : public Visitor {
public:
    void visitConcreteElementA(ConcreteElementA& element) override;
    void visitConcreteElementB(ConcreteElementB& element) override;
};