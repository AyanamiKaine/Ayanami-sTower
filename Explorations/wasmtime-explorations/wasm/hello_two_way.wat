(module
  ;; --- Memory ---
  (memory (export "memory") 1)

  ;; --- Data Segments (Moved to top level) ---
  ;; Data segments must be defined at the module level. They initialize
  ;; memory when the module is first instantiated.
  (data (i32.const 0) "Hello, ")          ;; Prefix, 7 bytes long, at address 0
  (data (i32.const 20) "! Welcome to C#.") ;; Suffix, 16 bytes long, at address 20

  ;; --- A simple bump allocator ---
  (global $heap_ptr (mut i32) (i32.const 1024))

  ;; `allocate` function
  ;; We add the identifier '$allocate' here so it can be called by name.
  (func $allocate (export "allocate") (param $size i32) (result i32)
    (global.get $heap_ptr)
    (global.set $heap_ptr
      (i32.add
        (global.get $heap_ptr)
        (local.get $size)
      )
    )
  )

  ;; `deallocate` function
  (func (export "deallocate") (param $ptr i32) (param $size i32)
    ;; This is a no-op in our simple allocator.
  )

  ;; --- The main logic function ---
  (func (export "format_greeting") (param $name_ptr i32) (param $name_len i32) (result i32 i32)
    (local $result_ptr i32)
    
    ;; --- 1. Calculate the size of the new string and allocate memory for it ---
    (i32.const 24) ;; 7 (prefix) + 16 (suffix)
    local.get $name_len
    i32.add
    call $allocate
    local.set $result_ptr

    ;; --- 2. Copy the parts into the newly allocated memory block ---
    ;; a) Copy the prefix "Hello, "
    (memory.copy
      (local.get $result_ptr)      ;; Destination
      (i32.const 0)                ;; Source (from our data segment at the top)
      (i32.const 7)                ;; Size
    )

    ;; b) Copy the name provided by the host
    (memory.copy
      (i32.add (local.get $result_ptr) (i32.const 7)) ;; Destination (offset by prefix)
      (local.get $name_ptr)                           ;; Source
      (local.get $name_len)                           ;; Size
    )

    ;; c) Copy the suffix "! Welcome to C#."
    (memory.copy
      (i32.add (i32.add (local.get $result_ptr) (i32.const 7)) (local.get $name_len)) ;; Destination (offset by prefix and name)
      (i32.const 20)               ;; Source (from our data segment at the top)
      (i32.const 16)               ;; Size
    )

    ;; --- 3. Return the pointer and total length of the new string ---
    local.get $result_ptr
    
    (i32.add (i32.add (i32.const 7) (local.get $name_len)) (i32.const 16))
  )
)
