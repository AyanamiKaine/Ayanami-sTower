#include <algorithm>
#include <vector>
#include <functional>
#include <numeric>
#include <execution>
// Imperative approach to getting one value based on a collection, like calculating the average.

class MathUtilImp {
public:
    double average_score(const std::vector<int>& scores) {
        
        int sum = 0;
        
        for (int score : scores) {
            sum += score;
        }

        return sum / (double)scores.size();
    };
};

// Functional approach

class MathUtilFunc {
public:

    double acc_average_score(const std::vector<int>& scores){
        //std::accumulate sums all values sequentially up into one based on a given collections beginning and ending
        return std::accumulate(
                scores.cbegin(), scores.cend(),
                0
                ) / (double)scores.size();
    }

    double reduce_average_score(const std::vector<int>& scores){
        return std::reduce(
                std::execution::par, // says do the reduce in parallel
                scores.cbegin(), scores.cend(),
                0
                )  / (double) scores.size();
    }

    int count_lines(const std::string& s) {
        return std::accumulate(
                s.cbegin(), s.cend(),
                0,
                f
                );
    }

    // We erase whitespaces as long as we dont find a char.
    // it’s better to pass the string by value instead of passing it as a const reference, because you’re modifying it and returning the modified version
    std::string trim_left(std::string s){
        s.erase(s.begin(),
                std::find_if(s.begin(), s.end(), is_not_space));
        return s;
    }

    std::string trim_right(std::string s){
        s.erase(std::find_if(s.rbegin(), s.rend(), is_not_space).base(), 
                s.end());
        return s;
    }

    std::string trim(std::string s){
        return trim_left(trim_right(std::move(s)));
    }
private:
    static bool is_not_space(char c){
        return (c != ' ');
    }
    //Helper function for counting lines
    static int f(int previous_count, char c) {
        return (c != '\n')   ? previous_count
                            : previous_count + 1;
    }
};
