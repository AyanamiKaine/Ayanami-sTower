/**
 * The desire to know, understand, and explore. This includes the need for knowledge, curiosity, and meaning
 */
export class CognitiveNeeds {
    constructor(value, desire) {
        this.value = value;
        /**
         * The desire, determines the priority to fullfill this need, a mad person may ignore his desire to eat or to be
         * safe to go after a higher transcendence need.
         */
        this.desire = desire;
    }
}
