// --- Self-Contained Behavior Functions (Mixins) ---
// These functions now initialize their own required properties.
// This makes them more encapsulated and easier to use.

/**
 * Grants the ability to have and manage health, and initializes health for the entity.
 * @param {object} entity - The entity.
 * @param {number} initialHealth - The starting and maximum health.
 */
export const health = (entity, initialHealth) => {
    entity.health = initialHealth;

    return {
        takeDamage: (amount) => {
            const newHealth = entity.health - amount;
            if (newHealth < 0) entity.health = 0;
            else entity.health = newHealth;
        },
        heal: (amount) => {
            const newHealth = entity.health + amount;
            entity.health = newHealth;
        },
    };
};
