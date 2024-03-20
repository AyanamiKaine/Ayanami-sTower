require 'test/unit'
require './EntityManager.rb'

class EntityManagerTest < Test::Unit::TestCase
  def test_entity_creation_returns_entity
    entity_manager = EntityManager.new

    entity = entity_manager.add_entity("Enemy")

    assert_true(entity.alive)
  end

  def test_entity_creation_increases_total_numbers_of_entities
    entity_manager = EntityManager.new

    entityA = entity_manager.add_entity("Enemy")
    entityB = entity_manager.add_entity("Enemy")

    assert_equal(2, entityB.id)
  end

  def test_entity_creation_add_to_entities_list
    entity_manager = EntityManager.new

    entityA = entity_manager.add_entity("enemy")
    entityB = entity_manager.add_entity("enemy")

    assert_equal(2, entity_manager.to_add.size)
  end

  def test_get_tag_entities
    entity_manager = EntityManager.new

    entityA = entity_manager.add_entity("enemy")
    entityB = entity_manager.add_entity("player")

    entity_manager.update

    assert_equal(entityB.id, entity_manager.get_entities("player")[0].id)
  end

  def test_remove_dead_entities
    entity_manager = EntityManager.new

    entityA = entity_manager.add_entity("enemy")

    entity_manager.update

    entityA.alive = false

    entity_manager.update

    assert_equal(0, entity_manager.get_all_entities.size)
  end
end
