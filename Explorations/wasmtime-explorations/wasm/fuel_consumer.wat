(module
  (func (export "run")
    (loop
      (i32.const 1)
      (i32.add)
      (br_if 0 (i32.const 1))
    )
  )
)