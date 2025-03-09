# Possible Features

## [] Cloud Saves

I use the app on windows and linux it would be quite nice if we could painlessly sync them. Having a small server that does that is quite trival. Simply associate an ID with a filepath to the json save file, send that to the client and we are all good. To update it simply send the file to the server that then replaces it. We could even easily create a backup system.

Keep it simply and just create a key value store. Where the key is the USER_ID and the value the path to the json file. Read the file and send it to the client.
