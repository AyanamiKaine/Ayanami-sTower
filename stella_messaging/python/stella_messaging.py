import datetime
import pynng
import curio
import json

# Define a JSON-RPC method name for the date request
GET_DATE = "get_date"

address = "ipc:///tmp/reqrep.ipc"
import datetime
import json

class StellaClient:
    def __init__(self):
        self.methods = {}  # Dictionary to store registered methods
        self.available_methods = {"get_date"}  # Store available methods separately
        self.methods["get_date"] = self.get_date


    def add_method(self, name, func):
        self.methods[name] = func

    def get_date(self):
        return datetime.datetime.now()

    def handle_jsonrpc_request(self, request_bytes):
        try:
            request = json.loads(request_bytes.decode())
        except json.JSONDecodeError:
            return self._create_error_response(-32700, "Parse error", None)

        match request:
            case {"jsonrpc": "2.0", "method": method, "id": id}:
                match method:  
                    case 'get_date':
                        result = self.methods[method]()  # Call the registered function
                        return self._create_success_response(result, id) 
                    case _:
                        return self._create_error_response(-32601, "Method not found", id)
            case _:
                return self._create_error_response(-32600, "Invalid Request", None)

    def _create_success_response(self, result, id):
        return json.dumps({"jsonrpc": "2.0", "result": result, "id": id}, default=str).encode()

    def _create_error_response(self, code, message, id):
        return json.dumps({"jsonrpc": "2.0", "error": {"code": code, "message": message}, "id": id}, default=str).encode()
    
async def node0(sock):
    async def response_eternally():
        while True:
            msg = await sock.arecv_msg()
            response = StellaClient().handle_jsonrpc_request(msg.bytes)
            await sock.asend(response)

    sock.listen(address)
    return await curio.spawn(response_eternally)

async def node1():
    with pynng.Req0() as sock:
        sock.dial(address)
        request = {"jsonrpc": "2.0", "method": 'get_date', "id": 1}
        print(f"NODE1: SENDING DATE REQUEST: {request}")
        await sock.asend(json.dumps(request).encode())
        msg = await sock.arecv_msg()
        response = json.loads(msg.bytes.decode())
        print(f"NODE1: RECEIVED RESPONSE: {response}")

        request = {"jsonrpc": "2.0", "method": 'get_name', "id": 1}
        print(f"NODE1: SENDING DATE REQUEST: {request}")
        await sock.asend(json.dumps(request).encode())
        msg = await sock.arecv_msg()
        response = json.loads(msg.bytes.decode())
        print(f"NODE1: RECEIVED RESPONSE: {response}")

async def main():
    with pynng.Rep0() as rep:
        client = StellaClient()
        n0 = await node0(rep)
        await curio.sleep(1)
        await node1()
        await n0.cancel()

if __name__ == "__main__":
    try:
        curio.run(main)
    except KeyboardInterrupt:
        pass