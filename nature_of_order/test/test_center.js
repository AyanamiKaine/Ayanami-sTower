import { equal } from "assert";
import { Center } from "../build/nature_of_order/Center.js";
import { Tags } from "../build/nature_of_order/Tag.js";
import { Vector3D } from "../build/nature_of_order/Vector3D.js";
describe("Center", function () {
	describe("Enumerate of the centers field of a center", function () {
		it("it should enumerate of the centers of a center", function () {
			let main_center = new Center();
			let sec_center1 = new Center();
			let sec_center2 = new Center();
			let sec_center3 = new Center();
			main_center.addCenter(sec_center1);
			main_center.addCenter(sec_center2);
			main_center.addCenter(sec_center3);
			let sum = 0;

			for (const centers of main_center) {
				sum += 1;
			}

			equal(sum, 3);
			equal(sec_center1.parentCenter, main_center);
		});
	});

	describe("A center has a position in space itself", function () {
		it("a center is more related to another if they are closer together", function () {
			let main_center = new Center();
			let sec_center1 = new Center();

			//equal(sum, 3);
		});
	});

	describe("A center has a collection of tags that are symbols, symbols are unique", function () {
		it("add a tag to a center", function () {
			let center = new Center();
			let tags = new Tags();

			center.addTag(tags.addTag("Enemy"));
			equal(center.tags[0] === tags.getTag("Enemy"), true);
		});
	});

	describe("A centers positition is always defined by its relation to its parent position", function () {
		it("if a center has now parent global position is assumed, if it has one, the global position is parentPos + childPos", function () {
			const parrentCenter = new Center({ position: new Vector3D(2, 2, 2) });
			const childCenter = new Center({ position: new Vector3D(2, 2, 2) });

			parrentCenter.addCenter(childCenter);

			const expectedVector = new Vector3D(4, 4, 4);
			equal(expectedVector.equals(childCenter.globalPosition), true);
		});
	});

	describe("A centers holds many other centers and know how many", function () {
		it("length property of a center returns the number of child centers", function () {
			const parrentCenter = new Center({ position: new Vector3D(2, 2, 2) });
			const childCenter = new Center({ position: new Vector3D(2, 2, 2) });

			parrentCenter.addCenter(childCenter);

			const expectedLength = 1;
			equal(parrentCenter.size, expectedLength);
		});
	});
});
