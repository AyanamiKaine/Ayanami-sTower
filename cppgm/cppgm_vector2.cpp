#include <cppgm_vector2.h>
#include <cmath>

namespace cppgm {
	Vector2 Vector2::operator+(const Vector2& rhs) const
	{
		return Vector2{ x + rhs.x, y + rhs.y };
	}

	Vector2 Vector2::operator-(const Vector2& rhs) const
	{
		return Vector2{ x - rhs.x, y - rhs.y };
	}

	// Sadly this is qustionable because the tolerance could be for some applications too big and for other too small, it all 
	// depends on the specifc domain this vector class is used, the tolerance should be either much bigger or possible be changed
	// by a user or completly removed
	bool Vector2::operator==(const Vector2& rhs) const
	{
		constexpr float epsilon = 0.001f; // Small tolerance

		// Floating points errors could make two really similar vectors2
		// say that they are not the same for example when x1 = 1.0000000005 and x2 = 1.0000001
		// The difference is so small that we should treat them the same
		return (fabs(x - rhs.x) <= epsilon && fabs(y - rhs.y) <= epsilon);
	}

	bool Vector2::operator!=(const Vector2& rhs) const
	{
	}

	Vector2 Vector2::operator*(const float value) const
	{
		return Vector2{ x * value, y * value };
	}

	Vector2 Vector2::operator/(const float value) const
	{
	}

	void Vector2::operator+=(const Vector2& rhs)
	{
		x += rhs.x;
		y += rhs.y;
	}

	void Vector2::operator-=(const Vector2& rhs)
	{
		x -= rhs.x;
		y -= rhs.y;
	}

	void Vector2::operator*=(const float value)
	{
		x *= value;
		y *= value;
	}

	void Vector2::operator/=(const float value)
	{
	}

	Vector2& Vector2::Rotate(float angleInDegrees) {
		const float angleInRadians = angleInDegrees * 3.14f / 180.0f;
		const float cosTheta = cos(angleInRadians);
		const float sinTheta = sin(angleInRadians);

		x = x * cosTheta - y * sinTheta;
		y = x * sinTheta + y * cosTheta;

		return *this;
	}


	Vector2& Vector2::Scale(float scale)
	{
		x *= scale;
		y *= scale;

		return *this;
	}

	Vector2& Vector2::Add(const Vector2& vector2)
	{
		x += vector2.x;
		y += vector2.y;

		return *this;
	}

	void Vector2::Normalize()
	{
		float length = Length();  // Calculate the current magnitude 
		if (length != 0.0f) { // Avoid division by zero
			x /= length;
			y /= length;
		}
	}
}