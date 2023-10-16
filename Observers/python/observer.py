#!/usr/bin/env python3.10
import sys
import os
from socket import socket

sys.path.append(os.path.abspath('../../Clients/python'))
from client import *

class Observer():
    def __init__(self, port=4000):
        """Initialises the client, connects to the server, and receives the initial configuration and game state"""
        self.socket: socket = socket()
        self.port: int = port

        # Connect to socket
        self.socket.connect(("127.0.0.1", port))

        # Read each player's name
        self.hunter_name = self.readline()
        self.prey_name = self.readline()
        print(self.hunter_name)
        print(self.prey_name)

        # Read the initial configuration
        config_string = self.readline()
        split = config_string.split()
        self.config = Config(int(split[0]), int(split[1]))
        self.game = decode_game(" ".join(split[2:]))
        self.hunter_remaining = 120*1000
        self.prey_remaining = 120*1000

        print(self.config)
        self.render()

    def run(self) -> None:
        while True:
            state = self.readline().split()

            print(state)
            discriminator = state[0]
            if discriminator == "end":
                print("Game ended!")
                break
            elif discriminator == "continues":
                self.game = decode_game(" ".join(state[3:]))
            elif discriminator == "caught":
                self.game = decode_game(" ".join(state[3:]))
                print("Prey is caught!")
            elif discriminator == "timeout":
                self.game = decode_game(" ".join(state[3:]))
                print("Prey evaded until timeout!")
            self.hunter_remaining = float(state[1])
            self.prey_remaining = float(state[2])

            self.render()
    
    def readline(self) -> str:
        """Read a message from the server."""
        return self.socket.makefile().readline().strip()

    def render(self) -> None:
        print(self.game)



# -------------------------------------------------------------------------------
# MAIN
# -------------------------------------------------------------------------------

if __name__ == '__main__':
    if len(sys.argv) == 1:
        port = 4000
    else:
        port = int(sys.argv[1])

    observer = Observer(port)
    observer.run()


