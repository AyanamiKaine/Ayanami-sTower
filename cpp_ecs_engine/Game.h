#pragma once
#include <chrono>
#include <iostream>

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
	void SpawnBullet(std::shared_ptr<Entity> entity, const Vector2& mousePos);
public:
	Game();
	void Run();
};

inline void Game::SpawnBullet(std::shared_ptr<Entity> entityThatSpawnedTheBullet, const Vector2& target)
{
	auto bullet = m_entities.AddEntity("bullet");

	// Creating components for the bullet
	bullet->cCollision = std::make_shared<CCollision>(4.0f);
	bullet->cTransform = std::make_shared<CTransform>(entityThatSpawnedTheBullet->cTransform->position, Vector2{ 0,0 }, 0);
	bullet->cShape = std::make_shared<CShape>(4.0f, 4, sf::Color(10, 10, 10), sf::Color(255, 0, 0), 4.0f);


	// Calculating the velocity direction
	Vector2 direction = target - bullet->cTransform->position;
	direction.Normalize();

	// Invert the x/y value of the direction vector
	direction.y *= 1;
	direction.x *= 1;
	bullet->cTransform->velocity = direction * 5;

}

inline Game::Game()
{
	init();
}

// Some systems should run while we pause and
// some system should stop while we pause
inline void Game::Run()
{
	while (m_running)
	{
		m_entities.Update();

		if (!m_paused)
		{
			sEnemeySpawner();
			sMovement();
			sCollision();
		}
		sUserInput();
		sRender();

		m_currentFrame++;
	}
}

inline void Game::SetPaused(bool paused)
{
}

inline void Game::sMovement()
{
	const auto& list_of_entities = m_entities.GetEntities();

	for (auto& entity : list_of_entities)
	{
		if (entity->cInput)
		{
			if (entity->cInput->up)
				entity->cTransform->velocity.y = -5;
			else
				entity->cTransform->velocity.y = 0;

			if (entity->cInput->down)
				entity->cTransform->velocity.y = 5;

			if (entity->cInput->right)
				entity->cTransform->velocity.x = 5;
			else
				entity->cTransform->velocity.x = 0;

			if (entity->cInput->left)
				entity->cTransform->velocity.x = -5;

		}

		if (entity->cTransform)
		{
			 entity->cTransform->position += entity->cTransform->velocity;
		}

		for (auto bullet : m_entities.GetEntities("bullet"))
		{

		}
	}
}

inline void Game::sUserInput()
{
	sf::Event event;
	while (m_window.pollEvent(event))
	{
		// Close window : exit
		if (event.type == sf::Event::Closed)
		{
			m_running = false;
			m_window.close();
		}

		if (event.type == sf::Event::KeyPressed)
		{
			switch (event.key.code)
			{
			case sf::Keyboard::W:
				m_player->cInput->up = true;
				break;

			case sf::Keyboard::P:
				if (m_paused == false)
				{
					m_paused = true;
				}
				else
				{
					m_paused = false;
				}
				break;

			default:
				break;
			}
		}

		if (event.type == sf::Event::MouseButtonPressed)
		{
			if (event.mouseButton.button == sf::Mouse::Left)
			{
				SpawnBullet(m_player, Vector2{static_cast<float>(event.mouseButton.x), static_cast<float>(event.mouseButton.y)});
			}
		}

		if (sf::Keyboard::isKeyPressed(sf::Keyboard::W))
		{
			m_player->cInput->up = true;
		}
		else
		{
			m_player->cInput->up = false;
		}

		if (sf::Keyboard::isKeyPressed(sf::Keyboard::S))
		{
			m_player->cInput->down = true;
		}
		else
		{
			m_player->cInput->down = false;
		}

		if (sf::Keyboard::isKeyPressed(sf::Keyboard::D))
		{
			m_player->cInput->right = true;
		}
		else
		{
			m_player->cInput->right = false;
		}

		if (sf::Keyboard::isKeyPressed(sf::Keyboard::A))
		{
			m_player->cInput->left = true;
		}
		else
		{
			m_player->cInput->left = false;
		}
	}

}

inline void Game::sRender()
{
	// Clear screen
	m_window.clear();

	const auto& list_of_entities = m_entities.GetEntities();

	for (auto& entity : list_of_entities)
	{
		if (entity->cShape)
		{
			entity->cTransform->angle += 1.0f;
			entity->cShape->circle.setRotation(entity->cTransform->angle);

			entity->cShape->circle.setPosition(entity->cTransform->position.x, entity->cTransform->position.y);

			m_window.draw(entity->cShape->circle);

		}
	}

	// Update the window
	m_window.display();
}

inline void Game::sEnemeySpawner()
{
	if (m_currentFrame % 120 == 0)
	{
		auto entity = m_entities.AddEntity("enemy");

		float ex = rand() % m_window.getSize().x;
		float ey = rand() % m_window.getSize().y;

		entity->cCollision = std::make_shared<CCollision>(32.0f);
		entity->cTransform = std::make_shared<CTransform>(Vector2(ex, ey), Vector2(0.0f, 0.0f), 0);
		entity->cShape = std::make_shared<CShape>(32.0f, 8, sf::Color(10, 10, 10), sf::Color(125, 0, 60), 4.0f);

		m_lastEnemySpawnTime = m_currentFrame;
	}
}

inline void Game::sCollision()
{
	for (auto bullet : m_entities.GetEntities("bullet"))
	{
		for (auto enemy : m_entities.GetEntities("enemy"))
		{
			if(bullet->cTransform->position.Distance(enemy->cTransform->position) <= bullet->cCollision->radius + enemy->cCollision->radius)
			{
				enemy->Destroy();
				bullet->Destroy();
			}
		}
	}
}

inline void Game::sLifespan()
{
}

inline void Game::SpawnPlayer()
{
	auto entity = m_entities.AddEntity("player");

	entity->cTransform = std::make_shared<CTransform>(
		Vector2{ 400.0f, 600.0f },
		Vector2{ 0,0 }, 
		10);

	entity->cShape = std::make_shared<CShape>(32.0f, 8, sf::Color(10, 10, 10), sf::Color(255, 0, 0), 4.0f);

	entity->cInput = std::make_shared<CInput>();

	m_player = entity;
}


inline void Game::init()
{
	m_window.create(sf::VideoMode(1280, 720), "Game Test");
	m_window.setFramerateLimit(60);

	SpawnPlayer();
	m_running = true;
}
