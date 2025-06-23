// Represents a one-to-many relationship where one entity can orbit another but many entities can orbit the same entity
export const orbits = (entity, entityToOrbit) => {
    entity.orbits = entityToOrbit;
};
