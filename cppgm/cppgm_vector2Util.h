#include <cppgm_vector2.h>

namespace cppgm {
    struct cppgm_vector2Util
    {
        [[nodiscard]] float Length() const;
        [[nodiscard]] float Distance(const Vector2& vector2) const;
    };
}
