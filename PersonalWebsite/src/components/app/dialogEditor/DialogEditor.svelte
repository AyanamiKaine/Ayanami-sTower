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
    import EntryNode from './nodes/EntryNode.svelte';
    import CommentNode from './nodes/CommentNode.svelte';
    import { generateSourceCode } from './CodeGenerator';
    import CodeDisplayModal from './CodeGeneratorModel.svelte';

    let generatedCode = $state(null);

    /**
     * Handles the 'Generate Code' button click. It snapshots the current graph
     * state and calls the generator service.
     */
    function handleGenerateCode() {
        // Use $state.snapshot to get the plain JS object/array values
        const currentNodes = $state.snapshot(nodes);
        const currentEdges = $state.snapshot(edges);
        generatedCode = generateSourceCode(currentNodes, currentEdges);
    }


      /**
     * Closes the code display modal by resetting the state.
     */
    function closeCodeModal() {
        generatedCode = null;
    }

    const nodeTypes = {
        entry: EntryNode,
        condition: ConditionNode,
        dialog: DialogNode,
        annotation: AnnotationNode,
        instruction: InstructionNode,
        state: StateNode,
        event: EventNode,
        comment: CommentNode
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
    {
            id: "entry-point",
            type: "entry",
            position: { x: 0, y: -650 },
            data: {
                dialogId: "boulder_strength_check"
            },
        },
        // 2. The NPC poses the challenge.
        {
            id: "npc-greeting",
            type: "dialog",
            position: { x: 0, y: -300 },
            data: {
                speaker: "generic_npc",
                menuText: "Start Conversation",
                speechText:
                    "Greetings, traveler. I need you to lift this heavy boulder. Are you strong enough?",
            },
        },
        {
            id: 'comment-example',
            type: 'comment',
            position: { x: -450, y: -200 },
            data: {
                comment: "Adding comments is easy, they are also automatically added to the generated source code."
            } 
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
            position: { x: -275, y: 150 },
            data: {
                speaker: "Player",
                menuText: "[ACT] Try to lift the boulder.",
                speechText:
                    "*You brace yourself and attempt to lift the massive rock...*",
            },
        },
        // 3B. The player chooses to leave.
        {
            id: "player-action-leave",
            type: "dialog",
            position: { x: 450, y: 150 },
            data: {
                speaker: "Player",
                menuText: "[LEAVE] I think I'll pass.",
                speechText: "I think I'll pass.",
            },
        },
        {
            id: "event-trigger",
            type: "event",
            position: { x: 650, y: 600 },
            data: {
                eventName: "passedOnBoulder",
            },
        },
        {
            id: "instruction",
            type: "instruction",
            position: { x: 450, y: 850 },
            data: {
            },
        },
        // 4. The player's action leads to a hidden condition check.
        {
            id: "strength-check",
            type: "condition",
            position: { x: -275, y: 550 },
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
            position: { x: -450, y: 950 },
            data: {
                menuText: "",
                speechText: "Incredible! You lifted it with ease. Thank you!",
            },
        },
        // 5B. The outcome if the condition is false.
        {
            id: "outcome-weak",
            type: "dialog",
            position: { x: -50, y: 950 },
            data: {
                speaker: "generic_npc",
                menuText: "",
                speechText:
                    "Hmm. It seems you need to train a bit more. Come back when you are stronger.",
            },
        },
        {
            id: "annotation-1",
            type: "annotation",
            draggable: false, // Make annotations non-interactive
            position: { x: 100, y: 580 },
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
            position: { x: 100, y: 625 },
            data: {
                level: 2,
                label: "The outcome of the condition determines which dialog option is shown.",
            },
        },
        {
            id: "annotation-3",
            type: "annotation",
            draggable: false,
            position: { x: 100, y: 225 },
            data: {
                level: 1,
                label: "Right click to add new nodes.",
            },
        },
        {
            id: "annotation-4",
            type: "annotation",
            draggable: false,
            position: { x: 100, y: 265 },
            data: {
                level: 2,
                label: "Right click on a node to delete it.",
            },
        },
        {
            id: "annotation-5",
            type: "annotation",
            draggable: false,
            position: { x: -760, y: 1045 },
            data: {
                level: 1,
                label: "Having non speaker is generally an error.",
            },
        },
    ]);

    // The edges now reflect the new, more logical flow.
    let edges = $state.raw([
        { id: "e-intro-greeting", source: "entry-point", target: "npc-greeting" },
        // The NPC greeting presents two choices to the player.
        {
            id: "e-greeting-lift",
            source: "npc-greeting",
            target: "player-action-lift",
        },
        {
            id: "e-comment",
            source: "comment-example",
            target: "player-action-lift",
        },
        {
            id: "e-greeting-leave",
            source: "npc-greeting",
            target: "player-action-leave",
        },
        {
            id: "e-event-trigger",
            source: "player-action-leave",
            target: "event-trigger",
        },
        {
            id: "e-event-trigger-2",
            source: "event-trigger",
            target: "instruction",
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
        }
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

<div class="ui-layer">
    <div class="top-controls">
        <button class="generate-btn" onclick={handleGenerateCode}>
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="16 18 22 12 16 6"></polyline><polyline points="8 6 2 12 8 18"></polyline></svg>
            Generate Code
        </button>
    </div>

    {#if generatedCode}
        <CodeDisplayModal {generatedCode} onClose={closeCodeModal} />
    {/if}
</div>
  

<div style="width: 100%; height: 100%;">    
    <SvelteFlow
        {nodeTypes}
        bind:nodes
        bind:edges
        minZoom={0.1}
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

<style>
    .ui-layer {
        position: absolute;
        top: 15px;
        right: 15px;
        z-index: 10; /* SvelteFlow default is 4 */
    }
    .generate-btn {
        background-color: #1a1a1a;
        color: #fff;
        border: 1px solid #444;
        border-radius: 6px;
        padding: 10px 15px;
        font-size: 14px;
        font-weight: 600;
        cursor: pointer;
        display: flex;
        align-items: center;
        gap: 8px;
        transition: background-color 0.2s;
    }
    .generate-btn:hover {
        background-color: #333;
    }
</style>