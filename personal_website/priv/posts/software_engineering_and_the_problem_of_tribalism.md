%{
  published: false,
  title: "Software Engineering and the Problem of Tribalism",
  category: "computer-science"
}
---

When I program, I am constantly hit with a storm of questions: Which language is a better choice here?  Static or dynamic typing?  What's the most performant framework? What about libraries. The frustrating truth is that there are rarely simple answers.  Choosing the right tools is less about trade-offs and more about applying flexible heuristics. 


This question can be paralyzing. We love to put languages on a pedestal, yet each comes with its own constraints and areas where it shines.  The challenge is to understand the boundaries of our tools and combat the biases that often lead us astray.


Many of those questions have simply no cookie cutter answer, only heuristics *(Note, how I dont say tradeoffs)*.

## Which Programming language should i use?

This always breaks my head, why? Because i love all programming languages equally, all have their trade offs. But some of the trade-offs are not as simply visible than others.

Even then trade-offs are quite irrelevant because of the concept of [Layers of Limits](./layers_of_limits.md), Lets look at performance, C++ should perform even with not using the most performant idioms faster then python or JavaScript in all most all cases. But when performance was not the limiting factor in all three programs but instead I/O from another process or server. Making the computation in JS or Python would not make the overall system faster. As other components are the **Limiting Factor**

Still people say performance is the most important aspect even though they don't know the limits of their system (https://news.ycombinator.com/item?id=8572085) 

Why is that? I believe it mostly comes from defensive programming, first people use lets say JavaScript to solve a problem for now the performance of JS is not the limiting factor but instead I/O from disk, later I/O gets faster and faster soon the limiting factor of JS is reached. And cannot be overcome with performance optimizations in JS itself, now JS is the limiting factor. Overcoming this limit is not as easy as changing algorithms, data structures or upgrading hardware. As the problem lies in the fact that the underlying hardware is not 100% efficiently be used. Many times changing the programming language entirely is used to combat that problem. There are many talks of the kind: "We rewrote program in X in language Y and latency is down by 50%! and speed up by 2X!". When people experience this journey they become biased in seeking performance *before* it becomes a big problem.

### (Hard) Limits of Programming Languages
- Performance   (This is based on how efficient a program language is in relation to the underlying hardware)
- Platforms     (Where you program can even run)
- Libraries     (This determines how much you have to do yourself)
### Believes

Beliefs and ideas play a big role in the tools we use.

* "I hate OOP for its indirection", "You never did True OOP!"
* "I hate FP for its slowness", "You choose the wrong data structures and algorithms!"
* "Static Types help navigate complex codebases!", "Time that you will lose because you will fight with the type system!"
* "Dynamic Types lets me program much faster, getting things done!", "Time you will lose because type errors will happen at run time, so you will have to write much more tests!"
* "90% of your entire program is just in 10% of the code, just optimize the 10%!, the rest is irrelevant", "High latency spikes in the rest 90%, will make you 10% optimizations irrelevant at the low 1% and 99% times"


If i would believe the common ideas from programmers, i would have to stop programming and start hitting my head on a rock.

The problem is with slogans "PERCENT_NUMBER of PROBLEM are caused by CAUSE", "Avoid CAUSE to become productive", "Do SOLUTION instead to avoid PROBLEM"

Those things "might" help but it does not mean its true. And the last part is really important: **truthiness**.

Many things online are spited as facts even though those facts where only **based on stories told around a fire**, those stories might be true but that doesn't mean their conclusions where.

We need actual science, not some believe or story. Stories are great to know we need to investigate further but **we cannot draw conclusions from stories** only from science. Another problem with this line of thinking is the fallacy [[Direct Causality (20240326001725)|Direct Causality]] **limited notions of linear causality**, and it is difficult or **impossible to incorporate nonlinear relationships**
        
"*Stories do not allow for conclusions*, they only open the door for further scientific research/discussion"

One reason why people are not using a programming language is "not having enough information"
https://www.youtube.com/watch?v=3MvKLOecT1I Garrett Smith - Why The Cool Kids Don't Use Erlang

In addition what people really want is a guide, that shows how to solve a particular problem step by step. How can I do this? or this? and examples, if there is no example, guide or tutorial people simply assume its not possible to do X. (This brings us back to the stories, if there is no story that tells us X is possible we assume its not or phrased another way we dont know how much effort we need to put in to achive X)

## Performance

This brings us directly to performance

Its not about any optimizations performance is all about finding and identifying the [Layers of Limits](./layers_of_limits.md), what really limits your system?, what could limit your system in the future?

People all the time say, video games cannot be made in an interpreted language **ITS TOO SLOW**, again just a stories, based on experience not science. The problem is that people do real decisions on experience instead of science, possibly choosing a bad one.

## Heuristics vs Trade-offs

Heuristics are based on beliefes and experiences while trade-offs are based on science.

This might suprise you, but choosing a tool on the bases of trade-offs is often quite missleading.

See this example, choosing python has the tradeoff of performance and platform availability (Of course there are some more for the sake of brevity they will be ommited). Choosing C++ has much better performance and platform availablity but suffers some tooling issues and a slower feedback loop in programming as you have to compile your program.

The thing is you only pay for the trade-off if it really affects you. 
- A C++ can compile in seconds (There are stories that it can take minutes in "real programs")
- A C++ program will run faster than a Python program but even 50 Times faster does not really matter when the difference is 0.001 ms to 0.050 ms

But what would be the correct heuristics to deal with this?

Requirments finding is hard, as often requirements unfold overtime.