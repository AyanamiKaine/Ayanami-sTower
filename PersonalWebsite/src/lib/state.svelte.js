let dialogState = $state(
    new Map([
        ["playerStrength", 10],
    ])
);

export default {
    get state() {
        return dialogState;
    },

    update(updater) {
        updater(dialogState);
        dialogState = new Map(dialogState);
    },
};
