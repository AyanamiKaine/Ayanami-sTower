#include "observer.h"
#include <memory>
#include <algorithm>
#include <iostream>

void WeatherData::registerObserver(std::shared_ptr<Observer> o) {
    observers.push_back(o);
};

void WeatherData::removeObserver(std::shared_ptr<Observer> o) {
    // Using the erase-remove idiom for efficiency and correctness
    // This is a pattern often encountered in c++ when removing a specifc element from an vector
    // This maybe worthy to be explained in an example project
    observers.erase(std::remove_if(observers.begin(), observers.end(),
                                   [o](const std::shared_ptr<Observer>& obs) { 
                                       return obs == o; 
                                    }), 
                    observers.end());
}

void WeatherData::notifyObservers()  {
    for (auto& observer : observers) {
        observer->update(temperature, humidity, pressure); 
    }
}

void WeatherData::setMeasurements(float temp,
                                  float humid,
                                  float press) {
    temperature = temp;
    humidity    = humid;
    pressure    = press;
    mesurementsChanged();
};

void WeatherData::mesurementsChanged(){
    notifyObservers();
}

void Display::update(float temp,
                     float humid,
                     float press){
    temperature = temp;
    humidity    = humid;
    pressure    = press;
    
    display();
};

void Display::display(){
    std::cout << "Current Temperature:" << temperature << "\n";
    std::cout << "Current Humidity:" << humidity << "\n";
    std::cout << "Current Pressure:" << pressure << "\n";
};