require 'test/unit'
require './Vector2D.rb'

class Vector2DTest < Test::Unit::TestCase
  def test_vector_addtion
    vec1 = Vector2D.new(5,5)
    vec2 = Vector2D.new(10,10)

    vec3 = vec1 + vec2

    assert_equal(Vector2D.new(15,15), vec3)
  end

  def test_vector_multiplication
    vec1 = Vector2D.new(5,5)
    vec2 = Vector2D.new(10,10)

    vec3 = vec1 * vec2

    assert_equal(Vector2D.new(50,50), vec3)
  end

  def test_float_vector_division
    vec1 = Vector2D.new(5.0,5.0)
    vec2 = Vector2D.new(10.0,10.0)

    vec3 = vec1 / vec2

    assert_equal(Vector2D.new(0.5,0.5), vec3)
  end

  def test_integer_vector_division
    vec1 = Vector2D.new(10,10)
    vec2 = Vector2D.new(5,5)

    vec3 = vec1 / vec2

    assert_equal(Vector2D.new(2,2), vec3)
  end

  def test_vector_substraction
    vec1 = Vector2D.new(5,5)
    vec2 = Vector2D.new(10,10)

    vec3 = vec1 - vec2

    assert_equal(Vector2D.new(-5,-5), vec3)
  end

  def test_vector_addition_assignment
    vec1 = Vector2D.new(5,5)
    vec2 = Vector2D.new(10,10)

    vec2 += vec1

    assert_equal(Vector2D.new(15,15), vec2)
  end

  def test_vector_substraction_assignment
    vec1 = Vector2D.new(5,5)
    vec2 = Vector2D.new(10,10)

    vec1 -= vec2

    assert_equal(Vector2D.new(-5,-5), vec1)
  end

  def test_vector_mutliplication_assignment
    vec1 = Vector2D.new(5,5)
    vec2 = Vector2D.new(10,10)

    vec1 *= vec2

    assert_equal(Vector2D.new(50,50), vec1)
  end

  def test_vector_division_assignment
    vec1 = Vector2D.new(5,5)
    vec2 = Vector2D.new(10,10)

    vec2 /= vec1

    assert_equal(Vector2D.new(2,2), vec2)
  end

  def test_vector_scale
    vec1 = Vector2D.new(5,5)

    vec1.scale(2).scale(2)

    assert_equal(Vector2D.new(20,20), vec1)
  end
end
