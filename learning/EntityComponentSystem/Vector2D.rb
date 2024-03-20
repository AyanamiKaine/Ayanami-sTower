class Vector2D < Struct.new(:x,:y)

  def initialize(x = 0.0, y = 0.0)
    self.x = x
    self.y = y
  end

  def +(rhs)
    Vector2D.new(x + rhs.x, y + rhs.y)
  end

  def -(rhs)
    Vector2D.new(x - rhs.x, y - rhs.y)
  end

  def *(rhs)
    Vector2D.new(x * rhs.x, y * rhs.y)
  end

  def /(rhs)
    Vector2D.new(x / rhs.x, y / rhs.y)
  end

  def add(value)
    self.x += value
    self.y += value
    self # Return self for chaining
  end

  def scale(scale)
    self.x *= scale
    self.y *= scale
    self
  end

  def rotate(angle_in_degrees)
    angle_in_radians = angle_in_degrees * Math::PI / 180.0
    cos_theta = Math.cos(angle_in_radians)
    sin_theta = Math.sin(angle_in_radians)

    new_x = x * cos_theta - y * sin_theta
    new_y = x * sin_theta + y * cos_theta
    self.x = new_x
    self.y = new_y
    self
  end

  def normalize
    len = length
    if len > 0
      self.x /= len
      self.y /= len
    end
    self
  end

  def dot(rhs)
    x * rhs.x + y * rhs.y
  end

  def length
    Math.sqrt(x * x + y * y)
  end

  def distance(vec2D)
    dx = x - vec2D.x
    dy = y - vec2D.y
    Math.sqrt(dx * dx + dy * dy)
  end
end
