import { FancyButton } from "@pixi/ui";
import { animate } from "motion";
import type { AnimationPlaybackControls } from "motion/react";
import type { FederatedPointerEvent, FederatedWheelEvent, Ticker } from "pixi.js";
import { Container, Graphics, Point, Rectangle } from "pixi.js";

import { engine } from "../../getEngine";
import { PausePopup } from "../../popups/PausePopup";
import { SettingsPopup } from "../../popups/SettingsPopup";
import { ContextMenu, type ContextMenuOption } from "../../ui/ContextMenu";
import { Game } from "../../../game/game";
import { StarSystem } from "../../ui/StarSystem";

/** The screen that holds the app */
export class MainScreen extends Container {
  /** Assets bundles required by this screen */
  public static assetBundles = ["main"];


  public mainContainer: Container;
  private pauseButton: FancyButton;
  private settingsButton: FancyButton;
  private paused = false;
  private contextMenu: ContextMenu; // <-- Add a property for the context menu
  private game: Game;

  private readonly ZOOM_FACTOR = 1.1;
  private readonly MIN_ZOOM = 0.2;
  private readonly MAX_ZOOM = 5.0;

  // --- Panning Properties ---
  private isPanning = false;
  private lastPanPosition = new Point();


  constructor() {
    super();
    this.game = new Game();
    this.mainContainer = new Container();
    this.addChild(this.mainContainer);
    this.mainContainer.hitArea = new Rectangle(-10000, -10000, 20000, 20000); // <-- Make hit area larger to cover the screen

    this.mainContainer.eventMode = 'static';

    this.mainContainer.on('wheel', this.onWheelScroll);
    this.mainContainer.on('pointerdown', this.onPointerDown);
    this.mainContainer.on('pointerup', this.onPointerUp);
    this.mainContainer.on('pointerupoutside', this.onPointerUp);
    this.mainContainer.on('pointermove', this.onPointerMove);

    // Right-click event to show the context menu
    this.mainContainer.on('rightclick', (event) => {
      event.preventDefault();
      // Define the menu options using the new interface
      const menuOptions: ContextMenuOption[] = [
        {
          label: 'Add Star System', action: 'add-circle', callback: () => {

            // 1. Create an instance of our new StarSystem class.
            //    We pass it our existing contextMenu instance.
            const star = new StarSystem(this.contextMenu);

            // 2. Get the correct local position (as we did before).
            const localPosition = this.mainContainer.toLocal(event.global);
            star.position.set(localPosition.x, localPosition.y);

            // 3. Add the fully interactive star to the container.
            this.mainContainer.addChild(star);
          }, icon: '🌣'
        },
      ];

      // The show call remains the same!
      this.contextMenu.show(event.global.x, event.global.y, menuOptions);
    });

    this.mainContainer.on('pointerdown', () => {
      this.contextMenu.hide();
    });

    const buttonAnimations = {
      hover: {
        props: {
          scale: { x: 1.1, y: 1.1 },
        },
        duration: 100,
      },
      pressed: {
        props: {
          scale: { x: 0.9, y: 0.9 },
        },
        duration: 100,
      },
    };
    this.pauseButton = new FancyButton({
      defaultView: "icon-pause.png",
      anchor: 0.5,
      animations: buttonAnimations,
    });
    this.pauseButton.onPress.connect(() =>
      engine().navigation.presentPopup(PausePopup),
    );
    this.addChild(this.pauseButton);

    this.settingsButton = new FancyButton({
      defaultView: "icon-settings.png",
      anchor: 0.5,
      animations: buttonAnimations,
    });
    this.settingsButton.onPress.connect(() =>
      engine().navigation.presentPopup(SettingsPopup),
    );
    this.addChild(this.settingsButton);


    // Create and add the context menu to the stage
    this.contextMenu = new ContextMenu();
    this.addChild(this.contextMenu);
  }

  /** Prepare the screen just before showing */
  public prepare() { }

  /** Update the screen */
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  public update(_time: Ticker) {
    if (this.paused) return;
  }

  /** Pause gameplay - automatically fired when a popup is presented */
  public async pause() {
    this.mainContainer.interactiveChildren = false;
    this.paused = true;
  }

  /** Resume gameplay */
  public async resume() {
    this.mainContainer.interactiveChildren = true;
    this.paused = false;
  }

  /** Fully reset */
  public reset() { }

