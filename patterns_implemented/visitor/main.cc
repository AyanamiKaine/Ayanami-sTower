#include "visitor.h"
#include <memory>

int main() {
    std::vector<std::unique_ptr<Element>> elements;
    elements.push_back(std::make_unique<ConcreteElementA>());
    elements.push_back(std::make_unique<ConcreteElementB>());

    ConcreteVisitor visitor;

    for (auto& element : elements) {
        element->accept(visitor);
    }

    return 0;
}