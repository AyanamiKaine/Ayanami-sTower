require "./Entity.rb"

class EntityManager
  attr_reader :entities, :to_add, :entity_map, :total_entities

  def initialize()
    @entities = []
    @to_add = []
    @entity_map = Hash.new { |hash, key| hash[key] = [] }
    @total_entities = 0
  end

  # Adds entities, defers the actual adding until update is called again
  def add_entity(tag)
    @total_entities += 1
    entity = Entity.new(total_entities, tag)
    to_add.push(entity)
    entity
  end

  # Returns the list of ALL entities
  def get_all_entities
    entities
  end

  # Returns a list of entities based on a specified tag
  def get_entities(tag)
    entity_map[tag]
  end

  # Adds/Removees all to be added/dead entities to the entities list, this is done
  # to defer the moment when entities are removed or added, so no entities are
  # added/removed when other systems iterate over list of entities
  def update
    to_add.each do |entity|
      entities.push(entity)
      entity_map[entity.tag].push(entity)
    end

    remove_dead_entities(entities)

    entity_map.each_value do |entities|
      remove_dead_entities(entities)
    end

    to_add.clear # Clear to_add to avoid adding entities repeatedly
  end

private
  ## Removes dead entities in a entity list
  def remove_dead_entities(entities)
    entities.delete_if { |entity| !entity.alive }
  end
end
