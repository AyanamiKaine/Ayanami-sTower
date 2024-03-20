require "./Scene.rb"

class GameEngine
  attr_reader :scenes

  def initialize(scenes)
    @scenes = scenes
  end

  def Quit
  end

  def update
    scenes.update
  end

  def update
  end
end

GameEngine.new(Scene.new([])).update
