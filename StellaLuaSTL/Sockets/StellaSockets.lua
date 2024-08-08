local NNG_BINDINGS = require "./c_libs/ngg/NNG_BINDINGS"
local ffi = require "ffi"

-- By Default every send and receive of a socket is non blocking
-- They return nil if you have to do send or receive again.
-- This works at best in using coroutines.
local StellaSockets = {
    Push              = {},
    Pull              = {},
    Subscriber        = {},
    Publisher         = {},
    Request           = {},
    Response          = {},
    NNG_FLAG_NONBLOCK = 2,
    NNG_FLAG_BLOCK    = 0,
}

local StellaMessages = {}

function StellaMessages.create_message_from_string(string_message)
    local message = StellaMessages.create_empty_msg()
    NNG_BINDINGS.nng_msg_append(message[0], string_message, #string_message + 1) -- Copy data into the message body
    return message
end

--Creates an empty but allocated msg* object
function StellaMessages.create_empty_msg()
    local message = ffi.new("nng_msg*[1]")
    NNG_BINDINGS.nng_msg_alloc(message, 0)
    return message
end

-- Resets the body length of msg to zero.
function StellaMessages.clear_msg_contents(msg)
    NNG_BINDINGS.nng_msg_clear(msg)
end

-- Returns the string contents of a msg body, if its null it instead returns an empty string ""
function StellaMessages.get_string_from_message(message)
    return ffi.string(NNG_BINDINGS.nng_msg_body(message[0]))
end

function StellaMessages.trim_topic_from_message(message, topic)
    NNG_BINDINGS.nng_msg_trim(message[0], #topic+1)
end

function StellaSockets.subscribe_to_topic(socket, topic)
    NNG_BINDINGS.nng_socket_set_string(socket, "sub:subscribe", topic); 
end

function StellaSockets.unsubscribe_to_topic(socket, topic)
    NNG_BINDINGS.nng_socket_set_string(socket, "sub:unsubscribe", topic); 
end


function StellaSockets.Push.new(url, sleep_time_ms)
    local self = {}
    local current_sleep_time = sleep_time_ms or 0
    local push_socket = ffi.new("nng_socket")
    NNG_BINDINGS.nng_push0_open(push_socket);

    NNG_BINDINGS.nng_dial(push_socket, url, ffi.null, 0)

    function self:send(message)
        local push_message = StellaMessages.create_message_from_string(message)
        local rv = NNG_BINDINGS.nng_sendmsg(push_socket, push_message[0], StellaSockets["NNG_FLAG_NONBLOCK"])

        if rv ~= 0 then
            NNG_BINDINGS.nng_msg_free(push_message[0])
            NNG_BINDINGS.nng_msleep(current_sleep_time);
            return nil
        end
    end


    function self:send_blocking(message)
        local push_message = StellaMessages.create_message_from_string(message)
        local rv = NNG_BINDINGS.nng_sendmsg(push_socket, push_message[0], StellaSockets["NNG_FLAG_BLOCK"])
    end

    return self
end

function StellaSockets.Pull.new(url, sleep_time_ms)
    local self = {}
    local pull_socket = ffi.new("nng_socket")
    NNG_BINDINGS.nng_pull0_open(pull_socket);
    local current_sleep_time = sleep_time_ms or 0

    NNG_BINDINGS.nng_listen(pull_socket, url, ffi.null, 0)

    function self:receive()
        local pull_message = StellaMessages.create_empty_msg()
        local rv = NNG_BINDINGS.nng_recvmsg(pull_socket, pull_message, StellaSockets["NNG_FLAG_NONBLOCK"])
        
        if rv ~= 0 then
            NNG_BINDINGS.nng_msg_free(pull_message[0])
            NNG_BINDINGS.nng_msleep(current_sleep_time);
            return nil
        end
        local message = StellaMessages.get_string_from_message(pull_message)
        NNG_BINDINGS.nng_msg_free(pull_message[0])
        return message
    end

    function self:receive_blocking()
        local pull_message = StellaMessages.create_empty_msg()
        local rv = NNG_BINDINGS.nng_recvmsg(pull_socket, pull_message, StellaSockets["NNG_FLAG_BLOCK"])
        local message = StellaMessages.get_string_from_message(pull_message)
        NNG_BINDINGS.nng_msg_free(pull_message[0])
        return message

    end

    return self
end

function StellaSockets.Response.new(url, sleep_time_ms)
    local self = {}
    local current_sleep_time = sleep_time_ms or 0
    local rep_socket = ffi.new("nng_socket")
    NNG_BINDINGS.nng_rep0_open(rep_socket);
    
    local listen_rv = NNG_BINDINGS.nng_listen(rep_socket, url, ffi.null, 0)
    if listen_rv ~= 0 then
        io.stderr:write("Response Receive Returned Code: " .. ffi.string(NNG_BINDINGS.nng_strerror(listen_rv)) .. "\n")
    end

    function self:send(message)
        local req_message = StellaMessages.create_message_from_string(message)

        local rv = NNG_BINDINGS.nng_sendmsg(rep_socket, req_message[0], StellaSockets["NNG_FLAG_NONBLOCK"])
        --if rv ~= 0 then
        --    io.stderr:write("Request Receive Returned Code: " .. ffi.string(NNG_BINDINGS.nng_strerror(rv)) .. "\n")
        --end
        if rv ~= 0 then
            NNG_BINDINGS.nng_msg_free(req_message[0])
            NNG_BINDINGS.nng_msleep(current_sleep_time);
            return nil
        end
    end

    function self:receive()
        local rep_message = StellaMessages.create_empty_msg()
        local rv = NNG_BINDINGS.nng_recvmsg(rep_socket, rep_message, StellaSockets["NNG_FLAG_NONBLOCK"])
        --if rv ~= 0 then
        --    io.stderr:write("Response Receive Returned Code: " .. ffi.string(NNG_BINDINGS.nng_strerror(rv)) .. "\n")
        --end

        if rv ~= 0 then
            NNG_BINDINGS.nng_msg_free(rep_message[0])
            NNG_BINDINGS.nng_msleep(current_sleep_time);
            return nil
        end

        local message = StellaMessages.get_string_from_message(rep_message) -- Get the message
        NNG_BINDINGS.nng_msg_free(rep_message[0])
        return message
    end
    
    -- DOES NOT BLOCK
    function self:send_blocking(message)
        local req_message = StellaMessages.create_message_from_string(message)
        NNG_BINDINGS.nng_sendmsg(rep_socket, req_message[0], StellaSockets["NNG_FLAG_BLOCK"])
    end

    -- DOES NOT BLOCK
    function self:receive_blocking()
        local rep_message = StellaMessages.create_empty_msg()
        NNG_BINDINGS.nng_recvmsg(rep_socket, rep_message, StellaSockets["NNG_FLAG_BLOCK"])
        local message = StellaMessages.get_string_from_message(rep_message) -- Get the message
        NNG_BINDINGS.nng_msg_free(rep_message[0])
        return message
    end

    return self
end

function StellaSockets.Request.new(url, sleep_time_ms)
    local self = {}
    local current_sleep_time = sleep_time_ms or 0
    
    local req_socket = ffi.new("nng_socket")
    NNG_BINDINGS.nng_req0_open(req_socket);

    local dial_rv = NNG_BINDINGS.nng_dial(req_socket, url, ffi.null, 0)
    if dial_rv ~= 0 then
        io.stderr:write("Response Receive Returned Code: " .. ffi.string(NNG_BINDINGS.nng_strerror(dial_rv)) .. "\n")
    end

    -- DOES NOT BLOCK
    function self:send(message)
        local req_message = StellaMessages.create_message_from_string(message)
        local rv = NNG_BINDINGS.nng_sendmsg(req_socket, req_message[0], StellaSockets["NNG_FLAG_NONBLOCK"])
        --if rv ~= 0 then
        --    io.stderr:write("Request Receive Returned Code: " .. ffi.string(NNG_BINDINGS.nng_strerror(rv)) .. "\n")
        --end

        if rv ~= 0 then
            NNG_BINDINGS.nng_msg_free(req_message[0])
            NNG_BINDINGS.nng_msleep(current_sleep_time);
            return nil
        end
    end
    
    -- DOES NOT BLOCK
    function self:receive()
        local rep_message = StellaMessages.create_empty_msg()
        local rv = NNG_BINDINGS.nng_recvmsg(req_socket, rep_message, StellaSockets["NNG_FLAG_NONBLOCK"])

        if rv ~= 0 then
            NNG_BINDINGS.nng_msg_free(rep_message[0])
            NNG_BINDINGS.nng_msleep(current_sleep_time);
            return nil
        end

        local message = StellaMessages.get_string_from_message(rep_message) -- Get the message
        NNG_BINDINGS.nng_msg_free(rep_message[0])
        return message
    end

        -- DOES BLOCK
    function self:send_blocking(message)
        local req_message = StellaMessages.create_message_from_string(message)
        NNG_BINDINGS.nng_sendmsg(req_socket, req_message[0], StellaSockets["NNG_FLAG_BLOCK"])
    end

    -- DOES NOT BLOCK
    function self:receive_blocking()
        local rep_message = StellaMessages.create_empty_msg()
        NNG_BINDINGS.nng_recvmsg(req_socket, rep_message, StellaSockets["NNG_FLAG_BLOCK"])
        local message = StellaMessages.get_string_from_message(rep_message) -- Get the message
        NNG_BINDINGS.nng_msg_free(rep_message[0])
        return message
    end
    return self
end

function StellaSockets.Publisher.new(url, sleep_time_ms)
    local self = {}
    local current_sleep_time = sleep_time_ms
    local pub_socket = ffi.new("nng_socket[1]")
    local pub_server = NNG_BINDINGS.nng_pub0_open(pub_socket);
    NNG_BINDINGS.nng_listen(pub_socket[0], url, ffi.null, 0)

    function self:send(message, topic)
        local pub_message = StellaMessages.create_message_from_string(topic)

        local rv = NNG_BINDINGS.nng_msg_append(pub_message[0], message, #message + 1);

        NNG_BINDINGS.nng_sendmsg(pub_socket[0], pub_message[0], StellaSockets["NNG_FLAG_NONBLOCK"])
        if rv ~= 0 then
            NNG_BINDINGS.nng_msg_free(pub_message[0])
            NNG_BINDINGS.nng_msleep(current_sleep_time);
            return nil
        end
    end

    function self:send_blocking(message, topic)
        local pub_message = StellaMessages.create_message_from_string(topic)
        local rv = NNG_BINDINGS.nng_msg_append(pub_message[0], message, #message + 1);
        NNG_BINDINGS.nng_sendmsg(pub_socket[0], pub_message[0], StellaSockets["NNG_FLAG_BLOCK"])
    end

    return self
end

function StellaSockets.Subscriber.new(url, sleep_time_ms)
    local subscribed_topics = {}
    local self = {}
    local current_sleep_time = sleep_time_ms or 0
    local sub_socket = ffi.new("nng_socket")
    local sub_server = NNG_BINDINGS.nng_sub0_open(sub_socket);
    local NNG_FLAG_NONBLOCK = 2

    NNG_BINDINGS.nng_dial(sub_socket, url, ffi.null, 0)


    function self:receive()
        local sub_message = StellaMessages.create_empty_msg()
        local rv = NNG_BINDINGS.nng_recvmsg(sub_socket, sub_message, StellaSockets["NNG_FLAG_NONBLOCK"])
        if rv ~= 0 then
            NNG_BINDINGS.nng_msg_free(sub_message[0])
            NNG_BINDINGS.nng_msleep(current_sleep_time);
            return nil
        end

        local topic = StellaMessages.get_string_from_message(sub_message)
        StellaMessages.trim_topic_from_message(sub_message, topic)
        local message = StellaMessages.get_string_from_message(sub_message)
        NNG_BINDINGS.nng_msg_free(sub_message[0])

        return message
    end


    -- DOES NOT BLOCK
    function self:receive_blocking()
        local sub_message = StellaMessages.create_empty_msg()
        NNG_BINDINGS.nng_recvmsg(sub_socket, sub_message, StellaSockets["NNG_FLAG_BLOCK"])
        local topic = StellaMessages.get_string_from_message(sub_message)
        StellaMessages.trim_topic_from_message(sub_message, topic)
        local message = StellaMessages.get_string_from_message(sub_message)
        NNG_BINDINGS.nng_msg_free(sub_message[0])
        return message
    end

    function self:subscribe(topic)
        StellaSockets.subscribe_to_topic(sub_socket, topic)
    end
    
    function self:unsubscribe(topic)
        StellaSockets.unsubscribe_to_topic(sub_socket, topic)
    end

    return self
end

return StellaSockets