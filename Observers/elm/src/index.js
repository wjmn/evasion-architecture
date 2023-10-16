import './main.css';
import { Elm } from './Main.elm';
import * as serviceWorker from './serviceWorker';

var app = Elm.Main.init({
  node: document.getElementById('root')
});

// Websocket connection
const websocket = new WebSocket('ws://127.0.0.1:4000');

websocket.onopen = (e) => {
  console.log("Websocket connected.");
};

websocket.onmessage = (e) => {
  console.log(e.data);
  app.ports.messageReceiver.send(e.data);
};

websocket.onclose = (e) => {
  console.log("Websocket closed.");
};


// If you want your app to work offline and load faster, you can change
// unregister() to register() below. Note this comes with some pitfalls.
// Learn more about service workers: https://bit.ly/CRA-PWA
serviceWorker.unregister();
