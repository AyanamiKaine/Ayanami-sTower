- The following readme is AI-Generated

# StellaSharedMemory

**A high-performance, cross-platform .NET library for Inter-Process Communication (IPC) using memory-mapped files.**

StellaSharedMemory provides robust, easy-to-use abstractions for sharing data between multiple processes efficiently. It is designed with performance and flexibility in mind, offering two primary mechanisms for shared memory access: a simple locked memory segment and a sophisticated lock-free circular buffer.

---

## Features

- **Cross-Platform:** Works seamlessly on both **Windows** and **Linux** (`/dev/shm`).
- **Two Core Components:**
  - `SharedMemory`: A simple, file-locked segment for basic data sharing between two processes.
  - `MultiWriterCircularBuffer`: An advanced, lock-free, multiple-producer, multiple-consumer circular buffer for high-throughput messaging.
- **High Performance:** The circular buffer is designed for minimal contention and high-speed logging or event streaming, using atomic operations to avoid kernel-level locks during reads.
- **Fair Multi-Writer Access:** The `MultiWriterCircularBuffer` uses a ticket-based spin-lock to ensure that multiple writing processes get access in a fair, first-in, first-out (FIFO) order, preventing writer starvation.
- **Independent, Lock-Free Readers:** Multiple processes can read from the circular buffer concurrently without blocking each other or the writers. Each reader maintains its own position.
- **Pluggable Serialization:** Comes with built-in support for multiple serialization formats. An `ISerializer` interface makes it easy to add your own.
  - `JsonSerializer` (System.Text.Json)
  - `MessagePackObjectSerializer` (MessagePack)
  - `MemoryPackObjectSerializer` (MemoryPack)
  - `ProtobufObjectSerializer` (Protobuf-net)
- **Configurable Backpressure:** The circular buffer writer can be configured with different behaviors for when the buffer is full:
  - `Overwrite` (Default): The fastest mode. If the buffer is full, the writer overwrites the oldest data. Ideal for logging or telemetry where losing old messages is acceptable.
  - `Block`: Guarantees delivery. If the buffer is full, the writer will wait until a reader consumes data and frees up space.

---

## Core Components

### 1. `SharedMemory`

This is the simplest component, designed for sharing a single block of data between processes. It uses a file-based lock (`.lock`) to ensure that only one process can write to the memory at a time, preventing data corruption.

**Use Cases:**

- Sharing configuration data that is written once and read by many processes.
- Simple, low-frequency state synchronization between two applications.

**Example:**

```csharp
// Process A: Creates and writes data
using var serializer = new JsonSerializer();
using (var shm = new SharedMemory("MySharedData", SharedMemoryMode.Create, 1024))
{
    var myData = new { Message = "Hello from Process A", Value = 42 };
    shm.Write(myData, serializer);
}

// Process B: Opens and reads data
using (var shm = new SharedMemory("MySharedData", SharedMemoryMode.Open))
{
    var receivedData = shm.Read<dynamic>(serializer);
    Console.WriteLine(receivedData.Message); // "Hello from Process A"
}
```

### 2. MultiWriterCircularBuffer
   This is the powerhouse of the library. It implements a highly optimized circular buffer (or ring buffer) that allows multiple processes to write messages and multiple processes to read them concurrently with minimal locking.

#### Use Cases:

- High-speed, structured logging from multiple microservices to a single log aggregator.
- Inter-service event bus or message queue.
- Real-time data streaming between producers and consumers.

#### Example:

```csharp
// --- WRITER PROCESS ---
// Creates the buffer and writes messages.
// Multiple instances of this process can run and write concurrently.
var serializer = new JsonSerializer();
var message = new { Timestamp = DateTime.UtcNow, LogLevel = "Info", Text = "Application starting." };

using (var buffer = new MultiWriterCircularBuffer("MyLogBuffer", SharedMemoryMode.Create, 65536))
{
    buffer.Write(message, serializer);
}

// --- READER PROCESS ---
// Opens the buffer and reads all available messages.
// Multiple, independent readers can run.
using (var reader = new LockFreeBufferReader("MyLogBuffer"))
{
    while (reader.TryRead(serializer, out dynamic? logEntry))
    {
        Console.WriteLine($"[{logEntry.Timestamp}] {logEntry.LogLevel}: {logEntry.Text}");
    }
}
```

Backpressure Management
When using the MultiWriterCircularBuffer, you can control what happens when a writer is faster than the readers and the buffer becomes full. This is configured via the WriteBehavior enum during the writer's construction.

WriteBehavior.Overwrite (Default)
This mode prioritizes writer performance. The writer will never wait. If the buffer is full, it will simply wrap around and overwrite the oldest unread messages.

```csharp
// The writer will not block, even if readers are slow.
using var writer = new MultiWriterCircularBuffer(
    name: "MyFastBuffer",
    mode: SharedMemoryMode.Create,
    capacity: 16384,
    behavior: WriteBehavior.Overwrite // Explicitly setting the default
);
```

WriteBehavior.Block
This mode guarantees that no messages are lost. If a write operation would overwrite an unread message (based on the position of the slowest reader), the Write call will block and wait until space is freed up by a reader.

```csharp
// The writer will pause if it gets too far ahead of the slowest reader.
using var writer = new MultiWriterCircularBuffer(
    name: "MyGuaranteedBuffer",
    mode: SharedMemoryMode.Create,
    capacity: 16384,
    behavior: WriteBehavior.Block
);
```

## Getting Started

### Installation

### Building from Source

Clone the repository.

Open the solution in Visual Studio or use the .NET CLI.

Build the solution: dotnet build

### Running Tests

The project includes a comprehensive xUnit test suite that covers all features, including concurrency and backpressure scenarios.

To run the tests:

`dotnet test`
