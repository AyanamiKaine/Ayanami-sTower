/**
 * Grants the ability to have a 3D position and related helper methods.
 * This mixin initializes a `position3D` property on the entity.
 *
 * @param {object} entity - The entity to apply the position to.
 * @param {object} [initialPosition={x: 0, y: 0, z: 0}] - The starting position vector.
 * @param {number} [initialPosition.x=0] - The initial x coordinate.
 * @param {number} [initialPosition.y=0] - The initial y coordinate.
 * @param {number} [initialPosition.z=0] - The initial z coordinate.
 * @returns {object} An object containing helper methods for manipulating the position.
 */
export const position3D = (entity, initialPosition = { x: 0, y: 0, z: 0 }) => {
    // Assign the initial position to the entity.
    // A new object is created to avoid potential reference issues with the input object.
    entity.position3D = {
        x: initialPosition.x || 0,
        y: initialPosition.y || 0,
        z: initialPosition.z || 0
    };

    return {
        /**
         * Moves the entity by a given delta vector.
         * @param {object} delta - The vector to add to the current position.
         * @param {number} [delta.x=0] - The change in the x coordinate.
         * @param {number} [delta.y=0] - The change in the y coordinate.
         * @param {number} [delta.z=0] - The change in the z coordinate.
         */
        move(delta) {
            if (!delta) return;
            entity.position3D.x += delta.x || 0;
            entity.position3D.y += delta.y || 0;
            entity.position3D.z += delta.z || 0;
        },

        /**
         * Sets the entity's position to a new absolute position.
         * Any omitted coordinates will remain unchanged.
         * @param {object} newPosition - The new absolute position vector.
         * @param {number} [newPosition.x] - The new x coordinate.
         * @param {number} [newPosition.y] - The new y coordinate.
         * @param {number} [newPosition.z] - The new z coordinate.
         */
        setPosition(newPosition) {
            if (!newPosition) return;
            entity.position3D = { ...entity.position3D, ...newPosition };
        },

        /**
         * Calculates the Euclidean distance to another entity that also has a position3D behavior.
         * @param {object} otherEntity - The entity to measure the distance to.
         * @returns {number|null} The distance, or null if the other entity doesn't have a position.
         */
        distanceTo(otherEntity) {
            if (!otherEntity || typeof otherEntity.position3D !== 'object') {
                console.warn("distanceTo: The 'otherEntity' does not have a 'position3D' property.");
                return null;
            }
            const dx = entity.position3D.x - otherEntity.position3D.x;
            const dy = entity.position3D.y - otherEntity.position3D.y;
            const dz = entity.position3D.z - otherEntity.position3D.z;
            return Math.sqrt(dx * dx + dy * dy + dz * dz);
        }
    };
};
