from src.VMActor import VMActor

vm = VMActor()

def test(v):
    print('Test function called!')
    
vm.defun('test', test)
bc = vm.s_expression_to_bytecode('(test)')
print(f'Bytecode: {bc}')
vm.send(*bc)
result = vm.handle_message()
print(f'Handle message result: {result}')
