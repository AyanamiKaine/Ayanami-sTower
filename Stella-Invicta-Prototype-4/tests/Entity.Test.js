import { test, expect, describe } from "bun:test";
import { Entity } from "../src/Entity";
import { health } from "../mixins/Health";
import { orbits } from "../mixins/Orbits";
import { polity } from "../mixins/Polity";
import { connectedTo } from "../mixins/ConnectedTo";

describe("Entity Constructor Tests", () => {
    test("EntityShouldHaveAName", () => {
        const e = new Entity("Karl");

        expect(e.name).toBe("Karl");
    });

    test("EntityShouldHaveADefaultEmptyName", () => {
        const e = new Entity();

        expect(e.name).toBe("");
    });

    test("EntitesCanHaveAParent", () => {
        const parent = new Entity("Parent");
        const child = new Entity("child", parent);

        expect(child.parent).toBe(parent);
        expect(parent.children.at(0)).toBe(child);
    });
});

describe("Adding Mixins to Entities", () => {
    test("AddingMixin", () => {
        const e = new Entity("Karl");
        e.with(health, 20);

        expect(e.health).toBe(20);
    });

    test("EntityHasMixin", () => {
        const e = new Entity("Karl");
        e.with(health, 20);

        expect(e.has(health)).toBe(true);
    });
});

describe("Health Mixin", () => {
    test("TakingDamage", () => {
        const e = new Entity("Karl");
        e.with(health, 20);

        e.takeDamage(30);

        expect(e.health).toBe(0);
    });

    test("HealingDamage", () => {
        const e = new Entity("Karl");
        e.with(health, 20);

        e.heal(10);

        expect(e.health).toBe(30);
    });
});

describe("Tag Mixins", () => {
    test("EntityCanHaveTags", () => {
        const e = new Entity("Karl");

        // tags or identifiers are just empty functions
        // they are simply used to say entity IsA character
        const character = () => { };

        e.with(character);

        expect(e.has(character)).toBe(true);
    });

    test("EntityHasCorrectTag", () => {
        const e = new Entity("Karl");

        // tags or identifiers are just empty functions
        // they are simply used to say entity IsA character
        const character = () => { };
        const starSystem = () => { };
        const planet = () => { };

        e.with(character);

        expect(e.has(character)).toBe(true);
        expect(e.has(starSystem)).toBe(false);
        expect(e.has(planet)).toBe(false);
    });
});

describe("Polity Mixin", () => {
    test("EntityCanBeAPolity", () => {
        const e = new Entity("Umbral Cult");
        const morian = new Entity("Morian");

        e.with(polity, morian, "VA", "Voice of the Abyss");
        expect(e.has(polity)).toBe(true);
        expect(e.abbreviation).toBe("VA");
        expect(e.leaderTitle).toBe("Voice of the Abyss");
    });
});

describe("Orbits Mixin", () => {
    test("EntityCanOrbitAnotherEntity", () => {
        const sun = new Entity("Sun");
        const earth = new Entity("Earth");
        const moon = new Entity("Moon");
        earth.with(orbits, sun);
        moon.with(orbits, earth);

        expect(moon.has(orbits)).toBe(true);
        expect(moon.orbits).toBe(earth);
        expect(earth.orbits).toBe(sun);
        expect(moon.orbits.orbits).toBe(sun);
    });
});

describe("ManyToManyRelationship Mixin", () => {
    test("EntityCanHaveManyToManyRelationshipComponent", () => {
        const sol = new Entity("Sol");
        const alphaCentauri = new Entity("Alpha Centauri");
        const korriban = new Entity("Korriban");

        // This relation is symetrically, this means
        // when we say that a is connected to b then
        // b is connected to a
        // In this case b needs to also have the connectedTo
        // component
        sol.with(connectedTo, alphaCentauri);
        korriban.with(connectedTo, sol);

        expect(alphaCentauri.has(connectedTo)).toBe(true);
        expect(sol.has(connectedTo)).toBe(true);
        expect(korriban.has(connectedTo)).toBe(true);

        expect(alphaCentauri.isConnectedTo(sol)).toBe(true);
        expect(sol.isConnectedTo(alphaCentauri)).toBe(true);

        expect(sol.isConnectedTo(korriban)).toBe(true);
        expect(korriban.isConnectedTo(sol)).toBe(true);
    });
});