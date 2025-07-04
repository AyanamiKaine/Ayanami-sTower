import { test, expect, describe } from "bun:test";
import { Entity } from "../src/game/Entity";
import { health } from "../src/game/mixins/Health";
import { orbits } from "../src/game/mixins/Orbits";
import { polity } from "../src/game/mixins/Polity";
import { connectedTo } from "../src/game/mixins/ConnectedTo";
import { trait } from "../src/game/mixins/Trait";
import { hasTraits } from "../src/game/mixins/HasTraits";
import { character } from "../src/game/mixins/Character";
import { featureDefinition } from "../src/game/mixins/FeatureDefinition";
import { hasFeatures } from "../src/game/mixins/HasFeatures";

describe("Entity Constructor Tests", () => {
    test("EntityShouldHaveAName", () => {
        const e = new Entity({ name: "Karl" });

        expect(e.name).toBe("Karl");
    });

    test("EntityShouldHaveADefaultEmptyName", () => {
        const e = new Entity();

        expect(e.name).toBe("");
    });

    test("EntitesCanHaveAParent", () => {
        const parent = new Entity({ name: "parent" });
        const child = new Entity({ name: "child", parent: parent });

        expect(child.parent).toBe(parent);
        expect(parent.children.at(0)).toBe(child);
    });
});

describe("Adding Mixins to Entities", () => {
    test("AddingMixin", () => {
        const e = new Entity({ name: "Karl" });
        e.with(health, 20);

        expect(e.health).toBe(20);
    });

    test("EntityHasMixin", () => {
        const e = new Entity({ name: "Karl" });
        e.with(health, 20);

        expect(e.has(health)).toBe(true);
    });
});

describe("Health Mixin", () => {
    test("TakingDamage", () => {
        const e = new Entity({ name: "Karl" });
        e.with(health, 20);

        e.takeDamage(30);

        expect(e.health).toBe(0);
    });

    test("HealingDamage", () => {
        const e = new Entity({ name: "Karl" });
        e.with(health, 20);

        e.heal(10);

        expect(e.health).toBe(30);
    });
});

describe("Tag Mixins", () => {
    test("EntityCanHaveTags", () => {
        const e = new Entity({ name: "Karl" });

        // tags or identifiers are just empty functions
        // they are simply used to say entity IsA character
        const character = () => {};

        e.with(character);

        expect(e.has(character)).toBe(true);
    });

    test("EntityHasCorrectTag", () => {
        const e = new Entity({ name: "Karl" });

        // tags or identifiers are just empty functions
        // they are simply used to say entity IsA character
        const character = () => {};
        const starSystem = () => {};
        const planet = () => {};

        e.with(character);

        expect(e.has(character)).toBe(true);
        expect(e.has(starSystem)).toBe(false);
        expect(e.has(planet)).toBe(false);
    });
});

describe("Polity Mixin", () => {
    test("EntityCanBeAPolity", () => {
        const e = new Entity({ name: "Umbral Cult" });
        const morian = new Entity({ name: "Morian" });

        e.with(polity, morian, "VA", "Voice of the Abyss");
        expect(e.has(polity)).toBe(true);
        expect(e.abbreviation).toBe("VA");
        expect(e.leaderTitle).toBe("Voice of the Abyss");
    });
});

describe("Orbits Mixin", () => {
    test("EntityCanOrbitAnotherEntity", () => {
        const sun = new Entity({ name: "Sun" });
        const earth = new Entity({ name: "Earth" });
        const moon = new Entity({ name: "Moon" });
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
        const sol = new Entity({ name: "Sol" });
        const alphaCentauri = new Entity({ name: "Alpha Centauri" });
        const korriban = new Entity({ name: "Korriban" });

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

describe("Trait Mixin", () => {
    test("EntitiesAreTraitsAndCanHaveTraits", () => {
        const korriban = new Entity({ name: "Korriban" });
        const toxicAtmosphereTrait = new Entity({
            name: "Toxic Atmosphere",
        }).with(trait, false);

        korriban.with(hasTraits);

        korriban.addTrait(toxicAtmosphereTrait);

        expect(korriban.hasTrait(toxicAtmosphereTrait)).toBe(true);
    });
});

describe("Feature Mixin", () => {
    test("EntitiesCanHaveFeatures", () => {
        const canFly = new Entity({ name: "Flight" }).with(
            featureDefinition,
            "CanFly",
            "Allows movement through the air.",
            "movement"
        );

        const immuneToPoison = new Entity({
            name: "Poison Immunity",
        }).with(
            featureDefinition,
            "ImmuneToPoison",
            "Grants immunity to all forms of poison.",
            "defense"
        );

        const angelicTrait = new Entity({ name: "Angelic" }).with(trait, true);
        angelicTrait.with(hasFeatures);

        // Add the defined features to the 'Angelic' trait itself.
        angelicTrait.addFeature(canFly);
        angelicTrait.addFeature(immuneToPoison);

        const myCharacter = new Entity({ name: "Seraphina" });
        myCharacter
            .with(character) // Mark as a character.
            .with(hasTraits) // Give the ability to have traits.
            .with(hasFeatures); // The character can also have its own direct features!
        // Add the 'Angelic' trait to Seraphina.
        myCharacter.addTrait(angelicTrait);

        expect(myCharacter.hasTrait(angelicTrait)).toBe(true);
        expect(angelicTrait.hasFeature(immuneToPoison)).toBe(true);
        expect(angelicTrait.hasFeature(canFly)).toBe(true);

        expect(myCharacter.hasFeature(immuneToPoison)).toBe(true);
        expect(myCharacter.hasFeature("CanFly")).toBe(true);
    });
});

describe("Entity Hierarchy", () => {
    test("SolarSystem", () => {
        const milkyWay = new Entity({ name: "Milky Way" });

        const sol = new Entity({ name: "Sol", parent: milkyWay });

        const sun = new Entity({ name: "Sun", parent: sol });
        const earth = new Entity({ name: "Earth", parent: sol });
        const moon = new Entity({ name: "Moon", parent: sol });

        expect(moon.parent.parent).toBe(milkyWay);
    });
});
