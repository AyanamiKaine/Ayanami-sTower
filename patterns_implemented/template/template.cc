#include "template.h"// Concrete Subclasses (CSVProcessor, XMLProcessor)



void CSVProcessor::readData() {
    std::cout << "Reading data from CSV file..." << std::endl;
}

void CSVProcessor::processData() {
        std::cout << "Processing CSV data..." << std::endl;
}

void CSVProcessor::writeFile() {
        std::cout << "Writing processed data to CSV file..." << std::endl;
}


void XMLProcessor::readData() {
        std::cout << "Reading data from XML file..." << std::endl;
}

void XMLProcessor::processData() {
        std::cout << "Processing XML data..." << std::endl;
}

void XMLProcessor::writeFile() {
        std::cout << "Writing processed data to XML file..." << std::endl;
}

void FileProcessor::closeFile()
{
    std::cout << "Opening File" << std::endl;
}
void FileProcessor::openFile(const std::string& filename)
{
    std::cout << "Closing File" << std::endl;
}

