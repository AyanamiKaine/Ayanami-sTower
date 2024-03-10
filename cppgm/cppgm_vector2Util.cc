#include "cppgm_vector2Util.h"
#include "cppgm_vector2.h"
#include <cmath>

namespace cppgm {
    [[nodiscard]] float cppgm_vector2Util::Distance(const Vector2& vec2A, const Vector2& vec2B) const
    {
        return sqrtf((vec2A.x - vec2B.x) * (vec2A.x - vec2B.x) + (vec2A.y - vec2B.y) * (vec2A.y - vec2B.y));
    };


}
