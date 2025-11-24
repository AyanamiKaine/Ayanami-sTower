# NanoRPC Protocol Specification

**Version:** 1.0  
**Date:** November 2025  
**Status:** Stable

This document describes the NanoRPC binary protocol for implementing compatible clients and servers in any programming language.

---

## Table of Contents

1. [Overview](#overview)
2. [Transport Layer](#transport-layer)
3. [Frame Format](#frame-format)
4. [Message Types](#message-types)
5. [Protocol Limits](#protocol-limits)
6. [Message Flows](#message-flows)
7. [Serialization](#serialization)
8. [Error Handling](#error-handling)
9. [Implementation Guide](#implementation-guide)
10. [Examples](#examples)

---

## Overview

NanoRPC is a lightweight binary RPC protocol designed for high-performance communication between distributed services. It supports:

- **RPC (Remote Procedure Call)** - Request/response pattern
- **Cast (Fire-and-Forget)** - One-way messages without response
- **Pub/Sub** - Topic-based publish/subscribe messaging
- **Streaming** - Server-to-client data streaming with backpressure

### Design Goals

- **Simplicity** - Easy to implement in any language
- **Performance** - Minimal overhead, binary encoding
- **Flexibility** - Supports multiple communication patterns
- **Safety** - Built-in limits to prevent resource exhaustion

---

## Transport Layer

NanoRPC operates over **TCP** connections.

- **Default Port:** Application-defined (commonly 8023)
- **Connection:** Persistent, full-duplex
- **Byte Order:** Big-endian (network byte order)
- **String Encoding:** UTF-8

### Connection Lifecycle

```
Client                              Server
   |                                   |
   |-------- TCP Connect ------------->|
   |                                   |
   |-------- Handshake Frame --------->|  (optional)
   |                                   |
   |<======= Message Exchange ========>|
   |                                   |
   |-------- TCP Close --------------->|
   |                                   |
```

---

## Frame Format

Every message is a single **frame** consisting of a fixed-size header followed by a variable-length body.

### Header Structure (17 bytes)

```
 0                   1                   2                   3
 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|     Type      |                  Message ID                   |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|  (Msg ID cont)|                 Target Length                 |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
| (TgtLen cont) |                 Method Length                 |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
| (MthLen cont) |                  Body Length                  |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
| (BodyLen cont)|
+-+-+-+-+-+-+-+-+
```

| Offset | Size    | Field         | Description                                           |
| ------ | ------- | ------------- | ----------------------------------------------------- |
| 0      | 1 byte  | Type          | Message type (see [Message Types](#message-types))    |
| 1      | 4 bytes | Message ID    | Unique request identifier (uint32, big-endian)        |
| 5      | 4 bytes | Target Length | Length of target string in bytes (uint32, big-endian) |
| 9      | 4 bytes | Method Length | Length of method string in bytes (uint32, big-endian) |
| 13     | 4 bytes | Body Length   | Length of JSON payload in bytes (uint32, big-endian)  |

**Total Header Size: 17 bytes**

### Body Structure

The body immediately follows the header and consists of three parts:

```
+----------------+----------------+------------------+
|     Target     |     Method     |   JSON Payload   |
|  (TargetLen)   |  (MethodLen)   |    (BodyLen)     |
+----------------+----------------+------------------+
```

| Part    | Size            | Encoding   | Description           |
| ------- | --------------- | ---------- | --------------------- |
| Target  | TargetLen bytes | UTF-8      | Actor/service name    |
| Method  | MethodLen bytes | UTF-8      | Action/method name    |
| Payload | BodyLen bytes   | UTF-8 JSON | Request/response data |

### Complete Frame

```
+--------+------------+--------------+--------------+-----------+
| Header |   Target   |    Method    |     JSON     |
| (17B)  | (variable) |  (variable)  |  (variable)  |
+--------+------------+--------------+--------------+-----------+
         |<------------ Body (TargetLen + MethodLen + BodyLen) ----------->|
```

---

## Message Types

### RPC Messages (0x01 - 0x0F)

| Value  | Name          | Description                           |
| ------ | ------------- | ------------------------------------- |
| `0x01` | **Call**      | RPC request expecting a response      |
| `0x02` | **Cast**      | Fire-and-forget message (no response) |
| `0x03` | **Reply**     | Successful response to a Call         |
| `0x04` | **Error**     | Error response to a Call              |
| `0x05` | **Handshake** | Connection initialization             |

### Pub/Sub Messages (0x10 - 0x1F)

| Value  | Name            | Description                |
| ------ | --------------- | -------------------------- |
| `0x10` | **Subscribe**   | Subscribe to a topic       |
| `0x11` | **Unsubscribe** | Unsubscribe from a topic   |
| `0x12` | **Publish**     | Publish message to a topic |

### Streaming Messages (0x20 - 0x2F)

| Value  | Name             | Description             |
| ------ | ---------------- | ----------------------- |
| `0x20` | **StreamStart**  | Start a new stream      |
| `0x21` | **StreamData**   | Stream data chunk       |
| `0x22` | **StreamEnd**    | End of stream (success) |
| `0x23` | **StreamCancel** | Cancel/abort stream     |

---

## Protocol Limits

To prevent resource exhaustion and ensure security:

| Limit             | Value                    | Description                                 |
| ----------------- | ------------------------ | ------------------------------------------- |
| Max Target Length | 256 bytes                | Maximum UTF-8 encoded target name           |
| Max Method Length | 256 bytes                | Maximum UTF-8 encoded method name           |
| Max Body Length   | 16,777,216 bytes (16 MB) | Maximum JSON payload size                   |
| Max Frame Size    | 16,777,729 bytes         | Header + Max Target + Max Method + Max Body |

### Validation Rules

1. **Type** must be a valid message type (see table above)
2. **Target Length** must be 0 ≤ length ≤ 256
3. **Method Length** must be 0 ≤ length ≤ 256
4. **Body Length** must be 0 ≤ length ≤ 16,777,216

If validation fails, the connection SHOULD be closed.

---

## Message Flows

### RPC Call/Reply

```
Client                              Server
   |                                   |
   |------ Call (id=1) --------------->|
   |       target: "math"              |
   |       method: "add"               |
   |       body: {"a": 1, "b": 2}      |
   |                                   |
   |<----- Reply (id=1) ---------------|
   |       body: {"result": 3}         |
   |                                   |
```

### RPC Call/Error

```
Client                              Server
   |                                   |
   |------ Call (id=2) --------------->|
   |       target: "math"              |
   |       method: "divide"            |
   |       body: {"a": 1, "b": 0}      |
   |                                   |
   |<----- Error (id=2) ---------------|
   |       body: {"error": "..."}      |
   |                                   |
```

### Cast (Fire-and-Forget)

```
Client                              Server
   |                                   |
   |------ Cast (id=0) --------------->|
   |       target: "logger"            |
   |       method: "log"               |
   |       body: {"msg": "hello"}      |
   |                                   |
   |       (no response)               |
   |                                   |
```

**Note:** Cast messages typically use `id=0` since no correlation is needed.

### Pub/Sub

```
Subscriber                          Server                          Publisher
    |                                  |                                  |
    |------ Subscribe ---------------->|                                  |
    |       target: "events"           |                                  |
    |                                  |                                  |
    |                                  |<-------- Publish ----------------|
    |                                  |          target: "events"        |
    |                                  |          body: {"data": ...}     |
    |                                  |                                  |
    |<-------- Publish ----------------|                                  |
    |          target: "events"        |                                  |
    |          body: {"data": ...}     |                                  |
    |                                  |                                  |
    |------ Unsubscribe -------------->|                                  |
    |       target: "events"           |                                  |
    |                                  |                                  |
```

### Streaming

```
Client                              Server
   |                                   |
   |------ StreamStart (id=5) -------->|
   |       target: "counter"           |
   |       method: "count"             |
   |       body: {"count": 3}          |
   |                                   |
   |<----- StreamData (id=5) ----------|
   |       body: 1                     |
   |                                   |
   |<----- StreamData (id=5) ----------|
   |       body: 2                     |
   |                                   |
   |<----- StreamData (id=5) ----------|
   |       body: 3                     |
   |                                   |
   |<----- StreamEnd (id=5) -----------|
   |                                   |
```

#### Stream Cancellation

```
Client                              Server
   |                                   |
   |------ StreamStart (id=6) -------->|
   |                                   |
   |<----- StreamData (id=6) ----------|
   |                                   |
   |------ StreamCancel (id=6) ------->|
   |                                   |
   |       (server stops sending)      |
   |                                   |
```

---

## Serialization

### JSON Payload Format

All payloads are serialized as **UTF-8 encoded JSON**.

#### Request Payload

```json
{
	"field1": "value1",
	"field2": 123,
	"nested": {
		"data": [1, 2, 3]
	}
}
```

#### Success Response Payload

Any valid JSON value:

```json
{ "result": 42 }
```

```json
"simple string response"
```

```json
[1, 2, 3]
```

#### Error Response Payload

```json
{
	"error": "Human-readable error message",
	"type": "ExceptionTypeName"
}
```

| Field | Type   | Required | Description               |
| ----- | ------ | -------- | ------------------------- |
| error | string | Yes      | Error message             |
| type  | string | No       | Exception/error type name |

### Empty Payloads

For messages with no data, use an empty JSON object:

```json
{}
```

---

## Error Handling

### Protocol Errors

Close the connection if:

1. Header validation fails
2. Invalid message type received
3. Frame exceeds protocol limits
4. Malformed UTF-8 in target/method
5. Invalid JSON in payload

### Application Errors

For RPC errors, respond with an `Error` frame (0x04):

```
Error Frame:
  Type: 0x04
  Id: <same as request>
  Target: <same as request>
  Method: <same as request>
  Body: {"error": "Description", "type": "ErrorType"}
```

### Timeout Handling

- Clients SHOULD implement request timeouts
- Timeout duration is implementation-defined (recommended: 5000ms default)
- On timeout, remove pending request and optionally close connection

---

## Implementation Guide

### Pseudocode: Reading a Frame

```pseudocode
function readFrame(socket):
    // 1. Read header (exactly 17 bytes)
    headerBytes = socket.readExactly(17)

    // 2. Parse header
    type      = headerBytes[0]
    id        = readUInt32BE(headerBytes, 1)
    targetLen = readUInt32BE(headerBytes, 5)
    methodLen = readUInt32BE(headerBytes, 9)
    bodyLen   = readUInt32BE(headerBytes, 13)

    // 3. Validate header
    if not isValidType(type):
        throw ProtocolError("Invalid message type")
    if targetLen > 256 or methodLen > 256 or bodyLen > 16MB:
        throw ProtocolError("Limit exceeded")

    // 4. Read body
    totalBodyLen = targetLen + methodLen + bodyLen
    bodyBytes = socket.readExactly(totalBodyLen)

    // 5. Parse body
    target  = decodeUTF8(bodyBytes[0:targetLen])
    method  = decodeUTF8(bodyBytes[targetLen:targetLen+methodLen])
    payload = parseJSON(bodyBytes[targetLen+methodLen:])

    return Frame(type, id, target, method, payload)
```

### Pseudocode: Writing a Frame

```pseudocode
function writeFrame(socket, type, id, target, method, payload):
    // 1. Encode strings
    targetBytes = encodeUTF8(target)
    methodBytes = encodeUTF8(method)
    payloadBytes = serializeJSON(payload)

    // 2. Validate limits
    if len(targetBytes) > 256:
        throw Error("Target too long")
    if len(methodBytes) > 256:
        throw Error("Method too long")
    if len(payloadBytes) > 16MB:
        throw Error("Payload too large")

    // 3. Build header
    header = new byte[17]
    header[0] = type
    writeUInt32BE(header, 1, id)
    writeUInt32BE(header, 5, len(targetBytes))
    writeUInt32BE(header, 9, len(methodBytes))
    writeUInt32BE(header, 13, len(payloadBytes))

    // 4. Write frame
    socket.write(header)
    socket.write(targetBytes)
    socket.write(methodBytes)
    socket.write(payloadBytes)
```

### Pseudocode: RPC Client

```pseudocode
class NanoClient:
    pendingRequests = Map<uint32, Promise>()
    nextId = 0

    function call(target, method, payload, timeout=5000):
        id = atomicIncrement(nextId)
        promise = new Promise()

        pendingRequests[id] = promise

        writeFrame(socket, CALL, id, target, method, payload)

        result = await promise.withTimeout(timeout)

        return result

    function handleIncoming(frame):
        if frame.type == REPLY:
            if pendingRequests.has(frame.id):
                pendingRequests[frame.id].resolve(frame.payload)
                pendingRequests.remove(frame.id)

        else if frame.type == ERROR:
            if pendingRequests.has(frame.id):
                pendingRequests[frame.id].reject(frame.payload.error)
                pendingRequests.remove(frame.id)
```

---

## Examples

### Binary Frame Examples

#### Call Frame

Request to call `math.add` with `{"a": 10, "b": 20}`:

```
Hex dump (total 43 bytes):

Header (17 bytes):
01                      # Type: Call (0x01)
00 00 00 01             # ID: 1
00 00 00 04             # Target Length: 4
00 00 00 03             # Method Length: 3
00 00 00 0F             # Body Length: 15

Body (26 bytes):
6D 61 74 68             # Target: "math" (4 bytes)
61 64 64                # Method: "add" (3 bytes)
7B 22 61 22 3A 31 30    # Body: {"a":10,"b":20} (15 bytes)
2C 22 62 22 3A 32 30 7D
```

#### Reply Frame

Response with `{"result": 30}`:

```
Header (17 bytes):
03                      # Type: Reply (0x03)
00 00 00 01             # ID: 1 (matches request)
00 00 00 04             # Target Length: 4
00 00 00 03             # Method Length: 3
00 00 00 0E             # Body Length: 14

Body:
6D 61 74 68             # Target: "math"
61 64 64                # Method: "add"
7B 22 72 65 73 75 6C    # Body: {"result":30}
74 22 3A 33 30 7D
```

#### Subscribe Frame

Subscribe to topic "events":

```
Header (17 bytes):
10                      # Type: Subscribe (0x10)
00 00 00 00             # ID: 0 (not used)
00 00 00 06             # Target Length: 6
00 00 00 00             # Method Length: 0
00 00 00 02             # Body Length: 2

Body:
65 76 65 6E 74 73       # Target: "events"
                        # Method: "" (empty)
7B 7D                   # Body: {}
```

---

## Language Implementation Checklist

When implementing NanoRPC in a new language, ensure:

- [ ] Big-endian byte order for all integers
- [ ] UTF-8 encoding for strings
- [ ] JSON serialization for payloads
- [ ] Header validation before reading body
- [ ] Proper handling of all message types
- [ ] Request/response correlation by ID
- [ ] Timeout support for RPC calls
- [ ] Thread-safe send operations (mutex/lock)
- [ ] Graceful connection close handling
- [ ] Protocol limit enforcement

---

## Version History

| Version | Date          | Changes               |
| ------- | ------------- | --------------------- |
| 1.0     | November 2025 | Initial specification |

---

## License

This protocol specification is released under the MIT License.
