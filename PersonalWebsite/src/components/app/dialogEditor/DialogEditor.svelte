<script>
    import {
        SvelteFlow,
        Controls,
        Background,
        BackgroundVariant,
        NodeToolbar,
    } from "@xyflow/svelte";
    import "@xyflow/svelte/dist/style.css";
    import { setContext } from "svelte";
    import { World } from "stella-ecs-js";

    import NodeContextMenu from "./contextMenus/NodeContextMenu.svelte";
    import EdgeContextMenu from "./contextMenus/EdgeContextMenu.svelte";
    import PaneContextMenu from "./contextMenus/PaneContextMenu.svelte"; 
    import ConditionNode from "./nodes/ConditionNode.svelte"; 
    import DialogNode from "./nodes/DialogNode.svelte";
    import AnnotationNode from "./nodes/AnnotationNode.svelte";
    import InstructionNode from './nodes/InstructionNode.svelte';
    import StateNode from './nodes/StateNode.svelte'; 
    import EventNode from './nodes/EventNode.svelte';

    const nodeTypes = {
        condition: ConditionNode,
        dialog: DialogNode,
        annotation: AnnotationNode,
        instruction: InstructionNode,
        state: StateNode,
        event: EventNode
    };

    let world = $state(new World());

    class Player {}
    class Strength {
        constructor(value) {
            this.Value = value;
        }
    }

    world.registerComponent(new Player());
    world.registerComponent(new Strength());

    let playerEntity = $state(
        world.createEntity().set(new Player()).set(new Strength(20)),
    );

    let dialogState = $state(new Map([
        ['hasMetGuard', false],
        ['playerGold', 50],
        ['playerStrength', 10]
    ]));


    let nodes = $state.raw([
        // 1. The conversation begins.
        {
            id: "intro",
            type: "input",
            position: { x: 50, y: -350 },
            data: { label: "Begin Conversation" },
        },
        // 2. The NPC poses the challenge.
        {
            id: "npc-greeting",
            type: "dialog",
            position: { x: 0, y: -200 },
            data: {
                menuText: "Start Conversation",
                speechText:
                    "Greetings, traveler. I need you to lift this heavy boulder. Are you strong enough?",
            },
        },
        {
            id: 'state-display',
            type: 'state',
            draggable: false,
            position: { x: 450, y: -200 },
            data: {} 
        },
        // 3A. The player chooses to act. This is a single choice.
        {
            id: "player-action-lift",
            type: "dialog",
            position: { x: -275, y: 100 },
            data: {
                menuText: "[ACT] Try to lift the boulder.",
                speechText:
                    "*You brace yourself and attempt to lift the massive rock...*",
            },
        },
        // 3B. The player chooses to leave.
        {
            id: "player-action-leave",
            type: "dialog",
            position: { x: 350, y: 100 },
            data: {
                menuText: "[LEAVE] I think I'll pass.",
                speechText: "I think I'll pass.",
            },
        },
        // 4. The player's action leads to a hidden condition check.
        {
            id: "strength-check",
            type: "condition",
            position: { x: -275, y: 450 },
            data: {
                mode: "builder",
                key: "playerStrength",
                operator: "GreaterThanOrEqual", // Storing the key of the enum
                value: "10",
                expression: "state.get('playerStrength') >= 10",
            },
        },
        // 5A. The outcome if the condition is true.
        {
            id: "outcome-strong",
            type: "dialog",
            position: { x: -450, y: 850 },
            data: {
                menuText: "",
                speechText: "Incredible! You lifted it with ease. Thank you!",
            },
        },
        // 5B. The outcome if the condition is false.
        {
            id: "outcome-weak",
            type: "dialog",
            position: { x: -50, y: 850 },
            data: {
                menuText: "",
                speechText:
                    "Hmm. It seems you need to train a bit more. Come back when you are stronger.",
            },
        },
        // 6. The conversation ends.
        {
            id: "end",
            type: "output",
            position: { x: 200, y: 1250 },
            data: { label: "End Conversation" },
        },
        {
            id: "annotation-1",
            type: "annotation",
            draggable: false, // Make annotations non-interactive
            position: { x: 100, y: 480 },
            data: {
                level: 1,
                label: "This node checks a condition.",
                arrowStyle:
                    "transform: rotate(180deg); left: -40px; top: -15px;",
            },
        },
        {
            id: "annotation-2",
            type: "annotation",
            draggable: false,
            position: { x: 100, y: 525 },
            data: {
                level: 2,
                label: "The outcome of the condition determines which dialog option is shown.",
            },
        },
        {
            id: "annotation-3",
            type: "annotation",
            draggable: false,
            position: { x: 50, y: 225 },
            data: {
                level: 1,
                label: "Right click to add new nodes.",
            },
        },
        {
            id: "annotation-4",
            type: "annotation",
            draggable: false,
            position: { x: 50, y: 265 },
            data: {
                level: 2,
                label: "Right click on a node to delete it.",
            },
        },
    ]);

    // The edges now reflect the new, more logical flow.
    let edges = $state.raw([
        { id: "e-intro-greeting", source: "intro", target: "npc-greeting" },
        // The NPC greeting presents two choices to the player.
        {
            id: "e-greeting-lift",
            source: "npc-greeting",
            target: "player-action-lift",
        },
        {
            id: "e-greeting-leave",
            source: "npc-greeting",
            target: "player-action-leave",
        },
        // The "lift" action leads to the strength check.
        {
            id: "e-lift-check",
            source: "player-action-lift",
            target: "strength-check",
        },
        // The check's outcomes lead to different NPC responses.
        {
            id: "e-check-strong",
            source: "strength-check",
            sourceHandle: "true-output",
            target: "outcome-strong",
        },
        {
            id: "e-check-weak",
            source: "strength-check",
            sourceHandle: "false-output",
            target: "outcome-weak",
        },
        // All paths eventually lead to the end.
        { id: "e-strong-end", source: "outcome-strong", target: "end" },
        { id: "e-weak-end", source: "outcome-weak", target: "end" },
        { id: "e-leave-end", source: "player-action-leave", target: "end" },
    ]);

    let nodeMenu = $state(null);
    let edgeMenu = $state(null);
    let paneMenu = $state(null);

    const handleNodeContextMenu = ({ event, node }) => {
        event.preventDefault();
        const paneBounds = event.target
            .closest(".svelte-flow")
            .getBoundingClientRect();
        nodeMenu = {
            id: node.id,
            top:
                event.clientY < paneBounds.height - 200
                    ? event.clientY
                    : undefined,
            left:
                event.clientX < paneBounds.width - 200
                    ? event.clientX
                    : undefined,
            right:
                event.clientX >= paneBounds.width - 200
                    ? paneBounds.width - event.clientX
                    : undefined,
            bottom:
                event.clientY >= paneBounds.height - 200
                    ? paneBounds.height - event.clientY
                    : undefined,
        };
    };

    const handleEdgeContextMenu = ({ event, edge }) => {
        event.preventDefault();
        const paneBounds = event.target
            .closest(".svelte-flow")
            .getBoundingClientRect();
        edgeMenu = {
            id: edge.id,
            top:
                event.clientY < paneBounds.height - 200
                    ? event.clientY
                    : undefined,
            left:
                event.clientX < paneBounds.width - 200
                    ? event.clientX
                    : undefined,
            right:
                event.clientX >= paneBounds.width - 200
                    ? paneBounds.width - event.clientX
                    : undefined,
            bottom:
                event.clientY >= paneBounds.height - 200
                    ? paneBounds.height - event.clientY
                    : undefined,
        };
    };

    // 3. Add a handler for the pane's context menu
    const handlePaneContextMenu = ({ event }) => {
        event.preventDefault();

        paneMenu = {
            top: event.clientY,
            left: event.clientX,
            clientX: event.clientX,
            clientY: event.clientY,
        };
    };

    function handlePaneClick() {
        nodeMenu = null;
        edgeMenu = null;
        paneMenu = null;
    }
