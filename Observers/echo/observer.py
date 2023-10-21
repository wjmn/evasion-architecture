#!/usr/bin/env python3.10
import sys
from websockets.sync.client import connect

class Observer():
    def __init__(self, port=4000):
        """Initialises the client, connects to the server and echos all messages."""
        self.socket = connect(f"ws://127.0.0.1:{port}")
        self.port: int = port

    def run(self) -> None:
        while True:
            read_string = self.read()
            if read_string == "end":
                break
            else:
                print(read_string)
    
    def read(self) -> str:
        """Read a message from the server."""
        return self.socket.recv().strip()

if __name__ == '__main__':
    if len(sys.argv) == 1:
        port = 4000
    else:
        port = int(sys.argv[1])

    observer = Observer(port)
    observer.run()


