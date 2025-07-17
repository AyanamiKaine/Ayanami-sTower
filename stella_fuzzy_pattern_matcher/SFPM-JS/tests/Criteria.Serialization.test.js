import { test, expect, describe } from "bun:test";
import { Criteria, Operator } from "../src/Criteria.js";
import { DictionaryFactSource } from "../src/FactSource.js";

describe("Criteria Serialization and Deserialization (without Predicates)", () => {

    const facts = new DictionaryFactSource(
        new Map([
            ["name", "Nick"],
            ["level", 15],
        ])
    );

    test("should correctly serialize a simple Criteria to a JSON string", () => {
        const criteria = new Criteria("level", 15, Operator.Equal);
        const jsonString = JSON.stringify(criteria);

        // The toJSON method is called automatically by JSON.stringify
        expect(jsonString).toBe('{"factName":"level","expectedValue":15,"operator":"Equal"}');
    });

    test("should throw an error when trying to serialize a Predicate Criteria", () => {
        const predicateCriteria = new Criteria("name", (name) => name === "Nick", Operator.Predicate);

        // We expect the JSON.stringify call, which triggers toJSON(), to throw our specific error.
        expect(() => {
            JSON.stringify(predicateCriteria);
        }).toThrow("Criteria with a Predicate operator cannot be serialized to JSON.");
    });

    test("should deserialize a JSON string into a simple Criteria and evaluate it correctly", () => {
        const jsonString = '{"factName":"level","expectedValue":15,"operator":"Equal"}';

        const criteria = Criteria.fromJSON(jsonString);

        expect(criteria).toBeInstanceOf(Criteria);
        expect(criteria.factName).toBe("level");
        expect(criteria.operator).toBe(Operator.Equal);
        expect(criteria.evaluate(facts)).toBe(true);
    });

    test("should throw an error when trying to deserialize a JSON object with a Predicate operator", () => {
        const jsonWithPredicate = {
            factName: "name",
            expectedValue: null, // Value doesn't matter as the operator check comes first
            operator: "Predicate"
        };

        expect(() => {
            Criteria.fromJSON(jsonWithPredicate);
        }).toThrow("Cannot deserialize a Criteria with a Predicate operator.");
    });
});