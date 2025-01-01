## Experimentations of working with Async Distributed Systems

I want to experiment with a system that works async in nature. It should not block when handling client requests, and should now always work with the Response/Request pattern but also allow for fire and forget.

## Pipes

Maybe thinking just in term of data transformations is the key. Not in a request/response term but instead in input -> output, and using simple json to do so. We are not using json to call specifc methods or returning errors. Avoiding internal state and focusing on input/output.

## Authentication

Two programs can may communicate to each other, but who should be allowed to? This is a crucial problem, if we expose the program only locally any local program may try to communicate with it. If we also expose it to the internet, the entire world may try one day to communicate with it. Here I will look deeper into _Asymmetric-key cryptography (Public-key cryptography)_.
