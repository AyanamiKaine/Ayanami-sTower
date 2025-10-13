// Import the .NET runtime for WebAssembly
import { dotnet } from "./_framework/dotnet.js";

let exports;
let initialized = false;

// DOM elements
const outputDiv = document.getElementById("output");
const inputField = document.getElementById("input");
const promptSymbol = document.getElementById("promptSymbol");

// Initialize the .NET runtime and the FORTH interpreter
async function init() {
    try {
        // Load the .NET WebAssembly runtime
        const { getAssemblyExports, getConfig } = await dotnet
            .withDiagnosticTracing(false)
            .withApplicationArgumentsFromQuery()
            .create();

        // Get the config to verify it loaded
        const config = getConfig();

        // Get the exported methods from our C# Program class
        exports = await getAssemblyExports(config.mainAssemblyName);

        // Initialize the FORTH interpreter
        const welcomeMessage =
            exports.StellaLang.REPL.Program.InitializeInterpreter();

        // Clear loading message and show welcome
        outputDiv.innerHTML = "";
        addOutput(welcomeMessage, "success");
        addOutput("", "normal");
        addOutput("Type FORTH commands and press Enter to execute.", "normal");
        addOutput("Try: 5 3 + . (should print 8)", "command-example");
        addOutput("Try: : SQUARE DUP * ; (define a word)", "command-example");
        addOutput("Try: 7 SQUARE . (should print 49)", "command-example");
        addOutput("", "normal");

        // Enable input
        inputField.disabled = false;
        inputField.focus();
        initialized = true;

        // Show initial prompt
        showPrompt();
    } catch (error) {
        outputDiv.innerHTML = "";
        addOutput(`ERROR: Failed to initialize WebAssembly runtime`, "error");
        addOutput(error.toString(), "error");
        console.error("Initialization error:", error);
    }
}

// Add output to the display
function addOutput(text, className = "normal") {
    const line = document.createElement("div");
    line.className = `output-line ${className}`;
    line.textContent = text;
    outputDiv.appendChild(line);

    // Auto-scroll to bottom
    outputDiv.scrollTop = outputDiv.scrollHeight;
}

// Show the prompt in the output
function showPrompt() {
    const promptLine = document.createElement("div");
    promptLine.className = "output-line";

    const promptSpan = document.createElement("span");
    promptSpan.className = "prompt";
    promptSpan.textContent = "> ";

    promptLine.appendChild(promptSpan);
    outputDiv.appendChild(promptLine);
    outputDiv.scrollTop = outputDiv.scrollHeight;
}

// Process user input
function processInput() {
    if (!initialized) {
        return;
    }

    const input = inputField.value.trim();

    if (input === "") {
        return;
    }

    // Show the input in the output
    const lastPrompt = outputDiv.querySelector(".output-line:last-child");
    if (lastPrompt && lastPrompt.querySelector(".prompt")) {
        const inputSpan = document.createElement("span");
        inputSpan.textContent = input;
        lastPrompt.appendChild(inputSpan);
    }

    // Clear input field
    inputField.value = "";

    try {
        // Process the input through the FORTH interpreter
        const result = exports.StellaLang.REPL.Program.ProcessInput(input);

        // Display the result
        if (result && result.trim() !== "") {
            const lines = result.split("\n");
            for (const line of lines) {
                if (line.trim() !== "") {
                    // Check if it's an error message
                    const className =
                        line.includes("ERROR") || line.includes("Error")
                            ? "error"
                            : "normal";
                    addOutput(line, className);
                }
            }
        }
    } catch (error) {
        addOutput(`JavaScript Error: ${error.toString()}`, "error");
        console.error("Execution error:", error);
    }

    // Show next prompt
    showPrompt();
}

// Set up event listeners
inputField.addEventListener("keydown", (event) => {
    if (event.key === "Enter") {
        event.preventDefault();
        processInput();
    }
});

// Handle Ctrl+C to clear input
inputField.addEventListener("keydown", (event) => {
    if (event.ctrlKey && event.key === "c") {
        event.preventDefault();
        inputField.value = "";
    }
});

// Initialize when the page loads
init();
