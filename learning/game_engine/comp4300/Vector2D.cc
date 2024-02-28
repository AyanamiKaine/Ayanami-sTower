#include "Vector2D.h"
#include "cmath"

Vector2D Vector2D::operator + (const Vector2D& rhs) const
{
    return Vector2D{ x + rhs.x, y + rhs.y };
};

Vector2D Vector2D::operator - (const Vector2D& rhs) const
{
    return Vector2D{ x - rhs.x, y - rhs.y };
};

Vector2D Vector2D::operator * (const Vector2D& rhs) const {
    return Vector2D {x * rhs.x, y * rhs.y };
}

Vector2D Vector2D::operator * (const float value) const {
    return Vector2D {x * value, y * value };
}

Vector2D Vector2D::operator / (const Vector2D& rhs) const {
    return Vector2D {x / rhs.x, y / rhs.y };
}

void Vector2D::operator += (const Vector2D& rhs)
{
	x += rhs.x;
	y += rhs.y;
};

void Vector2D::operator -= (const Vector2D& rhs)
{
	x -= rhs.x;
	y -= rhs.y;
};

void Vector2D::operator/= (const Vector2D& rhs) {
    x /= rhs.x;
    y /= rhs.y;    
}


void Vector2D::operator *= (const Vector2D& rhs)
{
	x *= rhs.x;
	y *= rhs.y;
};

Vector2D& Vector2D::Add(float value) {
    x += value;
    y += value;

    return *this;
};

Vector2D& Vector2D::Scale(float scale)
{
	x *= scale;
	y *= scale;

	return *this;
};

Vector2D& Vector2D::Rotate(float angleInDegrees) {
	const float angleInRadians = angleInDegrees * 3.14f / 180.0f;
	const float cosTheta = cos(angleInRadians);
	const float sinTheta = sin(angleInRadians);

	x = x * cosTheta - y * sinTheta;
	y = x * sinTheta + y * cosTheta;

	return *this;
};


Vector2D& Vector2D::Normalize() {
    float len = Length();
    if (len > 0) { // Avoid division by zero
        x /= len;
        y /= len;
    }
    return *this;
}

float Vector2D::Dot(const Vector2D& rhs) const {
    return x * rhs.x + y * rhs.y; 
}

float Vector2D::Length() const
{
	return sqrtf(x * x + y * y);
};

float Vector2D::Distance(const Vector2D& vec2D) const {
    float dx = x - vec2D.x;
    float dy = y - vec2D.y;
    return sqrtf(dx * dx + dy * dy);
}

