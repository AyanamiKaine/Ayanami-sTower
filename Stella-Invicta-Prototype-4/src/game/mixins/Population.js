export const population = (
    entity,
    consciousness = 0.0,
    militancy = 0.0,
    literacy = 0.0
) => {
    entity.consciousness = consciousness;
    entity.militancy = militancy;
    entity.literacy = literacy;
};
