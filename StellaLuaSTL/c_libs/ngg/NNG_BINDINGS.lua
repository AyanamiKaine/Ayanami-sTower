local ffi = require("ffi")

-- For NNG Version 1.8.0
local NNG_BINDINGS = {}

ffi.cdef[[
    typedef void (*CALL_BACK)(void*);   // Define the function pointer type


    typedef struct nng_socket_s { uint32_t id; } nng_socket;
    typedef struct nng_ctx_s { uint32_t id; } nng_ctx;

    typedef struct nng_dialer_s { uint32_t id; } nng_dialer;

    typedef struct nng_listener_s { uint32_t id; } nng_listener;

    typedef struct nng_pipe_s { uint32_t id; } nng_pipe;

    typedef int32_t         nng_duration; // in milliseconds
    typedef struct nng_msg  nng_msg;
    typedef struct nng_stat nng_stat;
    typedef struct nng_aio  nng_aio;
  
    struct nng_sockaddr_inproc {
        uint16_t sa_family;
        char     sa_name[128];
    };

    struct nng_sockaddr_path {
        uint16_t sa_family;
        char     sa_path[128];
    };

    struct nng_sockaddr_in6 {
        uint16_t sa_family;
        uint16_t sa_port;
        uint8_t  sa_addr[16];
        uint32_t sa_scope;
    };
    struct nng_sockaddr_in {
        uint16_t sa_family;
        uint16_t sa_port;
        uint32_t sa_addr;
    };

    struct nng_sockaddr_zt {
        uint16_t sa_family;
        uint64_t sa_nwid;
        uint64_t sa_nodeid;
        uint32_t sa_port;
    };

    struct nng_sockaddr_abstract {
        uint16_t sa_family;
        uint16_t sa_len;       // will be 0 - 107 max.
        uint8_t  sa_name[107]; // 108 linux/windows, without leading NUL
    };

    // nng_sockaddr_storage is the the size required to store any nng_sockaddr.
    // This size must not change, and no individual nng_sockaddr type may grow
    // larger than this without breaking binary compatibility.
    struct nng_sockaddr_storage {
        uint16_t sa_family;
        uint64_t sa_pad[16];
    };

    typedef struct nng_sockaddr_inproc   nng_sockaddr_inproc;
    typedef struct nng_sockaddr_path     nng_sockaddr_path;
    typedef struct nng_sockaddr_path     nng_sockaddr_ipc;
    typedef struct nng_sockaddr_in       nng_sockaddr_in;
    typedef struct nng_sockaddr_in6      nng_sockaddr_in6;
    typedef struct nng_sockaddr_zt       nng_sockaddr_zt;
    typedef struct nng_sockaddr_abstract nng_sockaddr_abstract;
    typedef struct nng_sockaddr_storage  nng_sockaddr_storage;

    typedef union nng_sockaddr {
        uint16_t              s_family;
        nng_sockaddr_ipc      s_ipc;
        nng_sockaddr_inproc   s_inproc;
        nng_sockaddr_in6      s_in6;
        nng_sockaddr_in       s_in;
        nng_sockaddr_zt       s_zt;
        nng_sockaddr_abstract s_abstract;
        nng_sockaddr_storage  s_storage;
    } nng_sockaddr;

    enum nng_sockaddr_family {
        NNG_AF_UNSPEC   = 0,
        NNG_AF_INPROC   = 1,
        NNG_AF_IPC      = 2,
        NNG_AF_INET     = 3,
        NNG_AF_INET6    = 4,
        NNG_AF_ZT       = 5, // ZeroTier
        NNG_AF_ABSTRACT = 6
    };

    // Scatter/gather I/O.
    typedef struct nng_iov {
        void * iov_buf;
        size_t iov_len;
    } nng_iov;
  
  
    int nng_pair1_open(nng_socket *);
    int nng_bus0_open(nng_socket *);
    
    int nng_rep0_open(nng_socket *);
    int nng_req0_open(nng_socket *);
    
    int nng_sub0_open(nng_socket *);
    int nng_pub0_open(nng_socket *);
    
    int nng_pull0_open(nng_socket *);
    int nng_push0_open(nng_socket *);
    
    int nng_respondent0_open(nng_socket *);
    int nng_surveyor0_open(nng_socket *);

    int nng_dial(nng_socket, const char *, nng_dialer *, int);
    int nng_listen(nng_socket, const char *, nng_listener *, int);

    int nng_close(nng_socket);    
    const char *nng_strerror(int);  

  
    int      nng_msg_alloc(nng_msg **, size_t);
    void     nng_msg_free(nng_msg *);
    int      nng_msg_realloc(nng_msg *, size_t);
    int      nng_msg_reserve(nng_msg *, size_t);
    size_t   nng_msg_capacity(nng_msg *);
    void *   nng_msg_header(nng_msg *);
    size_t   nng_msg_header_len(const nng_msg *);
    void *   nng_msg_body(nng_msg *);
    size_t   nng_msg_len(const nng_msg *);
    int      nng_msg_append(nng_msg *, const void *, size_t);
    int      nng_msg_insert(nng_msg *, const void *, size_t);
    int      nng_msg_trim(nng_msg *, size_t);
    int      nng_msg_chop(nng_msg *, size_t);
    int      nng_msg_header_append(nng_msg *, const void *, size_t);
    int      nng_msg_header_insert(nng_msg *, const void *, size_t);
    int      nng_msg_header_trim(nng_msg *, size_t);
    int      nng_msg_header_chop(nng_msg *, size_t);
    int      nng_msg_header_append_u16(nng_msg *, uint16_t);
    int      nng_msg_header_append_u32(nng_msg *, uint32_t);
    int      nng_msg_header_append_u64(nng_msg *, uint64_t);
    int      nng_msg_header_insert_u16(nng_msg *, uint16_t);
    int      nng_msg_header_insert_u32(nng_msg *, uint32_t);
    int      nng_msg_header_insert_u64(nng_msg *, uint64_t);
    int      nng_msg_header_chop_u16(nng_msg *, uint16_t *);
    int      nng_msg_header_chop_u32(nng_msg *, uint32_t *);
    int      nng_msg_header_chop_u64(nng_msg *, uint64_t *);
    int      nng_msg_header_trim_u16(nng_msg *, uint16_t *);
    int      nng_msg_header_trim_u32(nng_msg *, uint32_t *);
    int      nng_msg_header_trim_u64(nng_msg *, uint64_t *);
    int      nng_msg_append_u16(nng_msg *, uint16_t);
    int      nng_msg_append_u32(nng_msg *, uint32_t);
    int      nng_msg_append_u64(nng_msg *, uint64_t);
    int      nng_msg_insert_u16(nng_msg *, uint16_t);
    int      nng_msg_insert_u32(nng_msg *, uint32_t);
    int      nng_msg_insert_u64(nng_msg *, uint64_t);
    int      nng_msg_chop_u16(nng_msg *, uint16_t *);
    int      nng_msg_chop_u32(nng_msg *, uint32_t *);
    int      nng_msg_chop_u64(nng_msg *, uint64_t *);
    int      nng_msg_trim_u16(nng_msg *, uint16_t *);
    int      nng_msg_trim_u32(nng_msg *, uint32_t *);
    int      nng_msg_trim_u64(nng_msg *, uint64_t *);
    int      nng_msg_dup(nng_msg **, const nng_msg *);
    void     nng_msg_clear(nng_msg *);
    void     nng_msg_header_clear(nng_msg *);
    void     nng_msg_set_pipe(nng_msg *, nng_pipe);
    nng_pipe nng_msg_get_pipe(const nng_msg *);    
    // nng_strerror returns a human readable string associated with the error
    // code supplied.
    const char *nng_strerror(int);

    // nng_send sends (or arranges to send) the data on the socket.  Note that
    // this function may (will!) return before any receiver has actually
    // received the data.  The return value will be zero to indicate that the
    // socket has accepted the entire data for send, or an errno to indicate
    // failure.  The flags may include NNG_FLAG_NONBLOCK or NNG_FLAG_ALLOC.
    // If the flag includes NNG_FLAG_ALLOC, then the function will call
    // nng_free() on the supplied pointer & size on success. (If the call
    // fails then the memory is not freed.)
    int nng_send(nng_socket, void *, size_t, int);

    // nng_recv receives message data into the socket, up to the supplied size.
    // The actual size of the message data will be written to the value pointed
    // to by size.  The flags may include NNG_FLAG_NONBLOCK and NNG_FLAG_ALLOC.
    // If NNG_FLAG_ALLOC is supplied then the library will allocate memory for
    // the caller.  In that case the pointer to the allocated will be stored
    // instead of the data itself.  The caller is responsible for freeing the
    // associated memory with nng_free().
    int nng_recv(nng_socket, void *, size_t *, int);

    // nng_sendmsg is like nng_send, but offers up a message structure, which
    // gives the ability to provide more control over the message, including
    // providing backtrace information.  It also can take a message that was
    // obtain via nn_recvmsg, allowing for zero copy forwarding.
    int nng_sendmsg(nng_socket, nng_msg *, int);

    // nng_recvmsg is like nng_recv, but is used to obtain a message structure
    // as well as the data buffer.  This can be used to obtain more information
    // about where the message came from, access raw headers, etc.  It also
    // can be passed off directly to nng_sendmsg.
    int nng_recvmsg(nng_socket, nng_msg **, int);

    // nng_send_aio sends data on the socket asynchronously.  As with nng_send,
    // the completion may be executed before the data has actually been delivered,
    // but only when it is accepted for delivery.  The supplied AIO must have
    // been initialized, and have an associated message.  The message will be
    // "owned" by the socket if the operation completes successfully.  Otherwise
    // the caller is responsible for freeing it.
    void nng_send_aio(nng_socket, nng_aio *);

    // nng_recv_aio receives data on the socket asynchronously.  On a successful
    // result, the AIO will have an associated message, that can be obtained
    // with nng_aio_get_msg().  The caller takes ownership of the message at
    // this point.
    void nng_recv_aio(nng_socket, nng_aio *);

    // Context support.  User contexts are not supported by all protocols,
    // but for those that do, they give a way to create multiple contexts
    // on a single socket, each of which runs the protocol's state machinery
    // independently, offering a way to achieve concurrent protocol support
    // without resorting to raw mode sockets.  See the protocol specific
    // documentation for further details.  (Note that at this time, only
    // asynchronous send/recv are supported for contexts, but its easy enough
    // to make synchronous versions with nng_aio_wait().)  Note that nng_close
    // of the parent socket will *block* as long as any contexts are open.

    // nng_ctx_open creates a context.  This returns NNG_ENOTSUP if the
    // protocol implementation does not support separate contexts.
    int nng_ctx_open(nng_ctx *, nng_socket);

    // nng_ctx_close closes the context.
    int nng_ctx_close(nng_ctx);

    // nng_ctx_id returns the numeric id for the context; this will be
    // a positive value for a valid context, or < 0 for an invalid context.
    // A valid context is not necessarily an *open* context.
    int nng_ctx_id(nng_ctx);

    // nng_ctx_recv receives asynchronously.  It works like nng_recv_aio, but
    // uses a local context instead of the socket global context.
    void nng_ctx_recv(nng_ctx, nng_aio *);

    // nng_ctx_recvmsg is allows for receiving a message synchronously using
    // a context.  It has the same semantics as nng_recvmsg, but operates
    // on a context instead of a socket.
    int nng_ctx_recvmsg(nng_ctx, nng_msg **, int);

    // nng_ctx_send sends asynchronously. It works like nng_send_aio, but
    // uses a local context instead of the socket global context.
    void nng_ctx_send(nng_ctx, nng_aio *);

    // nng_ctx_sendmsg is allows for sending a message synchronously using
    // a context.  It has the same semantics as nng_sendmsg, but operates
    // on a context instead of a socket.
    int nng_ctx_sendmsg(nng_ctx, nng_msg *, int);

    int nng_ctx_get(nng_ctx, const char *, void *, size_t *);
    int nng_ctx_get_bool(nng_ctx, const char *, bool *);
    int nng_ctx_get_int(nng_ctx, const char *, int *);
    int nng_ctx_get_size(nng_ctx, const char *, size_t *);
    int nng_ctx_get_uint64(nng_ctx, const char *, uint64_t *);
    int nng_ctx_get_string(nng_ctx, const char *, char **);
    int nng_ctx_get_ptr(nng_ctx, const char *, void **);
    int nng_ctx_get_ms(nng_ctx, const char *, nng_duration *);
    int nng_ctx_get_addr(nng_ctx, const char *, nng_sockaddr *);

    int nng_ctx_set(nng_ctx, const char *, const void *, size_t);
    int nng_ctx_set_bool(nng_ctx, const char *, bool);
    int nng_ctx_set_int(nng_ctx, const char *, int);
    int nng_ctx_set_size(nng_ctx, const char *, size_t);
    int nng_ctx_set_uint64(nng_ctx, const char *, uint64_t);
    int nng_ctx_set_string(nng_ctx, const char *, const char *);
    int nng_ctx_set_ptr(nng_ctx, const char *, void *);
    int nng_ctx_set_ms(nng_ctx, const char *, nng_duration);
    int nng_ctx_set_addr(nng_ctx, const char *, const nng_sockaddr *);

    // nng_alloc is used to allocate memory.  It's intended purpose is for
    // allocating memory suitable for message buffers with nng_send().
    // Applications that need memory for other purposes should use their platform
    // specific API.
    void *nng_alloc(size_t);

    // nng_free is used to free memory allocated with nng_alloc, which includes
    // memory allocated by nng_recv() when the NNG_FLAG_ALLOC message is supplied.
    // As the application is required to keep track of the size of memory, this
    // is probably less convenient for general uses than the C library malloc and
    // calloc.
    void nng_free(void *, size_t);

    // nng_strdup duplicates the source string, using nng_alloc. The result
    // should be freed with nng_strfree (or nng_free(strlen(s)+1)).
    char *nng_strdup(const char *);

    // nng_strfree is equivalent to nng_free(strlen(s)+1).
    void nng_strfree(char *);

    // Async IO API.  AIO structures can be thought of as "handles" to
    // support asynchronous operations.  They contain the completion callback, and
    // a pointer to consumer data.  This is similar to how overlapped I/O
    // works in Windows, when used with a completion callback.
    //
    // AIO structures can carry up to 4 distinct input values, and up to
    // 4 distinct output values, and up to 4 distinct "private state" values.
    // The meaning of the inputs and the outputs are determined by the
    // I/O functions being called.

    // nng_aio_alloc allocates a new AIO, and associated the completion
    // callback and its opaque argument.  If NULL is supplied for the
    // callback, then the caller must use nng_aio_wait() to wait for the
    // operation to complete.  If the completion callback is not NULL, then
    // when a submitted operation completes (or is canceled or fails) the
    // callback will be executed, generally in a different thread, with no
    // locks held.
    int nng_aio_alloc(nng_aio **, void (*)(void *), void *);

    // nng_aio_free frees the AIO and any associated resources.
    // It *must not* be in use at the time it is freed.
    void nng_aio_free(nng_aio *);

    // nng_aio_reap is like nng_aio_free, but calls it from a background
    // reaper thread.  This can be useful to free aio objects from aio
    // callbacks (e.g. when the result of the callback is to discard
    // the object in question.)  The aio object must be in further use
    // when this is called.
    void nng_aio_reap(nng_aio *);

    // nng_aio_stop stops any outstanding operation, and waits for the
    // AIO to be free, including for the callback to have completed
    // execution.  Therefore the caller must NOT hold any locks that
    // are acquired in the callback, or deadlock will occur.
    void nng_aio_stop(nng_aio *);

    // nng_aio_result returns the status/result of the operation. This
    // will be zero on successful completion, or an nng error code on
    // failure.
    int nng_aio_result(nng_aio *);

    // nng_aio_count returns the number of bytes transferred for certain
    // I/O operations.  This is meaningless for other operations (e.g.
    // DNS lookups or TCP connection setup).
    size_t nng_aio_count(nng_aio *);

    // nng_aio_cancel attempts to cancel any in-progress I/O operation.
    // The AIO callback will still be executed, but if the cancellation is
    // successful then the status will be NNG_ECANCELED.
    void nng_aio_cancel(nng_aio *);

    // nng_aio_abort is like nng_aio_cancel, but allows for a different
    // error result to be returned.
    void nng_aio_abort(nng_aio *, int);

    // nng_aio_wait waits synchronously for any pending operation to complete.
    // It also waits for the callback to have completed execution.  Therefore,
    // the caller of this function must not hold any locks acquired by the
    // callback or deadlock may occur.
    void nng_aio_wait(nng_aio *);

    // nng_aio_busy returns true if the aio is still busy processing the
    // operation, or executing associated completion functions.  Note that
    // if the completion function schedules a new operation using the aio,
    // then this function will continue to return true.
    bool nng_aio_busy(nng_aio *);

    // nng_aio_set_msg sets the message structure to use for asynchronous
    // message send operations.
    void nng_aio_set_msg(nng_aio *, nng_msg *);

    // nng_aio_get_msg returns the message structure associated with a completed
    // receive operation.
    nng_msg *nng_aio_get_msg(nng_aio *);

    // nng_aio_set_input sets an input parameter at the given index.
    int nng_aio_set_input(nng_aio *, unsigned, void *);

    // nng_aio_get_input retrieves the input parameter at the given index.
    void *nng_aio_get_input(nng_aio *, unsigned);

    // nng_aio_set_output sets an output result at the given index.
    int nng_aio_set_output(nng_aio *, unsigned, void *);

    // nng_aio_get_output retrieves the output result at the given index.
    void *nng_aio_get_output(nng_aio *, unsigned);

    // nng_aio_set_timeout sets a timeout on the AIO.  This should be called for
    // operations that should time out after a period.  The timeout should be
    // either a positive number of milliseconds, or NNG_DURATION_INFINITE to
    // indicate that the operation has no timeout.  A poll may be done by
    // specifying NNG_DURATION_ZERO.  The value NNG_DURATION_DEFAULT indicates
    // that any socket specific timeout should be used.
    void nng_aio_set_timeout(nng_aio *, nng_duration);

    // nng_aio_set_iov sets a scatter/gather vector on the aio.  The iov array
    // itself is copied. Data members (the memory regions referenced) *may* be
    // copied as well, depending on the operation.  This operation is guaranteed
    // to succeed if n <= 4, otherwise it may fail due to NNG_ENOMEM.
    int nng_aio_set_iov(nng_aio *, unsigned, const nng_iov *);

    // nng_aio_begin is called by the provider to mark the operation as
    // beginning.  If it returns false, then the provider must take no
    // further action on the aio.
    bool nng_aio_begin(nng_aio *);

    // nng_aio_finish is used to "finish" an asynchronous operation.
    // It should only be called by "providers" (such as HTTP server API users).
    // The argument is the value that nng_aio_result() should return.
    // IMPORTANT: Callers must ensure that this is called EXACTLY ONCE on any
    // given aio.
    void nng_aio_finish(nng_aio *, int);

    // nng_aio_defer is used to register a cancellation routine, and indicate
    // that the operation will be completed asynchronously.  It must only be
    // called once per operation on an aio, and must only be called by providers.
    // If the operation is canceled by the consumer, the cancellation callback
    // will be called.  The provider *must* still ensure that the nng_aio_finish()
    // function is called EXACTLY ONCE.  If the operation cannot be canceled
    // for any reason, the cancellation callback should do nothing.  The
    // final argument is passed to the cancelfn.  The final argument of the
    // cancellation function is the error number (will not be zero) corresponding
    // to the reason for cancellation, e.g. NNG_ETIMEDOUT or NNG_ECANCELED.
    typedef void (*nng_aio_cancelfn)(nng_aio *, void *, int);
    void nng_aio_defer(nng_aio *, nng_aio_cancelfn, void *);

    // nng_aio_sleep does a "sleeping" operation, basically does nothing
    // but wait for the specified number of milliseconds to expire, then
    // calls the callback.  This returns 0, rather than NNG_ETIMEDOUT.
    void nng_sleep_aio(nng_duration, nng_aio *);

  
    int nng_socket_set(nng_socket, const char *, const void *, size_t);
    int nng_socket_set_bool(nng_socket, const char *, bool);
    int nng_socket_set_int(nng_socket, const char *, int);
    int nng_socket_set_size(nng_socket, const char *, size_t);
    int nng_socket_set_uint64(nng_socket, const char *, uint64_t);
    int nng_socket_set_string(nng_socket, const char *, const char *);
    int nng_socket_set_ptr(nng_socket, const char *, void *);
    int nng_socket_set_ms(nng_socket, const char *, nng_duration);
    int nng_socket_set_addr(
        nng_socket, const char *, const nng_sockaddr *);

    int nng_socket_get(nng_socket, const char *, void *, size_t *);
    int nng_socket_get_bool(nng_socket, const char *, bool *);
    int nng_socket_get_int(nng_socket, const char *, int *);
    int nng_socket_get_size(nng_socket, const char *, size_t *);
    int nng_socket_get_uint64(nng_socket, const char *, uint64_t *);
    int nng_socket_get_string(nng_socket, const char *, char **);
    int nng_socket_get_ptr(nng_socket, const char *, void **);
    int nng_socket_get_ms(nng_socket, const char *, nng_duration *);
    int nng_socket_get_addr(nng_socket, const char *, nng_sockaddr *);
    void nng_msleep(nng_duration msec);
]]

NNG_BINDINGS = ffi.load(
    ffi.os == "Windows"  and ".\\nng.dll"
    or ffi.os == "Linux" and "libnng.so" -- Linux path
    or ffi.os == "OSX"   and "libnng.dylib" -- macOS path
)

return NNG_BINDINGS