import { PlayerResponse } from './PlayerResponse';

export class DialogueNode {
    /**
     * @param {string} id                        - A unique identifier for this node, e.g., 'marena_intro_1'.
     * @param {string} npcText                   - The text the NPC says at this node. Can support variables.
     * @param {PlayerResponse[]} playerResponses - A list of potential responses the player can choose.
     */
    constructor(id, npcText, playerResponses = []) {
        this.id = id;
        this.npcText = npcText;
        this.playerResponses = playerResponses;
    }
}