</script>

<div style="width: 100%; height: 100%;">
    <SvelteFlow
        {nodeTypes}
        bind:nodes
        bind:edges
        fitView
        onnodecontextmenu={handleNodeContextMenu}
        onedarticy
        draft
        X
        gecontextmenu={handleEdgeContextMenu}
        onpanecontextmenu={handlePaneContextMenu}
        onpaneclick={handlePaneClick}
    >
        <Background variant={BackgroundVariant.Dots} />
        <Controls />

        {#if nodeMenu}
            <NodeContextMenu
                id={nodeMenu.id}
                top={nodeMenu.top}
                left={nodeMenu.left}
                right={nodeMenu.right}
                bottom={nodeMenu.bottom}
                onclick={handlePaneClick}
            />
        {/if}

        {#if edgeMenu}
            <EdgeContextMenu
                id={edgeMenu.id}
                top={edgeMenu.top}
                left={edgeMenu.left}
                right={edgeMenu.right}
                bottom={edgeMenu.bottom}
                onclick={handlePaneClick}
            />
        {/if}

        <!-- 5. Conditionally render the new PaneContextMenu -->
        {#if paneMenu}
            <PaneContextMenu
                top={paneMenu.top}
                left={paneMenu.left}
                clientX={paneMenu.clientX}
                clientY={paneMenu.clientY}
                onclick={handlePaneClick}
            />
        {/if}
    </SvelteFlow>
</div>
