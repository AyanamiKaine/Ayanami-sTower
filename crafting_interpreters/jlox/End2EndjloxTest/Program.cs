using jlox;

var inheritanceCall =
        """
        class Doughnut 
        { 
            cook() 
            { 
                print "Fry until golden brown."; 
            } 
        
        } 
        class BostonCream < Doughnut {} 
        BostonCream().cook();
        """;

var lox = new Lox();

lox.Run(inheritanceCall);
