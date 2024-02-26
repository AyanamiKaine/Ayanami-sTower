genrule(
    name = "combine",
    srcs = ["//:files/file01.txt", "//:files/file02.txt"],
    outs  = ["combined.txt"],
    cmd = "cat $(location //:files/file01.txt) $(location //:files/file02.txt) > $(location combined.txt)"
)
