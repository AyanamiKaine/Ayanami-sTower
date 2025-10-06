"""Debug chain of actors."""
from src.VMActor import VMActor

actor1 = VMActor()
actor2 = VMActor()
actor3 = VMActor()

# Set up chain: actor1 -> actor2 -> actor3
def forward_plus_10(vm):
    """Custom instruction: add 10 and forward to next actor."""
    print(f"  forward_plus_10 called, stack before: {list(vm.stack)}")
    value = vm.stack.pop()
    result = value + 10
    vm.stack.push(result)
    print(f"  Computed: {value} + 10 = {result}")
    # Forward to next actor
    next_actor = vm.variables.get('next')
    if next_actor:
        print(f"  Forwarding {result} to next actor")
        vm.send_to(next_actor, "OP_CONSTANT", result)
    else:
        print(f"  No next actor, chain ends")

actor1.define_new_instruction("OP_FORWARD", forward_plus_10)
actor2.define_new_instruction("OP_FORWARD", forward_plus_10)
actor3.define_new_instruction("OP_FORWARD", forward_plus_10)

# Set up chain references
actor1.variables['next'] = actor2
actor2.variables['next'] = actor3
actor3.variables['next'] = None

# Start the chain
print("Starting chain with value 5")
actor1.send("OP_CONSTANT", 5, "OP_FORWARD")

# Process actor1
print("\nProcessing actor1:")
count = 0
while actor1.handle_message():
    count += 1
    print(f"  Message #{count}")
    if count > 10:
        print("  Breaking!")
        break
print(f"actor1.top() = {actor1.top()}")

# Process actor2
print("\nProcessing actor2:")
print(f"actor2.bytecode before: {actor2.bytecode}")
count = 0
while actor2.handle_message():
    count += 1
    print(f"  Message #{count}")
    if count > 10:
        print("  Breaking!")
        break
print(f"actor2.top() = {actor2.top()}")

# Process actor3
print("\nProcessing actor3:")
print(f"actor3.bytecode before: {actor3.bytecode}")
count = 0
while actor3.handle_message():
    count += 1
    print(f"  Message #{count}")
    if count > 10:
        print("  Breaking!")
        break
print(f"actor3.top() = {actor3.top()}")

print("\nFinal results:")
print(f"actor1: {actor1.top()}")
print(f"actor2: {actor2.top()}")
print(f"actor3: {actor3.top()}")
