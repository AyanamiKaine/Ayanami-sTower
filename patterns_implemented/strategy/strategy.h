#ifndef STRATEGY_H
#define STRATEGY_H

#include <memory>
#include <string>
#include <iostream>
#include <iomanip>

class TextFormatter {
public:
    virtual void format(const std::string &text) = 0;
};

class LeftFormatter : public TextFormatter {
public:
    void format(const std::string &text) override;
};

class CenterFormatter : public TextFormatter {
public:
    void format(const std::string &text) override;
};

class RightFormatter : public TextFormatter {
public:
    void format(const std::string &text) override;
};

class TextEditor {
public:
    void setFormatter(std::unique_ptr<TextFormatter> formatter);
    void publishText(const std::string &text);

private:
    std::unique_ptr<TextFormatter> formatter;
};

#endif // STRATEGY_H
