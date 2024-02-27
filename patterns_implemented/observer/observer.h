#include <vector>
#include <memory>

class Observer {
public:
    virtual void update(float temp,
                        float humid,
                        float press) = 0;
};


class Subject {
public:
    virtual void registerObserver(std::shared_ptr<Observer> o) = 0; 
    virtual void removeObserver(std::shared_ptr<Observer> o) = 0; 
};

// We use the rule of zero, we dont define a specifc constructor or destructor
// We relie on defaults
class WeatherData : public Subject {
private:
    float temperature   = 0;
    float humidity      = 0;
    float pressure      = 0;
    
    std::vector<std::shared_ptr<Observer>> observers; 
public:
    void notifyObservers();
    void registerObserver(std::shared_ptr<Observer> o)      override;
    void removeObserver(std::shared_ptr<Observer> o)        override;

    void mesurementsChanged();
    void setMeasurements(float temperature, 
                         float humidity,
                         float pressure);
};

class Display : public Observer {
private:
    float temperature   = 0; 
    float humidity      = 0;
    float pressure      = 0;

public:
    void update(float temp,
                float humid,
                float press) override;

    void display();
};