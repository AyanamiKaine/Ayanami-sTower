# POCO Generator

Given a DB connection it generates POCOs based on the schema. Why are we doing this instead of the other way around? We want to decouple the database creation from the language platform. We define our schema in plain sql that can be executed by any SQLite driver in any language.