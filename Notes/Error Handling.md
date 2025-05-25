---
Created: 2025-05-24 11:41
aliases:
  - Error Handling
  - To Crash and Die or to Recover
  - How to handle errors
tags:
  - Programming
---
"Life desires to live, no one wants to die..."
- Ayanami

To Return errors, to handle them internally/externally, to throw, to catch, to log or print. There are actually many ways to do something with errors. But what are the best ways to deal with them?

Lets think about something simple like a domain object of **Age**. Negative ages make little sense. Now how do we enforce this? We could add a conditional check every time the age value gets set. `if(newValue >= 0) value = newValue;` But what if someone tries to set the age to an invalid value like -20 what should we do?

We could use `Exceptions` to signal something exceptional has happened. But I think its not exceptional to do such a common error. We could always set the age to 0 when an invalid value is provided. But then how does the user that input the wrong value know he did something wrong? We want to disallow invalid state but still want to provide feedback that an error accord but it was handled and there is no need to panic. Living systems have shown that just dying works from small things dying first, where higher level systems try to recover and doing their best not dying. 

In Essence this means that when an error occurs in setting an age we should handle it where we tried to set it.

```C#
Age age;
try {
	age = new(-20); // ERROR!
}
catch(ArgumentException ex)
{
	age = new(0); 
}
```

But this is really ugly when we think about it. We are mixing the happy path where everything worked as expected with the error path handling. We can do better. In Elixir we can write:

```Elixir
def new(age_value) when is_integer(age_value) and age_value >= 0 do 
	{:ok, age_value} 
end 

def new(age_value) when is_integer(age_value) and age_value < 0 do 
	{:error, :negative_age} 
end

def handle_age_result({:ok, age}) do 
	#...
end

def handle_age_result({:error, reason}) do 
	#...
end
```

```Elixir
25 
|> Age.new() # Might return {:ok, 25} or {:error, reason}
|> AgeCreationHandler.handle_age_result() # Handles these two cases
```

From a higher level view we dont want to be bothered with the mental load of the error path. As we can see in Elixir we decouple the error handling code from the happy code path.