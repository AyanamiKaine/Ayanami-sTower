#pragma once

class Vector2D {
public:
    float x {0.0f};
    float y {0.0f};

    Vector2D() = default;
    Vector2D(float x, float y):
        x(x), y(y) {};

    Vector2D operator + (const Vector2D& rhs) const;
    Vector2D operator - (const Vector2D& rhs) const;
    Vector2D operator * (const Vector2D& rhs) const;
    Vector2D operator * (const float value)   const;
    Vector2D operator / (const Vector2D& rhs) const;
    

    void operator += (const Vector2D& rhs);
    void operator -= (const Vector2D& rhs);
	void operator *= (const Vector2D& rhs);
    void operator /= (const Vector2D& rhs);

    Vector2D& Add(float value);
    Vector2D& Scale(float scale);
    Vector2D& Rotate(float angleInDegrees);
	Vector2D& Normalize();

    [[nodiscard]] float Dot(const Vector2D& rhs) const;
	[[nodiscard]] float Length() const;
    [[nodiscard]] float Distance(const Vector2D& vec2D) const;
};
