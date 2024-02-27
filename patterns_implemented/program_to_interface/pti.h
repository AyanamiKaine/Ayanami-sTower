#include <memory>

//Interface
class FlyBehavior {
public:
    virtual void fly() = 0;
};

//Interface
class QuackBehavior {
public:
    virtual void quack() = 0;
};

class FlyWithWings : public FlyBehavior {
public:
    void fly() override;
};

class FlyNoWay : public FlyBehavior {
public:
    void fly() override;
};

class FlyRocketPowered : public FlyBehavior {
public:
    void fly() override;
};

class Quack : public QuackBehavior {
public:
    void quack() override;
};

class Squeak : public QuackBehavior {
public:
    void quack() override;
};


class MuteQuack : public QuackBehavior {
public:
    void quack() override;
};

class Duck {
protected:
    std::unique_ptr<FlyBehavior> flyBehavior;
    std::unique_ptr<QuackBehavior> quackBehavior;
public:
    Duck() : flyBehavior(std::make_unique<FlyNoWay>()),
             quackBehavior(std::make_unique<Quack>()) {}  // Default behaviors

    // Parameterized constructor
    Duck(std::unique_ptr<FlyBehavior> fb, std::unique_ptr<QuackBehavior> qb)
        : flyBehavior(std::move(fb)), quackBehavior(std::move(qb)) {} 

    void performQuack();
    //void swim();
    //void display();
    void performFly();

    void setQuackBehavior(std::unique_ptr<QuackBehavior> qb);
    void setFlyBehavior(std::unique_ptr<FlyBehavior> fb);
};

class RubberDuck : public Duck {
public:    
    RubberDuck() 
        : Duck(std::make_unique<FlyNoWay>(), 
          std::make_unique<Squeak>()) 
    {}
};

class MallardDuck : public Duck {
public:    
    MallardDuck(std::unique_ptr<FlyBehavior> fb, std::unique_ptr<QuackBehavior> qb) 
        : Duck(std::move(fb), std::move(qb)) // Call the Duck constructor!
     {}
};