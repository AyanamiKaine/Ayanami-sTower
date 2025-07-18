import { writable } from "svelte/store";

// A simple store that holds the current notification message.
export const notification = writable("");
