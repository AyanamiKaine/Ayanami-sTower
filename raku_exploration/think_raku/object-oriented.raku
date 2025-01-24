# OOP in Raku is a bit weird.

# This is because you can easily model an 'Is-A' relationship as well
# as an 'Has-A' Relationship. 

# An 'Is-A' relationship is usually defined using inheritance.
# While 'Has-A' is usually defined using interfaces.
# In Raku interfaces are called roles.

# In general, classes are meant for managing objects and roles are meant for managing behavior and code reuse within objects

# The thing is that in many languages like C# and to some degree in Java you cannot mix in an interface at runtime for an object. Roles can be mixed into objects NOT ITS UNDERLYING CLASS.
# While I believe mixins in Java work at the class level not the specific object.(But this could be wrong)

# Roles can even be anonymous. (This is something I find kinda insane, what crazy ideas could we implement with that?)

