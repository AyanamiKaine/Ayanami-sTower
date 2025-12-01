---
sidebar_position: 2
---

# AI

<span class="badge badge--in-progress">In Progress</span>

## Core Principle

:::tip Fundamental Rule
The AI can use **any feature** the player can use. There shall not be one feature limited to the player.
:::

This ensures:

-   **Fair gameplay** - Players can't exploit AI limitations
-   **Emergent strategies** - AI can surprise players with creative solutions
-   **System integrity** - Features must be robust enough for AI use

## AI Decision Flow

```mermaid
flowchart TD
    A[Evaluate Game State] --> B[Identify Goals]
    B --> C[Consider Available Actions]
    C --> D{Action Available to Player?}
    D -->|Yes| E[Evaluate Action Value]
    D -->|No| F[Skip - Not Implemented]
    E --> G[Execute Best Action]
```

## Implementation Checklist

When adding any new feature, verify:

-   [ ] AI can detect when to use the feature
-   [ ] AI can evaluate the value of using it
-   [ ] AI can execute the feature correctly
-   [ ] AI handles edge cases gracefully

---

_See also: [AI Design Principles](/docs/Design/AI)_
