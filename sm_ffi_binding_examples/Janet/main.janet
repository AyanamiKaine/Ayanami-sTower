(ffi/context  "stella_messaging")

(ffi/defbind create-pull-socket :ptr []) # For Servers
(ffi/defbind create-push-socket :ptr []) # For Clients
(ffi/defbind socket-connect :void [socket :ptr address :string])
(ffi/defbind socket-bind :void [socket :ptr address :string])
(ffi/defbind socket-close :void [socket :ptr])

(ffi/defbind socket-send-string-message :void [socket :ptr message :string])
(ffi/defbind socket-receive-string-message :string [socket :ptr])

(ffi/defbind free-received-message :void [message :string])

(def sock (create-push-socket))
(socket-connect sock "ipc:///hello_world")


(def message "Hello from Janet!")
(socket-send-string-message sock message)
