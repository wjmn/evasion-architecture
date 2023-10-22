import './main.css';
import { Elm } from './Main.elm';
import * as serviceWorker from './serviceWorker';

let previousData = localStorage.getItem("evasion-previous-data");
if (previousData === null || previousData === undefined) {
  previousData = "[]";
};

var app = Elm.Main.init({
  node: document.getElementById('root'),
  flags: previousData
});

// Websocket connection
const websocket = new WebSocket('ws://127.0.0.1:4000');

websocket.onopen = (e) => {
  console.log("Websocket connected.");
  app.ports.messageReceiver.send("");
};

websocket.onmessage = (e) => {
  console.log(e.data);
  app.ports.messageReceiver.send(e.data);
};

websocket.onclose = (e) => {
  console.log("Websocket closed.");
};

app.ports.savePreviousString.subscribe(function(previousString) {
  localStorage.setItem("evasion-previous-data", previousString);
});

// If you want your app to work offline and load faster, you can change
// unregister() to register() below. Note this comes with some pitfalls.
// Learn more about service workers: https://bit.ly/CRA-PWA
serviceWorker.unregister();
