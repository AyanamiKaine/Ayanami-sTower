#include "Assets.h"
#include "Animation.h"
#include <SFML/Audio/Sound.hpp>
#include <SFML/Audio.hpp>
#include <SFML/Graphics/Font.hpp>
#include <SFML/Graphics/Texture.hpp>
#include <fstream>
#include <iostream>

void Assets::AddTexture(const std::string& name, const std::string& path)
{
    sf::Texture texture;
    
    if(!texture.loadFromFile(path))
    {
        std::cerr << "Could not load image" << " " << path << "!\n";
    }
    else 
    {
        std::cout << "Loading Asset:" << " " << path << "\n";
    }
    m_textures[name] = texture;
}

void Assets::AddFont(const std::string& name, const std::string& path)
{
    sf::Font font;

    if(!font.loadFromFile(path))
    {
        std::cerr << "Could not load font" << " " << path << "!\n";
    }
    else 
    {
        std::cout << "Loading Asset:" << " " << path << "\n";
    }
    m_fonts[name] = font;
}


void Assets::AddSound(const std::string& name, const std::string& path)
{
    sf::SoundBuffer soundBuffer;
    
    if(!soundBuffer.loadFromFile(path))
    {
        std::cerr << "Could not load sound" << " " << path << "!\n";
    }
    else 
    {
        std::cout << "Loading Asset:" << " " << path << "\n";
    }

    m_sounds[name] = soundBuffer;
}

void Assets::AddAnimation(const std::string& name, Animation animation)
{

}

const sf::Font& Assets::GetFont(const std::string& name) const 
{
   return m_fonts.at(name); 
}

void Assets::LoadFromFile(const std::string& path)
{
    // Looking for valid file extensions, like png, jpg, wav, etc.
    // If a valid file extension is found use the correct logic for it AddSount, etc.
    // Do this for all files and recursivly for all folders specified in the path
    
    std::cout << "Start loading assets defined in config file:" << " " << path << "\n";

    std::ifstream file(path);
    if (!file) {
        std::cerr << "Could not load config.txt file!\n";
        exit(-1);
    }
    std::string head;
    while (file >> head) {
        if (head == "Font") {
            std::string font_name;
            std::string font_path;
            file >> font_name >> font_path;
            AddFont(font_name, font_path);
        }
        else if (head == "Texture") {
            std::string name;
            std::string path;
            file >> name >> path;
            AddTexture(name, path);
        }
        else {
            //std::cerr << "head to " << head << "\n";
            //std::cerr << "The config file format is incorrect!\n";
        }
    }
}


