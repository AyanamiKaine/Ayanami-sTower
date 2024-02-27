#include "visitor.h"

void ConcreteElementA::accept(Visitor& visitor) {
    visitor.visitConcreteElementA(*this);
}

void ConcreteElementB::accept(Visitor& visitor) {
    visitor.visitConcreteElementB(*this);
}

void ConcreteVisitor::visitConcreteElementA(ConcreteElementA& element) {
    std::cout << "Performing operation on ConcreteElementA\n";
}

void ConcreteVisitor::visitConcreteElementB(ConcreteElementB& element) {
    std::cout << "Performing operation on ConcreteElementB\n";
}