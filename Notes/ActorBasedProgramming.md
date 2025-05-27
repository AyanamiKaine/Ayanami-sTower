_"An actor is just a lambda, didnt you know?"_

# Actors enable long living systems

Using actors makes it much more possible to create an ever lasting system. A system that is robust and can handle failure much better than in other paradigms. See [Making reliable distributed systems in the presence of software errors](https://erlang.org/download/armstrong_thesis_2003.pdf) by Joe Armstrong.

# Actor frameworks vs Actor Programming Languages

Actors can be implemented in every language. But the way you use them would be vastly different. Conceptually a actor is often implemented as a state machine. But not every language can elegantly display a state machine in code itself. A programming language syntax design does a lot for ease of use. While C-Like language often have mature fully working actor frameworks their respective syntax design does not allow for elegant use of them. Look at actor based programming language and actors based on frameworks. They look much more nice to the eye and makes it much easier to *think* in actors.