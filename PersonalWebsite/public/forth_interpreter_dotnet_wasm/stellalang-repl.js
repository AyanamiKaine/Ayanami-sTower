/**
 * StellaLang REPL - JavaScript Library
 * A WebAssembly-powered FORTH interpreter that can be embedded in any web application.
 *
 * @example
 * ```javascript
 * import { StellaLangREPL } from './stellalang-repl.js';
 *
 * const repl = new StellaLangREPL({
 *   wasmPath: './_framework/dotnet.js',
 *   onOutput: (text) => console.log(text),
 *   onError: (error) => console.error(error),
 *   onReady: () => console.log('REPL ready!')
 * });
 *
 * await repl.initialize();
 * const result = await repl.execute('5 3 + .');
 * ```
 */

export class StellaLangREPL {
    /**
     * Creates a new StellaLang REPL instance
     * @param {Object} options - Configuration options
     * @param {string} options.wasmPath - Path to the dotnet.js file (default: './_framework/dotnet.js')
     * @param {Function} options.onOutput - Callback for output messages
     * @param {Function} options.onError - Callback for error messages
     * @param {Function} options.onReady - Callback when REPL is initialized
     * @param {boolean} options.debugMode - Enable diagnostic tracing (default: false)
     */
    constructor(options = {}) {
        this.options = {
            wasmPath: "./_framework/dotnet.js",
            onOutput: null,
            onError: null,
            onReady: null,
            debugMode: false,
            ...options,
        };

        this.exports = null;
        this.initialized = false;
        this.initPromise = null;
    }

    /**
     * Initializes the WebAssembly runtime and FORTH interpreter
     * @returns {Promise<string>} Welcome message from the interpreter
     */
    async initialize() {
        // Prevent multiple simultaneous initializations
        if (this.initPromise) {
            return this.initPromise;
        }

        this.initPromise = this._doInitialize();
        return this.initPromise;
    }

    async _doInitialize() {
        if (this.initialized) {
            return "Already initialized";
        }

        try {
            // Dynamically import the .NET runtime
            const { dotnet } = await import(this.options.wasmPath);

            // Load the .NET WebAssembly runtime
            const { getAssemblyExports, getConfig } = await dotnet
                .withDiagnosticTracing(this.options.debugMode)
                .withApplicationArgumentsFromQuery()
                .create();

            // Get the config
            const config = getConfig();

            // Get the exported methods from the C# Program class
            this.exports = await getAssemblyExports(config.mainAssemblyName);

            // Initialize the FORTH interpreter
            const welcomeMessage =
                this.exports.StellaLang.REPL.Program.InitializeInterpreter();

            this.initialized = true;

            // Call ready callback if provided
            if (this.options.onReady) {
                this.options.onReady(welcomeMessage);
            }

            return welcomeMessage;
        } catch (error) {
            const errorMsg = `Failed to initialize REPL: ${error.message}`;

            if (this.options.onError) {
                this.options.onError(error);
            }

            throw new Error(errorMsg);
        }
    }

    /**
     * Executes FORTH code and returns the result
     * @param {string} input - FORTH code to execute
     * @returns {Promise<string>} Output from the interpreter
     * @throws {Error} If the interpreter is not initialized
     */
    async execute(input) {
        if (!this.initialized) {
            throw new Error("REPL not initialized. Call initialize() first.");
        }

        try {
            const result =
                this.exports.StellaLang.REPL.Program.ProcessInput(input);

            // Call output callback if provided and there's output
            if (this.options.onOutput && result && result.trim() !== "") {
                this.options.onOutput(result);
            }

            return result;
        } catch (error) {
            const errorMsg = `Execution error: ${error.message}`;

            if (this.options.onError) {
                this.options.onError(error);
            }

            throw new Error(errorMsg);
        }
    }

    /**
     * Executes multiple lines of FORTH code
     * @param {string[]} lines - Array of FORTH code lines
     * @returns {Promise<string[]>} Array of outputs for each line
     */
    async executeMultiple(lines) {
        const results = [];
        for (const line of lines) {
            const result = await this.execute(line);
            results.push(result);
        }
        return results;
    }

    /**
     * Check if the REPL is initialized
     * @returns {boolean}
     */
    isReady() {
        return this.initialized;
    }

    /**
     * Get the raw C# exports (for advanced usage)
     * @returns {Object|null} The C# exports object
     */
    getExports() {
        return this.exports;
    }
}

/**
 * Creates and initializes a StellaLang REPL instance in one call
 * @param {Object} options - Configuration options (same as StellaLangREPL constructor)
 * @returns {Promise<StellaLangREPL>} Initialized REPL instance
 */
export async function createREPL(options = {}) {
    const repl = new StellaLangREPL(options);
    await repl.initialize();
    return repl;
}

/**
 * Simple helper to create a basic REPL with console output
 * @returns {Promise<StellaLangREPL>} Initialized REPL instance
 */
export async function createConsoleREPL() {
    return createREPL({
        onOutput: (text) => console.log(text),
        onError: (error) => console.error("Error:", error),
        onReady: (msg) => console.log("✓", msg),
    });
}

// Default export
export default StellaLangREPL;
