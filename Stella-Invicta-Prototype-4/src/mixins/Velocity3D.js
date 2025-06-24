/**
 * Grants the ability to have a 3D velocity.
 * This mixin is a simple data container for velocity properties.
 *
 * @param {object} entity - The entity to apply the velocity to.
 * @param {object} [initialVelocity={x: 0, y: 0, z: 0}] - The starting velocity vector.
 * @param {number} [initialVelocity.x=0] - The initial velocity on the x-axis.
 * @param {number} [initialVelocity.y=0] - The initial velocity on the y-axis.
 * @param {number} [initialVelocity.z=0] - The initial velocity on the z-axis.
 * @returns {object} An object containing helper methods for manipulating velocity.
 */
export const velocity3D = (entity, initialVelocity = { x: 0, y: 0, z: 0 }) => {
    // Assign the initial velocity to the entity.
    entity.velocity3D = {
        x: initialVelocity.x || 0,
        y: initialVelocity.y || 0,
        z: initialVelocity.z || 0
    };

    return {
        /**
         * Sets the entity's velocity to a new value.
         * @param {object} newVelocity - The new velocity vector.
         */
        setVelocity(newVelocity) {
            if (!newVelocity) return;
            entity.velocity3D.x = newVelocity.x || 0;
            entity.velocity3D.y = newVelocity.y || 0;
            entity.velocity3D.z = newVelocity.z || 0;
        },

        /**
         * Changes the entity's velocity by a given delta vector (i.e., accelerates it).
         * @param {object} delta - The vector to add to the current velocity.
         */
        accelerate(delta) {
            if (!delta) return;
            entity.velocity3D.x += delta.x || 0;
            entity.velocity3D.y += delta.y || 0;
            entity.velocity3D.z += delta.z || 0;
        }
    };
};
