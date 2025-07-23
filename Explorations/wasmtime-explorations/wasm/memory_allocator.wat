(module
  (memory (export "mem") 1)
  (func (export "allocate_and_write") (param $offset i32) (param $value i32)
    (i32.store (local.get $offset) (local.get $value))
  )
)