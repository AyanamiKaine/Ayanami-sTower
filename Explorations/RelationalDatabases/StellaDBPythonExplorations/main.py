import sqlite3
import timeit

from pypika import Query, Table, Parameter

conn = sqlite3.connect("C:/Users/ayanami/Ayanami-sTower/Explorations/RelationalDatabases/StellaDBPythonExplorations/Exploration.db")
cursor = conn.cursor()


    # Helper function to create a named entity using Pypika to build queries
def Entity(name_value, owner_id):
    # Build the INSERT query for the Entity table using Pypika
    # Parameter('?') represents a standard SQL placeholder for security
    insert_entity_query = Query.into(Entity).columns('OwnerId').insert(Parameter('?'))
    sql_entity = insert_entity_query.get_sql()
        
    # Execute the generated SQL
    cursor.execute(sql_entity, (owner_id,))
        
    # Get the ID of the entity we just created
    new_id = cursor.lastrowid

    # Build the INSERT query for the Name table
    insert_name_query = Query.into(Name).columns('EntityId', 'Value').insert(Parameter('?'), Parameter('?'))
    sql_name = insert_name_query.get_sql()
        
    # Execute the generated SQL
    cursor.execute(sql_name, (new_id, name_value))
        
    return new_id

try:
    # Define table objects using Pypika
    Entity = Table('Entity')
    Name = Table('Name')
    Age = Table('Age')
    # Start a transaction manually.
    conn.execute("BEGIN TRANSACTION;")

    # Commit the transaction to save all changes.
    conn.commit()

# --- Query and Print All Entities ---
    print("\n--- Querying All Entities ---")

    # Build the SELECT query using Pypika to join the two tables
    select_query = Query.from_(Entity).join(Name).on(
        Entity.Id == Name.EntityId
    ).select(
        Entity.Id,
        Name.Value,
        Entity.OwnerId
    )

    # Get the SQL string from the builder
    sql_select = select_query.get_sql()
    print(f"Executing Query: {sql_select}")
    
    # Execute the query and fetch all results
    cursor.execute(sql_select)
    all_entities = cursor.fetchall()

    print("\n--- All Named Entities in Database ---")
    for entity in all_entities:
        entity_id, entity_name, owner_id = entity
        print(f"> ID: {entity_id}, Name: '{entity_name}', Owner ID: {owner_id}")
    print("------------------------------------")

    print("\n--- Incrementing All Ages by 1 ---")
    
    # Build the UPDATE query using Pypika.
    # We can use the Field object directly in the 'set' method for arithmetic.
    update_age_query = Query.update(Age).set(Age.Value, Age.Value + 1)
    
    sql_update = update_age_query.get_sql()
    print(f"Executing Query: {sql_update}")

    def run_query():
        cursor.execute(sql_select)
        cursor.fetchall()

    # --- Running the benchmark ---
    # The number of times to execute the function
    number_of_executions = 1000

    print(f"Running the query {number_of_executions} times to get an accurate benchmark...")

    total_time = timeit.timeit(run_query, number=number_of_executions)

    average_time = (total_time / number_of_executions) * 1000

    if average_time > 0:
        executions_per_ms = 1 / average_time
    else:
        executions_per_ms = float('inf')

    print(f"\n--- Benchmark Results ---")
    print(f"Total time for {number_of_executions} executions: {total_time:.4f} seconds.")
    print(f"Average time per query: {average_time:.10f} ms.")
    print("-------------------------")
    print(f"Theoretical executions per ms: {executions_per_ms:.2f}") # Calculated value

except sqlite3.Error as e:
    print(f"Database error: {e}")
    # If an error occurred, roll back any changes made during the transaction
    if conn:
        conn.rollback()
finally:
    # Ensure the database connection is always closed
    if conn:
        conn.close()