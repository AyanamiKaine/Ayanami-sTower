#include <iostream>
#include "template.h"
int main() {
    CSVProcessor csvProcessor;
    csvProcessor.processFile("data.csv");
        
    XMLProcessor xmlProcessor;
    xmlProcessor.processFile("data.xml");

    return 0;
}
