export class Vector3D {
    constructor(x = 0, y = 0, z = 0) {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    equals(OtherVector3D) {
        return (this.x == OtherVector3D.x &&
            this.y == OtherVector3D.y &&
            this.z == OtherVector3D.z);
    }
    add(otherVector3D) {
        return new Vector3D(this.x + otherVector3D.x, this.y + otherVector3D.y, this.z + otherVector3D.z);
    }
}
