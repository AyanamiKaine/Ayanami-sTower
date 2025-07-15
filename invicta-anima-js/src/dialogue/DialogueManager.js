import { Query } from 'sfpm-js';

export class DialogueManager {
    constructor(game) {
        this.allNodes = new Map(); // Stores all dialogue nodes, keyed by their ID.
        this.activeNode = null;
        this.game = game; // A reference to the game's state map.
    }

    /**
     * Loads a collection of dialogue nodes into the manager.
     * @param {DialogueNode[]} nodes
     */
    load(nodes) {
        for (const node of nodes) {
            this.allNodes.set(node.id, node);
        }
    }

    /**
     * Starts a dialogue from a specific entry node.
     * @param {string} nodeId
     */
    startDialogue(nodeId) {
        if (this.allNodes.has(nodeId)) {
            this.activeNode = this.allNodes.get(nodeId);
        } else {
            console.error(`Dialogue node with ID "${nodeId}" not found.`);
            this.activeNode = null;
        }
    }

    /**
     * Ends the current dialogue.
     */
    endDialogue() {
        this.activeNode = null;
        this.gameState.set('talkingTo', null);
    }

    /**
     * Returns the currently available player options based on the active node and game state.
     * @returns {Map<number, object>} A map of options ready to be used by the Game class.
     */
    getCurrentOptions() {
        if (!this.activeNode) {
            return new Map();
        }

        const availableResponses = new Map();
        let optionIndex = 1;

        // The query object's fact source is what we need to pass to the criteria.
        // Your Query class correctly creates a DictionaryFactSource internally.
        const query = new Query(this.game.gameState);
        const factSource = query._factSource; // Get the underlying fact source
        //console.log(factSource)
        for (const response of this.activeNode.playerResponses) {
            // =======================================================================
            // THE FIX IS HERE
            // =======================================================================
            // We now correctly evaluate each criteria and use its direct boolean result.
            const isAvailable = response.conditions.every(criteria => {
                return criteria.evaluate(factSource);
            });
            // =======================================================================

            if (isAvailable) {
                availableResponses.set(optionIndex, {
                    text: response.text,
                    // The action now handles both the state change AND the dialogue transition
                    action: () => {
                        // 1. Execute the response's specific action, if any
                        if (response.action) {
                            response.action(this.game);
                        }
                        // 2. Transition to the next node
                        if (response.nextNodeId) {
                            this.activeNode = this.allNodes.get(response.nextNodeId);
                        } else {
                            // If no next node, the conversation ends
                            this.endDialogue();
                        }
                    }
                });
                optionIndex++;
            }
        }
        return availableResponses;
    }
}