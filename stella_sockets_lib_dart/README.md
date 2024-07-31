<!--
This README describes the package. If you publish this package to pub.dev,
this README's contents appear on the landing page for your package.

For information about how to write a good package README, see the guide for
[writing package pages](https://dart.dev/guides/libraries/writing-package-pages).

For general information about developing packages, see the Dart guide for
[creating packages](https://dart.dev/guides/libraries/create-library-packages)
and the Flutter guide for
[developing packages and plugins](https://flutter.dev/developing-packages).
-->

# StellaSocket: A Dart Library for Scalable Messaging

StellaSocket is a Dart library that simplifies the use of NNG (Nanomsg Next Gen), a high-performance messaging library. It provides a user-friendly interface to create various types of sockets for different messaging patterns, enabling efficient communication between applications.

## Features

- Socket Creation: Easily create sockets of various types: Push, Request
- Connection and Binding: Connect to remote addresses as a client or bind to local addresses as a server.
- Send and Receive: Send and receive string messages effortlessly.
- Memory Management: Automatic freeing of received messages to avoid memory leaks.
- Error Handling: Provides exceptions for invalid socket types.

## Dependencies

This application utilizes

- NNG (Nanomsg-Next-Generation)
- Stella Testing
- Stella Messaging

All are licensed under the MIT License and a copy of the license can be found in the LICENSE file included with this distribution.
