#include "adapter.h"


void Adaptee::specificRequest() {
    std::cout << "Called Adaptee's specificRequest()\n"; 
}

void Adapter::request() {
    adaptee->specificRequest(); // Translating the request
}

