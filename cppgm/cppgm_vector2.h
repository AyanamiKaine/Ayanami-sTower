#pragma once
namespace cppgm {
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

		void Normalize();
	};
}