  private onPointerDown = (event: FederatedPointerEvent) => {
    // Handle panning with middle mouse button
    if (event.button === 0 && event.ctrlKey) {
      this.isPanning = true;
      this.lastPanPosition.copyFrom(event.global);
      this.mainContainer.cursor = 'move';
      // No need to prevent default here as Ctrl+Click doesn't have a strong default browser action.
      return; // Stop processing other click events
    }

    // Handle right-click for context menu
    if (event.button === 2) {
      event.preventDefault();
      const menuOptions: ContextMenuOption[] = [
        {
          label: 'Add Star System', action: 'add-circle', callback: () => {
            const star = new StarSystem(this.contextMenu);
            const localPosition = this.mainContainer.toLocal(event.global);
            star.position.set(localPosition.x, localPosition.y);
            this.mainContainer.addChild(star);
          }, icon: '🌣'
        },
      ];
      this.contextMenu.show(event.global.x, event.global.y, menuOptions);
    }

    // Hide context menu on left click
    if (event.button === 0) {
      this.contextMenu.hide();
    }
  }

  private onPointerUp = (event: FederatedPointerEvent) => {
    // --- THIS IS THE MODIFIED LOGIC ---
    // Stop panning if the left mouse button is released, and we were panning
    if (event.button === 0 && this.isPanning) {
      this.isPanning = false;
      this.mainContainer.cursor = 'default';
    }
  }

  private onPointerMove = (event: FederatedPointerEvent) => {
    // Move the container if panning is active
    if (this.isPanning) {
      const dx = event.global.x - this.lastPanPosition.x;
      const dy = event.global.y - this.lastPanPosition.y;

      this.mainContainer.x += dx;
      this.mainContainer.y += dy;

      this.lastPanPosition.copyFrom(event.global);
    }
  }

  /**
   * Handles zooming the main container based on the mouse wheel.
   * This version uses a direct calculation to ensure the point under the cursor remains stationary.
   * @param event The federated wheel event from PixiJS.
   */
  private onWheelScroll = (event: FederatedWheelEvent) => {
    event.preventDefault();

    const scroll = event.deltaY;
    if (scroll === 0) return;

    // --- Part 1: Calculate the new scale (No changes here) ---
    const zoomDirection = scroll < 0 ? this.ZOOM_FACTOR : 1 / this.ZOOM_FACTOR;
    const currentScale = this.mainContainer.scale.x;
    let newScale = currentScale * zoomDirection;
    newScale = Math.max(this.MIN_ZOOM, Math.min(newScale, this.MAX_ZOOM));

    if (newScale === currentScale) return;

    // --- Part 2: The Corrected Repositioning Logic ---

    // 1. Get the mouse's position in the world (the mainContainer's local space) BEFORE the zoom.
    const mousePositionInWorld = this.mainContainer.toLocal(event.global);

    // 2. Apply the new scale to the container.
    this.mainContainer.scale.set(newScale);

    // 3. Directly calculate the new position of the container.
    // The formula is: new_camera_pos = mouse_screen_pos - (mouse_world_pos * new_scale)
    const newPosX = event.global.x - (mousePositionInWorld.x * newScale);
    const newPosY = event.global.y - (mousePositionInWorld.y * newScale);

    this.mainContainer.position.set(newPosX, newPosY);
  }

  /** Resize the screen, fired whenever window size changes */
  public resize(width: number, height: number) {
    const centerX = width * 0.5;
    const centerY = height * 0.5;

    // Only set initial position if it hasn't been panned/zoomed yet
    if (this.mainContainer.x === 0 && this.mainContainer.y === 0) {
      this.mainContainer.x = centerX;
      this.mainContainer.y = centerY;
    }

    this.pauseButton.x = 30;
    this.pauseButton.y = 30;
    this.settingsButton.x = width - 30;
    this.settingsButton.y = 30;
  }

  /** Show screen with animations */
  public async show(): Promise<void> {
    const elementsToAnimate = [
      this.pauseButton,
      this.settingsButton,
    ];

    let finalPromise!: AnimationPlaybackControls;
    for (const element of elementsToAnimate) {
      element.alpha = 0;
      finalPromise = animate(
        element,
        { alpha: 1 },
        { duration: 0.3, delay: 0.75, ease: "backOut" },
      );
    }

    await finalPromise;
  }

  /** Hide screen with animations */
  public async hide() { }

  /** Auto pause the app when window go out of focus */
  public blur() {
    if (!engine().navigation.currentPopup) {
      engine().navigation.presentPopup(PausePopup);
    }
  }
}
