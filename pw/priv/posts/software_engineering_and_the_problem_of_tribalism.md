%{
  published: true,
  title: "Software Engineering and the Problem of Tribalism",
  category: "computer-science"
}
---

When i was starting to learn programming I always had the following questions haunt me from most to least:
- Which language should i use?
- Dynamic or Static Typing?
- What about performance?
- Windows, Linux, Ios, Android, IPhone?
- Is this just a trivial scripting program or will it evolve into something more over time?
- What about spaghetti code?
- Should i extensively document my design choices and stable public API?

Many of those questions have simply no cookie cutter answer, only heuristics *(Note, how I dont say tradeoffs)*.

## Which Programming language should i use?

This always breaks my head, why? Because i love all programming languages equally, all have their trade offs. But some of the trade-offs are not as simply visible than others.

Even then trade-offs are quite irrelevant because of the concept of [Layers of Limits](./layers_of_limits.md), Lets look at performance, C++ should perform even with not using the most performant idioms faster then python or JavaScript in all most all cases. But when performance was not the limiting factor in all three programs but instead I/O from another process or server. Making the computation in JS or Python would not make the overall system faster. As other components are the **Limiting Factor**

Still people say performance is the most important aspect even though they don't know the limits of their system (https://news.ycombinator.com/item?id=8572085) 

Why is that? I believe it mostly comes from defensive programming, first people use lets say JavaScript to solve a problem for now the performance of JS is not the limiting factor but instead I/O from disk, later I/O gets faster and faster soon the limiting factor of JS is reached. And cannot be overcome with performance optimizations in JS itself, now JS is the limiting factor. Overcoming this limit is not as easy as changing algorithms, data structures or upgrading hardware. As the problem lies in the fact that the underlying hardware is not 100% efficiently be used. Many times changing the programming language entirely is used to combat that problem. There are many talk of the kind, we rewrote program in x in language y and latency is down by 50%! and speed up by 2X!. When people experience this journey they become biased in seeking performance *before* it becomes a big problem.

### (Hard)Limits of Programming Languages
- Performance   (This is based on how efficient a program language is in relation to the underlying hardware)
- Platforms     (Where you program can even run)
- Libraries     (This determines how much you have to do yourself)
### Believes

Believes and Ideas play a big role too in what tools we use.
- "I hate OOP for its indirection", "You never did True OOP!"
- "I hate FP for its slowness", "You choose the wrong data structures and algorithms!"
- "Static Types help navigate complex code bases!", "Time that you will lose because you will fight with the type system!"
- "Dynamic Types lets me program much faster, getting things done!", "Time you will lose because type errors will happen at run time, so you will have to write much more tests!"
- "90% of your entire program is just in 10% of the code, just optimize the 10%!, the rest is irrelevant", "High latency spikes in the rest 90%, will make you 10% optimizations irrelevant at the low 1% and 99% times"

If i would believe the common ideas from programmers, i would have to stop programming and start hitting my head on a rock.

The problem is with slogans "PERCENT_NUMBER of PROBLEM are caused by CAUSE", "Avoid CAUSE to become productive", "Do SOLUTION instead to avoid PROBLEM"

Those things "might" help but it does not mean its true. And the last part is really important: **truthiness**.

Many things online are spited as facts even though those facts where only **based on stories told around a fire**, those stories might be true but that doesn't mean their conclusions where.

We need actual science, not some believe or story. Stories are great to know we need to investigate further but **we cannot draw conclusions from stories** only from science. Another problem with this line of thinking is the fallacy [[Direct Causality (20240326001725)|Direct Causality]] **limited notions of linear causality**, and it is difficult or **impossible to incorporate nonlinear relationships**
        
"*Stories do not allow for conclusions*, they only open the door for further scientific research/discussion"

One reason why people are not using a programming language is "not having enough information"
https://www.youtube.com/watch?v=3MvKLOecT1I Garrett Smith - Why The Cool Kids Don't Use Erlang

In addition what people really want is a guide, that shows how to solve a particular problem step by step. How can I do this? or this? and Examples, if there is no example, guide or tutorial people simply assume its not possible to do x. (This brings us back to the stories, if there is no story that tells us x is possible we assume its not)

## Performance

This brings us directly to performance

Its not about any optimizations performance is all about finding and identifying the [Layers of Limits](./layers_of_limits.md), what really limits your system?, what could limit your system in the future, 

People all the time say, video games cannot be made in an interpreted language **ITS TOO SLOW**, again just a stories, based on experience not science. The problem is that people do real decisions on experience instead of science, possibly choosing a bad one.