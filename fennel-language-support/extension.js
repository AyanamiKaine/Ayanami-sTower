// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
const vscode = require("vscode");

// An array of objects for keywords that should be expanded into snippets.
const FENNEL_SNIPPETS = [
    {
        label: "fn",
        detail: "Function definition snippet",
        documentation:
            "Inserts a function definition with placeholders for name, arguments, and body.",
        snippet: "(fn ${1:name} [${2:args}]\n\t$0\n)",
    },
    {
        label: "let",
        detail: "Let binding snippet",
        documentation:
            "Inserts a `let` block with placeholders for bindings and body.",
        snippet: "(let [${1:name} ${2:value}]\n\t$0\n)",
    },
    {
        label: "local",
        detail: "Local variable snippet",
        documentation: "Inserts a local variable definition.",
        snippet: "(local ${1:name} ${2:value})",
    },
    {
        label: "if",
        detail: "If-conditional snippet",
        documentation: "Inserts an if/else block.",
        snippet:
            "(if ${1:condition}\n\t${2:then-expression}\n\t${3:else-expression})",
    },
];

// A list of other Fennel keywords and macros for simple autocompletion.
const FENNEL_KEYWORDS = [
    "var",
    "global",
    "set",
    "do",
    "each",
    "for",
    "while",
    "tset",
    "quote",
    "unquote",
    "macro",
    "macros",
    "eval-compiler",
    "import-macros",
    "require-macros",
    "pick-values",
    "values",
    "lua",
    "λ",
    "->",
    "->>",
    "-?>",
    "-?>>",
    "?.",
    "doto",
    "when",
    "with-open",
    "collect",
    "icollect",
    "fcollect",
    "accumulate",
    "faccumulate",
    "partial",
    "lambda",
    "macrodebug",
    "case",
    "match",
    "case-try",
    "match-try",
    "+",
    "-",
    "*",
    "/",
    "%",
    "^",
    "==",
    "~=",
    "!=",
    "<=",
    ">=",
    "<",
    ">",
    "and",
    "or",
    "not",
    "#",
    "..",
    "true",
    "false",
    "nil",
];

/**
 * Parses the document text to find all user-defined function names.
 * @param {vscode.TextDocument} document - The document to parse.
 * @returns {Set<string>} A set of function names.
 */
function findUserDefinedFunctions(document) {
    const text = document.getText();
    const functionNames = new Set();

    const fnRegex = /\(\s*fn\s+([a-zA-Z0-9_\-?!<>=.]+)\s*\[/g;
    let match;
    while ((match = fnRegex.exec(text)) !== null) {
        functionNames.add(match[1]);
    }

    const localFnRegex =
        /\(\s*(?:local|var)\s+([a-zA-Z0-9_\-?!<>=.]+)\s+\(\s*fn/g;
    while ((match = localFnRegex.exec(text)) !== null) {
        functionNames.add(match[1]);
    }

    return functionNames;
}

/**
 * Parses the document text to find all user-defined macro names.
 * @param {vscode.TextDocument} document - The document to parse.
 * @returns {Set<string>} A set of macro names.
 */
function findUserDefinedMacros(document) {
    const text = document.getText();
    const macroNames = new Set();

    const macroRegex = /\(\s*macro\s+([a-zA-Z0-9_\-?!<>=.]+)\s*\[/g;
    let match;
    while ((match = macroRegex.exec(text)) !== null) {
        macroNames.add(match[1]);
    }

    return macroNames;
}

/**
 * Parses the document text to find all user-defined local variables.
 * @param {vscode.TextDocument} document - The document to parse.
 * @returns {Set<string>} A set of variable names.
 */
function findLocalVariables(document) {
    const text = document.getText();
    const variableNames = new Set();
    let match;

    const localOrVarRegex = /\(\s*(?:local|var)\s+([a-zA-Z0-9_\-?!<>=.]+)\s+/g;
    while ((match = localOrVarRegex.exec(text)) !== null) {
        variableNames.add(match[1]);
    }

    const letRegex = /\(\s*let\s+\[([^\]]*)\]/g;
    while ((match = letRegex.exec(text)) !== null) {
        const bindings = match[1].trim().split(/\s+/);
        for (let i = 0; i < bindings.length; i += 2) {
            if (bindings[i]) {
                variableNames.add(bindings[i]);
            }
        }
    }

    return variableNames;
}

/**
 * This method is called when your extension is activated.
 * @param {vscode.ExtensionContext} context
 */
function activate(context) {
    console.log(
        'Congratulations, your extension "fennel-language-support" is now active!'
    );

    const provider = vscode.languages.registerCompletionItemProvider("fennel", {
        provideCompletionItems(document, position, token, context) {
            // 1. Create completions for snippets
            const snippetCompletions = FENNEL_SNIPPETS.map((item) => {
                const completionItem = new vscode.CompletionItem(
                    item.label,
                    vscode.CompletionItemKind.Snippet
                );
                completionItem.insertText = new vscode.SnippetString(
                    item.snippet
                );
                completionItem.detail = item.detail;
                completionItem.documentation = new vscode.MarkdownString(
                    item.documentation
                );
                return completionItem;
            });

            // 2. Create completions for simple keywords
            const keywordCompletions = FENNEL_KEYWORDS.map((keyword) => {
                const kind = ["true", "false", "nil"].includes(keyword)
                    ? vscode.CompletionItemKind.Constant
                    : vscode.CompletionItemKind.Keyword;
                return new vscode.CompletionItem(keyword, kind);
            });

            // 3. Get dynamically found user-defined functions
            const userFunctions = findUserDefinedFunctions(document);
            const functionCompletions = Array.from(userFunctions).map(
                (funcName) => {
                    const item = new vscode.CompletionItem(
                        funcName,
                        vscode.CompletionItemKind.Method
                    );
                    item.detail = "User-defined function";
                    return item;
                }
            );

            // 4. Get dynamically found user-defined macros
            const userMacros = findUserDefinedMacros(document);
            const macroCompletions = Array.from(userMacros).map((macroName) => {
                const item = new vscode.CompletionItem(
                    macroName,
                    vscode.CompletionItemKind.Interface
                );
                item.detail = "User-defined macro";
                return item;
            });

            // 5. Get dynamically found local variables
            const localVariables = findLocalVariables(document);
            const variableCompletions = Array.from(localVariables)
                .filter(
                    (varName) =>
                        !userFunctions.has(varName) && !userMacros.has(varName)
                )
                .map((varName) => {
                    const item = new vscode.CompletionItem(
                        varName,
                        vscode.CompletionItemKind.Variable
                    );
                    item.detail = "Local variable";
                    return item;
                });

            // 6. Combine all lists and return
            return [
                ...snippetCompletions,
                ...keywordCompletions,
                ...functionCompletions,
                ...macroCompletions,
                ...variableCompletions,
            ];
        },
    });

    context.subscriptions.push(provider);
}

// This method is called when your extension is deactivated
function deactivate() {}

module.exports = {
    activate,
    deactivate,
};
