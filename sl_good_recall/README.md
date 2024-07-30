# Stella Spaced Repetition Algorithm Good Recall Component

- Based on Response/Request Pattern, every request done to the server will expect that the client wants to receive a message.
  - See StellaSockets Request and Response Sockets

The Component will validate incoming json requests as well as outgoing json responses.

Under no circumstances should the server encounter a null json object.

## API

The API is heavily inspired by elixir/erlang

### Expected Json Input as a string

Input send to the server is expected as a string formated json with two fields called `EaseFactor` and `NumberOfTimeSeen`

```json
{
	"EaseFactor": 2.5,
	"NumberOfTimeSeen": 5
}
```

### Expected Json Output as a String

As the type of output of the server can either be `<string, DateTime>` or `<string, string>` you must handle this case in client code.

#### Ok

The server anwser with a json string with one field called `ok`, its value is the new calculated DueDate with the type of `DateTime`

```json
{
	"ok": "2024-08-03T16:09:04.1464354+02:00"
}
```

#### Error

Should the server encounter an error it will send instead a json string with one field called `error`, its value is the error message as a string.

```json
{
	"error": "Json was not valid, couldnt correctly be deserialized"
}
```

```json
{
	"error": "Created Json Response is not valid"
}
```
