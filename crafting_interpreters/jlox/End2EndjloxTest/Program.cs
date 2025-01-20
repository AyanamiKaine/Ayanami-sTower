using jlox;

var source =
"""
class Doughnut {
  init(flavor) {
    this.flavor = flavor;
    this.eaten = false;
  }

  eat() {
    print "Chomp chomp " + this.flavor + " doughnut.";
    this.eaten = true;
  }

  isEaten() {
    return this.eaten;
  }
}

class Cruller < Doughnut {
  init(flavor, sprinkles) {
    super.init(flavor);
    this.sprinkles = sprinkles;
  }

  eat() {
    super.eat();
    print "Sprinkles go everywhere!";
  }

  describe() {
      return "A cruller is a twisted doughnut.";
  }
}
   
var glazed = Doughnut("Glazed");
glazed.eat();

fun makeCounter() {
  var i = 0;
  fun count() {
    i = i + 1;
    print i;
  }
  return count;
}

var counter = makeCounter();
counter(); // 1
counter(); // 2
counter(); // 3

var anotherCounter = makeCounter();
anotherCounter(); // 1 - should be independent

""";

var lox = new Lox();

lox.Run(source);
