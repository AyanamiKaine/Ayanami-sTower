import { test, expect, describe } from "bun:test";
import { SparsedSet } from "../src/SparsedSet";

SparsedSet;
describe("Sparsed Set", () => {
    test("Adding new values", () => {
        let set = new SparsedSet();

        set.insert(25);

        expect(set.size).toBe(1);
        expect(set.has(25)).toBe(true);
        expect(set.has(2)).toBe(false);
        expect(set.size).toBe(1);
    });

    test("removing values", () => {
        let set = new SparsedSet();

        set.insert(25);

        expect(set.size).toBe(1);
        expect(set.has(25)).toBe(true);
        expect(set.has(2)).toBe(false);

        set.remove(25);
        expect(set.has(25)).toBe(false);
        expect(set.size).toBe(0);
    });
});
