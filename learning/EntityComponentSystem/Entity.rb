class Entity
  attr_reader :id, :tag, :components
  attr_accessor :alive
  def initialize(id, tag)
    @id = id
    @tag = tag
    @alive = true
    @components = []
  end

  def is_alive?
    alive
  end

  def has_component?
  end

  def destroy
    alive = false
  end

  def add_component
  end

  def remove_component
  end
end
