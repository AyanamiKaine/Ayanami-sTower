"""Debug pipeline."""
from src.VMActor import VMActor

source = VMActor()
doubler = VMActor()
squarer = VMActor()
sink = VMActor()

# Doubler: receives value, doubles it, sends to squarer
def double_and_forward(vm):
    value = vm.stack.pop()
    doubled = value * 2
    vm.stack.push(doubled)
    print(f"Doubler: {value} * 2 = {doubled}")
    next_actor = vm.variables['next']
    vm.send_to(next_actor, "OP_CONSTANT", doubled, "OP_PROCESS")

doubler.define_new_instruction("OP_PROCESS", double_and_forward)
doubler.variables['next'] = squarer

# Squarer: receives value, squares it, sends to sink
def square_and_forward(vm):
    value = vm.stack.pop()
    squared = value * value
    vm.stack.push(squared)
    print(f"Squarer: {value} * {value} = {squared}")
    next_actor = vm.variables['next']
    vm.send_to(next_actor, "OP_CONSTANT", squared)

squarer.define_new_instruction("OP_PROCESS", square_and_forward)
squarer.variables['next'] = sink

# Source sends 5 into pipeline
source.send_to(doubler, "OP_CONSTANT", 5, "OP_PROCESS")

# Process pipeline
print("Processing doubler:")
print(f"  bytecode: {doubler.bytecode}")
while doubler.handle_message():
    pass
print(f"  result: {doubler.top()}")

print("\nProcessing squarer:")
print(f"  bytecode: {squarer.bytecode}")
while squarer.handle_message():
    pass
print(f"  result: {squarer.top()}")

print("\nProcessing sink:")
print(f"  bytecode: {sink.bytecode}")
while sink.handle_message():
    pass
print(f"  result: {sink.top()}")
