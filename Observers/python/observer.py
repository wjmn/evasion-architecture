#!/usr/bin/env python3.10
import sys
import os
import numpy as np
import ffmpeg
from dataclasses import dataclass
from websockets.sync.client import connect

sys.path.append(os.path.abspath('../../Clients/python'))
from client import *

@dataclass
class Observation: 
    game: GameState
    hunter_remaining: float
    prey_remaining: float

class Observer():
    def __init__(self, port=4000):
        """Initialises the client, connects to the server, and receives the initial configuration and game state"""
        self.socket = connect(f"ws://127.0.0.1:{port}")
        self.port: int = port
        print(f"Websocket connected to server on port {port}.")

        # Read each player's name
        self.hunter_name = self.read()
        self.prey_name = self.read()

        # Read the initial configuration
        config_string = self.read()
        split = config_string.split()
        self.config = Config(int(split[0]), int(split[1]))

        game = decode_game(" ".join(split[2:]))
        hunter_remaining = 120*1000
        prey_remaining = 120*1000
        self.game_stream = [Observation(game, hunter_remaining, prey_remaining)]

        print(f"Observing game between hunter {self.hunter_name} and prey {self.prey_name} with configuration N={self.config.next_wall_time} and M={self.config.max_walls}")


    def run(self) -> None:
        while True:
            state = self.read().split()

            discriminator = state[0]
            if discriminator == "end":
                print("Game ended.")
                break
            elif discriminator == "continues":
                game = decode_game(" ".join(state[3:]))
            elif discriminator == "caught":
                game = decode_game(" ".join(state[3:]))
                print("Prey is caught!")
            elif discriminator == "timeout":
                game = decode_game(" ".join(state[3:]))
                print("Prey evaded until timeout!")
            hunter_remaining = float(state[1])
            prey_remaining = float(state[2])

            if game.ticker % 100 == 0:
                print(f"Game tick {game.ticker} received.")

            self.game_stream.append(Observation(game, hunter_remaining, prey_remaining))

    
    def read(self) -> str:
        """Read a message from the server."""
        return self.socket.recv().strip()

    def render(self) -> None:
        print(f"Rendering game animation...this may take a while!")

        filename = f"observed_{self.hunter_name}_{self.prey_name}.mp4"

        process = (
            ffmpeg
                .input('pipe:', format='rawvideo', pix_fmt='rgb24', s='{}x{}'.format(MAX_WIDTH, MAX_HEIGHT))
                .output(filename, pix_fmt='yuv420p', vcodec="libx264", r=800)
                .overwrite_output()
                .run_async(pipe_stdin=True)
        )

        for state in self.game_stream:
            # image with black background
            image = np.zeros((MAX_HEIGHT, MAX_WIDTH, 3), dtype=np.uint8)
            image[0, 0:MAX_WIDTH, :] = 255
            image[MAX_HEIGHT-1, 0:MAX_WIDTH, :] = 255
            image[0:MAX_HEIGHT, 0, :] = 255
            image[0:MAX_HEIGHT, MAX_WIDTH-1, :] = 255

            # draw hunter in mostly red
            image[state.game.hunter_position.y, state.game.hunter_position.x, :] = [255, 100, 0]

            # draw prey in white
            image[state.game.prey_position.y, state.game.prey_position.x, :] = [255, 255, 255]

            # draw walls in white
            for wall in state.game.walls:
                if wall.x1 == wall.x2:
                    image[wall.y1:wall.y2, wall.x1, :] = 255
                elif wall.y1 == wall.y2:
                    image[wall.y1, wall.x1:wall.x2, :] = 255

            process.stdin.write(image.astype(np.uint8).tobytes())

        print(f"Processed all frames, saving to {filename}...")
        process.stdin.close()
        process.wait()


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
    observer.render()


