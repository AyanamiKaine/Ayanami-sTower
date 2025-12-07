---
sidebar_position: 99
draft: true
---

# DRAFT Communication Between Systems

<span class="api-stable">Stable API</span> <span class="skill-intermediate">Intermediate Elixir</span>

## Overview

Often times we want that systems communicate with each other. This could be trivial things like indicating that a character has died. That a new year or month is beginning. What day it is.

I believe we need two things.

1. A Pub/Sub message pipeline.
2. A way for systems to communicate directly. (This could lead to the problem that systems start to depend on each other. Relying only on the Pub/Sub mechanism could be much better. This needs to be more tested)

A pub sub system is probably a better choice, because this would also allow for mods to listen for them.

<div class="prerequisites">

#### Prerequisites

-   [Time Step](/docs/Engine/timestep) - How hourly updates function

</div>

---

## Quick Start

---

## Core Concepts

### Concept 1: Pub/Sub Event Queue

---

## API Reference

**Example:**

```elixir

```

---

### `ModuleName.experimental_function/1` {#experimental-function}

<span class="api-experimental">Experimental</span>

:::warning Experimental API
This function may change in future versions. Use with caution in production mods.
:::

<div class="function-signature">

```elixir
@spec experimental_function(input :: term()) :: term()
```

</div>

---

## Pipeline Integration

---

## Modding Guide

---

## Common Patterns

---

## Troubleshooting

---

## Performance Considerations

---

## Related

-   [Shared Context](/docs/Engine/shared-context) - ECS architecture

---
