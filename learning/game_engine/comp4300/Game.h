#pragma once

#include "EntityManager.h"
#include "SFML/Graphics.hpp"
class Game
{
	EntityManager			m_entities;
	sf::RenderWindow		m_window;

	sf::Font				m_font;
	sf::Text				m_text;

	int						m_score{ 0 };
	int						m_currentFrame{ 0 };
	int						m_lastEnemySpawnTime{ 0 };
	bool					m_paused;
	bool					m_running;

	std::shared_ptr<Entity>	m_player;

	void init();

	void SetPaused(bool paused);

	// Systems
	void sMovement();
	void sUserInput();
	void sRender();
	void sEnemeySpawner();
	void sCollision();
	void sLifespan();

	void SpawnPlayer();
	void SpawnEnemy();
	void SpawnBullet(std::shared_ptr<Entity> entity, const Vector2D& mousePos);
public:
	Game();
	void Run();
};

