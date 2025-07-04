import { Container, NineSliceSprite, Texture } from "pixi.js";

const defaultRoundedBoxOptions = {
  color: 0xffffff,
  width: 350,
  height: 600,
  shadow: true,
  shadowColor: 0xa0a0a0,
  shadowOffset: 22,
};

/**
 * Generic rounded box based on a nine-sliced sprite that can be resized freely.
 */
export class RoundedBox extends Container {
  /** The rectangular area, that scales without distorting rounded corners */
 image;
  /** Optional shadow matching the box image, with y offest */
  shadow;

  constructor(options = {}) {
    super();
    const opts = { ...defaultRoundedBoxOptions, ...options };
    this.image = new NineSliceSprite({
      texture: Texture.from("rounded-rectangle.png"),
      leftWidth: 34,
      topHeight: 34,
      rightWidth: 34,
      bottomHeight: 34,
      width: opts.width,
      height: opts.height,
      tint: opts.color,
    });
    this.image.x = -this.image.width * 0.5;
    this.image.y = -this.image.height * 0.5;
    this.addChild(this.image);

    if (opts.shadow) {
      this.shadow = new NineSliceSprite({
        texture: Texture.from("rounded-rectangle.png"),
        leftWidth: 34,
        topHeight: 34,
        rightWidth: 34,
        bottomHeight: 34,
        width: opts.width,
        height: opts.height,
        tint: opts.shadowColor,
      });
      this.shadow.x = -this.shadow.width * 0.5;
      this.shadow.y = -this.shadow.height * 0.5 + opts.shadowOffset;
      this.addChildAt(this.shadow, 0);
    }
  }

  /** Get the base width, without counting the shadow */
  get boxWidth() {
    return this.image.width;
  }

  /** Get the base height, without counting the shadow */
  get boxHeight() {
    return this.image.height;
  }
}
