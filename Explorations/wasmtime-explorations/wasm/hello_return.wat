(module
  ;; 1. Define a block of memory, 1 page (64KiB) in size.
  ;; We also export it with the name "memory" so the C# host can access it.
  (memory (export "memory") 1)

  ;; 2. Use a data segment to write our string into memory at a specific address
  ;; when the module is loaded. We'll choose address 8 for this example.
  (data (i32.const 8) "Hello from WASM!")

  ;; 3. Define the function that C# will call.
  ;; It takes no parameters and returns two 32-bit integers (result i32 i32).
  ;; These will be the pointer and the length.
  (func $get_greeting (result i32 i32)
    ;; Push the pointer to the string (address 8) onto the stack.
    i32.const 8
    
    ;; Push the length of the string ("Hello from WASM!" is 16 characters) onto the stack.
    i32.const 16
    
    ;; The function returns the top two values from the stack.
  )

  ;; 4. Export the function so C# can call it.
  (export "get_greeting" (func $get_greeting))
)
