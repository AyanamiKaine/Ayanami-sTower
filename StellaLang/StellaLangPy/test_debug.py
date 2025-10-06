"""Debug actor-to-actor communication."""
from src.VMActor import VMActor

# Test basic send_to
actor1 = VMActor()
actor2 = VMActor()

print("Before send_to:")
print(f"actor1.bytecode: {actor1.bytecode}")
print(f"actor2.bytecode: {actor2.bytecode}")

# Actor1 sends a message to actor2
actor1.send_to(actor2, "OP_CONSTANT", 42)

print("\nAfter send_to:")
print(f"actor1.bytecode: {actor1.bytecode}")
print(f"actor2.bytecode: {actor2.bytecode}")

# Actor2 processes the message
print("\nProcessing actor2 messages:")
count = 0
while actor2.handle_message():
    count += 1
    print(f"  Processed message #{count}")
    if count > 10:
        print("  Breaking - too many iterations!")
        break

print(f"\nFinal state:")
print(f"actor2.stack: {actor2.stack}")
print(f"actor2.top(): {actor2.top()}")
print(f"actor1.stack: {actor1.stack}")
