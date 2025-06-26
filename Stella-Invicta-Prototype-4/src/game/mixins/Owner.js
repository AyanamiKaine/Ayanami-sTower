// Imagine a planet that is owned by a character
export const owner = (entity, entityThatOwnsThisEntity) => {
    entity.owner = entityThatOwnsThisEntity;
};
