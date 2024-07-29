# StellaSocket: A C# Library for Scalable Messaging

StellaSocket is a C# library that simplifies the use of NNG (Nanomsg Next Gen), a high-performance messaging library. It provides a user-friendly interface to create various types of sockets for different messaging patterns, enabling efficient communication between applications.

## Features

- Socket Creation: Easily create sockets of various types: Pair, Bus, Pub, Pull, Push, Request, Respondent, Response, Sub, Surveyor.
- Connection and Binding: Connect to remote addresses as a client or bind to local addresses as a server.
- Send and Receive: Send and receive string messages effortlessly.
- Memory Management: Automatic freeing of received messages to avoid memory leaks.
- Error Handling: Provides exceptions for invalid socket types.
