#pragma once

#include "Animation.h"

#include <SFML/Graphics/Font.hpp>
#include <SFML/Graphics/Texture.hpp>
#include <SFML/Audio.hpp>
#include <map>
#include <string>

/*
 * The Asset Class holds all assets.
*/

class Assets {
private:
    std::map<std::string, sf::Texture> m_textures;
    std::map<std::string, Animation> m_animations;
    //Sounds are stored as a soundBuffer that must be loaded by a sf::sound class (sound.setBuffer(buffer)) and then it can be played with sound.play()
    std::map<std::string, sf::SoundBuffer> m_sounds;
    std::map<std::string, sf::Font> m_fonts;

public:
    void AddTexture(const std::string& name, const std::string& path);
    void AddAnimation(const std::string& name, Animation animation);
    void AddSound(const std::string& name, const std::string& path);
    void AddFont(const std::string& name, const std::string& path);

    const sf::Texture& GetTexture(const std::string& name) const;
    const Animation& GetAnimation(const std::string& name) const; 
    const sf::Sound& GetSound(const std::string& name) const;
    const sf::Font& GetFont(const std::string& name) const;

    //Load from file will do its best to try to load all valid assets from a path recursivly
    void LoadFromFile(const std::string& path);
};
