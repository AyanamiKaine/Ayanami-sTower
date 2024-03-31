#pragma once
#include <cmath>


class Vector2
{
public:
	float x {0};
	float y {0};

	Vector2() = default;
	Vector2(float x, float y) :
		x(x), y(y){}

	Vector2 operator + (const Vector2& rhs) const;
	Vector2 operator - (const Vector2& rhs) const;
	Vector2 operator * (const float value) const;
	Vector2 operator / (const float value) const;

	void operator += (const Vector2& rhs);
	void operator -= (const Vector2& rhs);
	void operator *= (const float value);
	void operator /= (const float value);

	bool operator == (const Vector2& rhs) const;
	bool operator != (const Vector2& rhs) const;

	Vector2& Add(const Vector2& vector2);
	Vector2& Scale(float scale);
	Vector2& Rotate(float angleInDegrees);

	[[nodiscard]] float Length() const;
	[[nodiscard]] float Distance(const Vector2& vector2) const;
	void Normalize();
};

inline Vector2 Vector2::operator+(const Vector2& rhs) const
{
	return Vector2{ x + rhs.x, y + rhs.y };
}

inline Vector2 Vector2::operator-(const Vector2& rhs) const
{
	return Vector2{ x - rhs.x, y - rhs.y };
}

inline bool Vector2::operator==(const Vector2& rhs) const
{
	constexpr float epsilon = 0.001f; // Small tolerance

	// Floating points errors could make two really similar vectors2
	// say that they are not the same for example when x1 = 1.0000000005 and x2 = 1.0000001
	// The difference is so small that we should treat them the same
	return (fabs(x - rhs.x) <= epsilon && fabs(y - rhs.y) <= epsilon);
}

inline bool Vector2::operator!=(const Vector2& rhs) const
{
}

inline Vector2 Vector2::operator*(const float value) const
{
	return Vector2{ x * value, y * value };
}

inline Vector2 Vector2::operator/(const float value) const
{
}

inline void Vector2::operator+=(const Vector2& rhs)
{
	x += rhs.x;
	y += rhs.y;
}

inline void Vector2::operator-=(const Vector2& rhs)
{
	x -= rhs.x;
	y -= rhs.y;
}

inline float Vector2::Length() const
{
	return sqrtf(x * x + y * y);
}


inline void Vector2::operator*=(const float value)
{
	x *= value;
	y *= value;
}

inline void Vector2::operator/=(const float value)
{
}

inline Vector2& Vector2::Rotate(float angleInDegrees) {
	const float angleInRadians = angleInDegrees * 3.14f / 180.0f;
	const float cosTheta = cos(angleInRadians);
	const float sinTheta = sin(angleInRadians);

	x = x * cosTheta - y * sinTheta;
	y = x * sinTheta + y * cosTheta;

	return *this;
}


inline Vector2& Vector2::Scale(float scale)
{
	x *= scale;
	y *= scale;

	return *this;
}

inline Vector2& Vector2::Add(const Vector2& vector2)
{
	x += vector2.x;
	y += vector2.y;

	return *this;
}

inline float Vector2::Distance(const Vector2& vector2) const
{
	return sqrtf((vector2.x - x) * (vector2.x - x) + (vector2.y - y) * (vector2.y - y));
}

inline void Vector2::Normalize()
{
	float length = Length();  // Calculate the current magnitude 
	if (length != 0.0f) { // Avoid division by zero
		x /= length;
		y /= length;
	}
}


