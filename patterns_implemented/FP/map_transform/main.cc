#include "map_reduce.h"
#include <vector>
#include <iostream>

int main (int argc, char *argv[]) {
    
    auto nums = std::vector<int> {10, 20 ,30, 40, 50};

    MathUtilFunc math;

    std::cout << "Calculating the average via map reduce" << " " << math.reduce_average_score(nums) << "\n";

    std::string manyLines {"Hello, World\nNew line test\nMORE\n"};
    std::cout << manyLines;
    std::cout << "Counting lines:" << " " << math.count_lines(manyLines) << "\n"; 

    std::cout << math.trim_left("   Hello, World") << "\n";

    std::cout << math.trim("   Hello, World!   ") << "\n";

    return 0;
}
