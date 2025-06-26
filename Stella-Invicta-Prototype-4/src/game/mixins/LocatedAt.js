// Imagine a character that is locatedAt a space station or a region on a planet.
export const owner = (entity, entityWhereWeAreLocated) => {
    entity.locatedAt = entityWhereWeAreLocated;
};
