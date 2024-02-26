#ifndef TEMPLATE_H
#define TEMPLATE_H

#include <iostream>
#include <string>

// Abstract Base Class (File Processor)
class FileProcessor {
public:
    // The Template Method, this represents the structure of the algorithm, 
    // where we want specifc steps of it be implemented by subclasses to specify them.
    // We do this when the overall structure of two applications of an algorithm is 
    // same but their implemenation differs slightly
    void processFile(const std::string& filename) {
        openFile(filename); 
        readData();         
        processData();      
        writeFile();        
        closeFile();        
    }
    // Closing and Opening a file is identically for the purpose of this dummy algorithm
    void openFile(const std::string& filename);
    void closeFile();


protected:
    // These methods are specifc for some file types to we let subclasses overwrite those without chaning the the overall
    // structure we process files, the interface stays the same but not the implementation.
    virtual void readData() = 0;
    virtual void processData() = 0;
    virtual void writeFile() = 0;
};

class CSVProcessor : public FileProcessor {
protected:
    void readData() override;
    void processData() override;
    void writeFile() override;
};

class XMLProcessor : public FileProcessor {
protected:
    void readData() override;
    void processData() override;
    void writeFile() override;
};

#endif // TEMPLATE_H
