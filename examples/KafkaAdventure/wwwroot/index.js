const inputField = document.getElementById("commandInput");
const terminal = document.getElementById("terminal");
const gamewindow = document.getElementById("gamewindow");

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/kafka-adventure/input")
    .configureLogging(signalR.LogLevel.Information)
    .build();

inputField.addEventListener("keydown", function (event) {
    if (event.key === "Enter") {
        const command = inputField.value.trim();
        if (command) {
            executeCommand(command);
        }
        inputField.value = "";
    }
});


/**
 * Scrolls the element with ID "body" to its bottom.
 */
function scrollToBottom() {
    const bdy = document.getElementById("body");
    bdy.scrollTop = bdy.scrollHeight;
}

/**
 * Sends a user command to the server and displays it in the terminal.
 * @param {string} command - The command string entered by the user.
 */
function executeCommand(command) {
    const gameId = localStorage.getItem('gameid');
    connection.send("SendMessage", gameId, command);

    const output = document.createElement("p");
    output.innerHTML = `<span class="text-green-500">$</span> ${command}`;
    terminal.appendChild(output);

    scrollToBottom();
}
/**
 * Displays a server response in the terminal with a typewriter animation effect.
 * @param {string} res - The response text to display.
 */
function addResponse(res) {
    const response = document.createElement("p");
    response.className = "text-green-400";
    terminal.appendChild(response);
    let i = 0;

    function typeWriter() {
        if (i < res.length) {
            response.innerHTML += res.charAt(i);
            i++;
            setTimeout(typeWriter, 10);
        }
    }

    typeWriter();
    scrollToBottom();
}

/**
 * Animates text into a specified DOM element, displaying one character at a time at a given speed.
 * @param {string} elementId - The ID of the DOM element where the text will be displayed.
 * @param {string} text - The text to animate.
 * @param {number} speed - The delay in milliseconds between each character.
 */
function slowType(elementId, text, speed) {
    let i = 0;
    const element = document.getElementById(elementId);

    function typeWriter() {
        if (i < text.length) {
            element.innerHTML += text.charAt(i);
            i++;
            setTimeout(typeWriter, speed);
        }
    }

    typeWriter();
}
/**
 * Generates a version 4 UUID string using timestamp and randomization.
 * @return {string} A randomly generated UUID in the format xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx.
 */
function generateUUID() { // Public Domain/MIT
    var d = new Date().getTime();//Timestamp
    var d2 = ((typeof performance !== 'undefined') && performance.now && (performance.now() * 1000)) || 0;//Time in microseconds since page-load or 0 if unsupported
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = Math.random() * 16;//random number between 0 and 16
        if (d > 0) {//Use timestamp until depleted
            r = (d + r) % 16 | 0;
            d = Math.floor(d / 16);
        } else {//Use microseconds since page-load if supported
            r = (d2 + r) % 16 | 0;
            d2 = Math.floor(d2 / 16);
        }
        return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
    });
}


/**
 * Initializes and maintains the SignalR connection for the game session.
 *
 * Retrieves or generates a unique game ID, establishes the SignalR connection, sets up a handler for incoming messages, and joins the game session. If the connection fails, it retries after a delay.
 */
async function start() {
    try {
        let gameId = localStorage.getItem('gameid');
        if (gameId == null) {
            gameId = generateUUID();
            localStorage.setItem('gameid', gameId);
        }

        await connection.start();
        connection.on("ReceiveMessage", addResponse);
        connection.send("JoinGame", gameId);
    } catch (err) {
        setTimeout(start, 5000);
    }
};

connection.onclose(async () => {
    await start();
});

start();