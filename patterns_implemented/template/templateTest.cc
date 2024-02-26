#include "template.h"
#include <gtest/gtest.h>

TEST(TemplateTest, TemplateExample) {
    CSVProcessor csvProcessor;
    csvProcessor.processFile("data.csv");
    
    XMLProcessor xmlProcessor;
    xmlProcessor.processFile("data.xml");
}