#pragma once

#include "Animation.h"

#include <SFML/Graphics/Font.hpp>
#include <SFML/Graphics/Texture.hpp>
#include <SFML/Audio/Sound.hpp>
#include <map>
#include <string>

/*
 * The Asset Class holds all assets.
*/

class Assets {
public:
    std::map<std::string, sf::Texture> m_textures;
    std::map<std::string, Animation> m_animations;
    std::map<std::string, sf::Sound> m_sounds;
    std::map<std::string, sf::Font> m_fonts;

private:
    void AddTexture(std::string name, std::string path);
    void AddAnimation(std::string name, Animation);
    void AddSound(std::string name, std::string path);
    void AddFont(std::string name, std::string path);

    sf::Texture& GetTexture(std::string name);
    Animation& GetAnimation(std::string name); 
    sf::Sound& GetSound(std::string name);
    sf::Font& GetFont(std::string name);
};
