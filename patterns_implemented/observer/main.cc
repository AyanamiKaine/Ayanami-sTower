#include "observer.h"

int main(){
    WeatherData weatherData;
    std::shared_ptr<Display> displayA = std::make_shared<Display>();
    std::shared_ptr<Display> displayB = std::make_shared<Display>();
    
    weatherData.registerObserver(displayA);
    weatherData.registerObserver(displayB);
    weatherData.setMeasurements(25, 65, 1013);  // Will trigger display update in 
                                                // in both displays.
    return 0;
}