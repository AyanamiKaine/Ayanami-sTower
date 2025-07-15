import { Criteria } from "sfpm-js";

export class PlayerResponse {
    /**
     * @param {string} text                 - The text displayed to the player for this option.
     * @param {string} nextNodeId           - The ID of the dialogue node to transition to.
     * @param {Function} [action=null]      - An optional function to execute that modifies the game state.
     * @param {Criteria[]} [conditions=[]]  - Optional criteria that must be met for this response to be available.
     */
    constructor(text, nextNodeId, action = null, conditions = []) {
        this.text = text;
        this.nextNodeId = nextNodeId;   // e.g., 'marena_intro_2'
        this.action = action;           // e.g., (gameState) => gameState.set('knowsMarena', true)
        this.conditions = conditions;   // e.g., [new Criteria('playerReputation', 50, Operator.GreaterThan)]
    }
}
