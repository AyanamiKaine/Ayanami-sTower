On windows draging the window blocks the thread as long as the event is happening, this means when you check for events in the same thread as the rendering it will block as long as the user holds the window. Often times this is not necessary, to circumvent this we must run the event handling in another thread using callbacks.

This problem was fixed in SDL 2.3 see: "https://github.com/libsdl-org/SDL/issues/1059"
