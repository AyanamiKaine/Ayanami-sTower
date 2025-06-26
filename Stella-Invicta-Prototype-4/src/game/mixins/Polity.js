/*
"A polity is a group of people with a collective identity,
who are organized by some form of political institutionalized 
social relations, and have a capacity to mobilize resources"

Why the name polity instead of lets say faction ?
It much better captures the diversity of complex political structures. 
https://en.wikipedia.org/wiki/Polity
*/
export const polity = (
    entity,
    seatOfPowerEntity,
    abbreviation = "",
    leaderTitle = ""
) => {
    entity.seatOfPower = seatOfPowerEntity;
    entity.abbreviation = abbreviation;
    entity.leaderTitle = leaderTitle;
};
