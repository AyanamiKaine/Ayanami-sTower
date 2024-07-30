(ql:quickload :cffi)
(defpackage :stella-messaging
    (:use :cl :cffi) 
    (:export #:create-request-socket
            #:socket-connect
            #:socket-send-string-message
            #:socket-close
            #:send-hello-message))

(in-package :stella-api)

(cffi:define-foreign-library stella-messaging-lib
  (:windows "stella_messaging.dll"))

(cffi:use-foreign-library stella-messaging-lib)

;; Define the nng_socket Struct
(cffi:defcstruct nng-socket 
  (id :uint32))

;; Define a pointer to the nng_socket struct
(defctype nng-socket-ptr :pointer) 

;; Define API Functions
(cffi:defcfun ("create_push_socket" create-push-socket) nng-socket) ;; For Clients
(cffi:defcfun ("create_pull_socket" create-pull-socket) nng-socket) ;; For Servers

(cffi:defcfun ("socket_connect" socket-connect) :void
  (sock nng-socket)
  (address :string))

(cffi:defcfun ("socket_bind" socket-bind) :void
  (sock nng-socket)
  (address :string))

(cffi:defcfun ("socket_close" socket-close) :void
  (sock nng-socket))

(cffi:defcfun ("socket_send_string_message" socket-send-string-message) :void
  (sock nng-socket)
  (message :string))

(cffi:defcfun ("socket_receive_string_message" socket-receive-string-message) :string
  (sock nng-socket))

(cffi:defcfun ("free_received_message" free-received-message) :void
  (message :string)) 



;;(defun send-hello-message ()
;;
;;  (let((sock1 (create-push-socket)))
;;      (socket-connect sock1 "ipc:///hello_world")  
;;      (socket-send-string-message sock1 "Hello from Common Lisp!")
;;      (print (socket-receive-string-message sock1))
;;        
;;    (socket-close sock1) ; Close the socket after sending
;;    t)) ; Indicate success by returning T