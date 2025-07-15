// A file where you define your dialogue content, e.g., src/data/marena_dialogue.js
import { Criteria, Operator } from 'sfpm-js';
import { DialogueNode } from '../dialogue/DialogueNode';
import { PlayerResponse } from '../dialogue/PlayerResponse';

export const marenaDialogue = [
    new DialogueNode('marena_intro_start', 'A woman with cybernetic eyes looks up as you approach. "Can I help you?"', [
        new PlayerResponse(
            "[TALK] Who are you?",
            'marena_intro_reveal_name',
            (game) => game.gameState.set('knowsMarena', true), // Action to perform
            [new Criteria('knowsMarena', false, Operator.Equal)] // Condition for this option
        ),
        new PlayerResponse(
            "[TALK] Hey Marena, how are you?",
            'marena_intro_friendly_chat',
            null, // No special action
            [new Criteria('knowsMarena', true, Operator.Equal)] // Only show if you know her
        ),
        new PlayerResponse(
            "[LEAVE] I'm leaving.",
            null, // No next node, so this will end the dialogue
        ),
    ]),

    new DialogueNode('marena_intro_reveal_name', '"The name is Marena. I run the tech shop here. Now, you know my name. What\'s yours?"', [
        new PlayerResponse(
            "[TALK] I'm just a traveler.",
            'marena_intro_end_generic'
        ),
        new PlayerResponse(
            "[TALK] My name is [PlayerName].", // You could add text replacement
            'marena_intro_end_friendly'
        ),
    ]),
];