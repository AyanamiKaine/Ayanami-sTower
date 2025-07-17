// index.js

// This file serves as the main entry point for the sfpm-js package.
// It exports all the core classes, providing a clean public API for users.

export { Rule } from './src/Rule.js';
export { Criteria, Operator, OperatorSymbols } from './src/Criteria.js';
export { Query } from './src/Query.js';
export { FactSource, DictionaryFactSource } from './src/FactSource.js';
export { match, orderBySpecificity, mostSpecificRule, leastSpecificRule } from './src/RuleMatcher.js';