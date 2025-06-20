Here we test the various serialization formats for C#, we serialize to bytes and deserialize from it. Our goal is to find out what formats scale how much in latency, while there do exist many benchmarks the are often incomplete and dont show all formats.

- Cbor
- MessagePack-CSharp
- MemoryPack
- Json
- Protobuf
