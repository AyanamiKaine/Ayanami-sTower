#include <cppgm_vector2Util.h>
namespace cppgm {
    float cppgm_vector2Util::Distance(const Vector2& vector2) const
    {
        return sqrtf((vector2.x - x) * (vector2.x - x) + (vector2.y - y) * (vector2.y - y));
    }

    float cppgm_vector2Util::Length() const
    {
        return sqrtf(x * x + y * y);
    }
}