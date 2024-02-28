#include "adapter.h"
#include <memory>


int main (int argc, char *argv[]) {
    
    auto adaptee = std::make_unique<Adaptee>();

    auto adapter = std::make_unique<Adapter>(std::move(adaptee)); 
    
    adapter->request();

    return 0;
}
