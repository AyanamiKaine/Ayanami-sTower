import { World } from "stella-ecs-js";
import { Character } from "./components/Character";
import { Name } from "./components/Name";
import { Age } from "./components/Age";
import { Attribute } from "./components/Attribute";
import { Trait } from "./components/Trait";

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
    Tick() {
        this.world.update();
        this.currentTick += 1;
    }

    UpdateCurrentPlayerOptions(query) {
        query.match(this.currentGameRules);
    }

    ShowCurrentPlayerOptions() {
        console.log(this.currentPlayerOptions);
    }

    AddScenario(scenario) {
        this.scenarios.set(scenario.name, scenario.rules);
    }

    RemoveScenario(scenario) {
        this.scenarios.delete(scenario.name);
    }
    RemoveScenarioByName(scenarioName) {
        this.scenarios.delete(scenarioName);
    }
}
