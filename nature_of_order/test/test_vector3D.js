import { equal } from "assert";
import { Vector3D } from "../build/nature_of_order/Vector3D.js";

describe("Vector3D", function () {
	describe("Create a Vector", function () {
		it("When a vector is created its defaults should be {0,0,0}", function () {
			let vec = new Vector3D();

			equal(vec.x, 0);
			equal(vec.y, 0);
			equal(vec.z, 0);
		});
	});

	describe("equality vector3d", function () {
		it("if two vectors have the same value they are equal", function () {
			let vec_a = new Vector3D(2, 2, 2);
			let vec_b = new Vector3D(2, 2, 2);

			let are_equal = vec_a.equals(vec_b);

			equal(are_equal, true);
		});
	});
});
