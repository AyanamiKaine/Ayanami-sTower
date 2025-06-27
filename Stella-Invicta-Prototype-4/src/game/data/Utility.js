let starSystemNames = {};

export async function loadGameData() {
    try {
        // Fetch the JSON file from your public assets folder
        const response = await fetch("./assets/StarSystemNames.json"); // Adjust the path if needed

        // Check if the request was successful
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        // Parse the JSON data from the response
        starSystemNames = await response.json();

        // Now that the data is loaded, you can start the part of your game that needs it
        console.log("Star system names loaded successfully!");
    } catch (error) {
        console.error("Could not load star system names:", error);
        // You might want to display an error message to the user on the screen
    }
}

export function getRandomSystemName() {
    // Get all arrays of names from the object's values and flatten them into a single array.
    const allNames = Object.values(starSystemNames).flat();

    // Check if there are any names to choose from. If not, return a default message.
    if (allNames.length === 0) {
        return "No names available.";
    }

    // Generate a random index from 0 to the total number of names.
    const randomIndex = Math.floor(Math.random() * allNames.length);

    // Return the name at the randomly selected index.
    return allNames[randomIndex];
}
