import { World } from "stella-ecs-js";
import { Character } from "./components/Character";
import { Name } from "./components/Name";
import { Age } from "./components/Age";
import { Attribute } from "./components/Attribute";
import { Trait } from "./components/Trait";
import { DialogueManager } from './dialogue/DialogueManager';

export class Game {
    constructor(parameters) {
        this.world = new World();
        this.currentTick = 0;
        /*
        Indicates what current options the player has, each options comes with a text
        showing what the player sees, and a action lambda that executes it. Changing the 
        game world. As input an action has the game world.
        */
        this.currentPlayerOptions = new Map();
        this.currentGameRules = [];
        this.scenarios = new Map();

        this.gameState = new Map();
        this.dialogueManager = new DialogueManager(this.gameState);


        this.world.registerComponent(Character);
        this.world.registerComponent(Name);
        this.world.registerComponent(Age);
        this.world.registerComponent(Attribute);
        this.world.registerComponent(Trait);
    }

    /**
     * Progresses the game world by one tick. Every action incurs a tick, the game world does not
     * stand still when the player is in a dialog or does something. NPCs have a life they follow.
     * They have goals and desires to fullfill. Some of those desires and goals will have dire
     * consequnces in the world.
     */
    tick() {
        this.world.update();
        this.currentTick += 1;
    }

    updateCurrentPlayerOptions() {
        if (this.dialogueManager.activeNode) {
            // If we are in a conversation, get options from the dialogue manager
            this.currentPlayerOptions = this.dialogueManager.getCurrentOptions(); // ERROR currentPlayerOptions is not defined
            //console.log(this.currentPlayerOptions)

        } else {
            // Otherwise, run the global rules for non-dialogue actions
            const query = new Query(this.gameState);
            this.currentPlayerOptions.clear(); // Clear old options
            query.match(this.currentGameRules, this); // The payload will populate the map
        }
        //console.debug(this.gameState)
    }

    showCurrentPlayerOptions() {
        console.log(this.currentPlayerOptions);
    }

    addScenario(scenario) {
        this.scenarios.set(scenario.name, scenario.rules);
    }

    removeScenario(scenario) {
        this.scenarios.delete(scenario.name);
    }

    removeScenarioByName(scenarioName) {
        this.scenarios.delete(scenarioName);
    }

    selectPlayerOption(optionIndex) {
        const selectedOption = this.currentPlayerOptions.get(optionIndex);
        if (selectedOption && typeof selectedOption.action === "function") {
            selectedOption.action();
            this.tick();
            this.updateCurrentPlayerOptions();
        } else {
            console.error(`Error: Invalid option index "${optionIndex}". No action found.`);
        }
    }
}
