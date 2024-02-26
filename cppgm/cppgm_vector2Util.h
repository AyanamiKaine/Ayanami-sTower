#include <cppgm_vector2.h>
struct cppgm_vector2Util
{
    [[nodiscard]] float Length() const;
	[[nodiscard]] float Distance(const Vector2& vector2) const;
};
