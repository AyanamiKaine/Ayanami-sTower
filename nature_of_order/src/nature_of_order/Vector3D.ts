export class Vector3D {
	readonly x: number;
	readonly y: number;
	readonly z: number;
	constructor(x: number = 0, y: number = 0, z: number = 0) {
		this.x = x;
		this.y = y;
		this.z = z;
	}

	equals(OtherVector3D: Vector3D) {
		return (
			this.x == OtherVector3D.x &&
			this.y == OtherVector3D.y &&
			this.z == OtherVector3D.z
		);
	}

	add(otherVector3D: Vector3D): Vector3D {
		return new Vector3D(
			this.x + otherVector3D.x,
			this.y + otherVector3D.y,
			this.z + otherVector3D.z
		);
	}
}
