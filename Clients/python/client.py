#!/usr/bin/env python3.10

import sys
import time
import random
from socket import socket
from dataclasses import dataclass
from typing import NamedTuple
from enum import Enum

# -------------------------------------------------------------------------------
# INSTRUCTIONS 
# -------------------------------------------------------------------------------

"""
TODO list for your team if you wish to use this client:
1. Change self.team_name in the Client class to your team name
2. Implement the calculate_hunter_move function in the Client class
3. Implement the calculate_prey_move function in the Client class
"""

# -------------------------------------------------------------------------------
# CONSTANTS
# -------------------------------------------------------------------------------

# The maximum width and height of the game field in distance units.
MAX_WIDTH = 300
MAX_HEIGHT = 300

# -------------------------------------------------------------------------------
# TYPES
# -------------------------------------------------------------------------------


@dataclass
class Config:
    """The initial configuration variables of the game.

    Attributes
    -----------
    next_wall_time: int
        (N) The time in game ticks until the hunter is allowed to make a new wall.
    max_walls: int
        (M) The maximum number of walls the hunter is allowed to have at any one time.
    """

    next_wall_time: int
    max_walls: int

class Role(Enum):
    """Your role in the current game."""
    HUNTER = 0
    PREY = 1

class Point(NamedTuple):
    """A single 2D point in the game field.

    Attributes
    ----------
    x: int
        The x coordinate of the point, which ranges from 0 to MAX_WIDTH.
    y: int
        The y coordinates of the point, which ranges from 0 to MAX_HEIGHT.
    """

    x: int
    y: int


class Velocity(NamedTuple):
    """A single 2D velocity vector in distance units per game tick (hunter) or odd game tick (prey).

    Velocity values must only be -1, 0 or 1. Any other values are coerced
    to these values via the signum function during deserialization.

    The hunter moves this distance every game tick.
    The prey moves this distance every odd game tick.

    Attributes
    ----------
    x: int
        The velocity in the x direction, which must be -1, 0, or 1.
    y: int
        The velocity in the y direction, which must be -1, 0, or 1.
    """

    x: int
    y: int


class Wall(NamedTuple):
    """A wall created by a hunter in the game field.

    From a deserialization perspective, please ensure when you construct a wall that:
    - Either x1=x2 (for a vertical wall) or y1=y2 (for a horizontal wall)

    There are other constraints walls must satisfy in order to be valid in the game, but these are
    checked after serialization. Namely:
    - Walls must touch the hunter's current position before they move
    - Walls must not collide with the hunter's position after the hunter moves
    - Walls must only be created after config.next_wall_time ticks have passed since the last wall
    - The number of walls must not exceed config.max_walls
    - The wall must not collide with the current position of the prey (but it can prevent the prey
      from moving, as walls are added before the prey is moved)
    - The wall must not collide with other walls (so you must ensure you check this carefully when
      creating walls)
    - If the wall is horizontal, then x1 < x2; if the wall is vertical, then y1 < y2

    Attributes
    ----------
    x1: int
        The x coordinate of the first end point of the wall.
    y1: int
        The y coordinate of the first end point of the wall.
    x2: int
        The x coordinate of the second end point of the wall.
    y2: int
        The y coordinate of the second end point of the wall.
    """
    x1: int
    y1: int
    x2: int
    y2: int


@dataclass
class GameState:
    """Representation of the current state of the game.

    Attributes
    ----------
    ticker: int
        The current game tick in game time.
    hunter_position: Point
        The current position of the hunter.
    hunter_velocity: Velocity
        The current velocity of the hunter.
    hunter_last_wall_time: None | int
        The time in game ticks since the last wall was created by the hunter (None if no walls created yet)
    prey_position: Point
        The current position of the prey.
    prey_velocity: Velocity
        The current velocity of the prey. Note that the prey only applies this velocity on odd game ticks.
    walls: list[Wall]
        The current walls in the game field.
    """
    ticker: int
    hunter_position: Point
    hunter_velocity: Velocity
    hunter_last_wall_time: None | int
    prey_position: Point
    prey_velocity: Velocity
    walls: list[Wall]

# -------------------------------------------------------------------------------
# GAME STATE DECODING
# -------------------------------------------------------------------------------

def decode_game(string: str) -> GameState:
    """Decode the game string into GameState data."""
    # Split the encoded game string
    split = string.split(" ")

    # Deconstruct into each component
    ticker = int(split[0])
    hunter_position = Point(int(split[1]), int(split[2]))
    hunter_velocity = Velocity(int(split[3]), int(split[4]))
    hunter_last_wall_time = None if split[5] == "null" else int(split[5])
    prey_position = Point(int(split[6]), int(split[7]))
    prey_velocity = Velocity(int(split[8]), int(split[9]))
    num_walls = int(split[10])
    walls = [Wall(*map(int, split[11+i*4: 11+(i+1)*4])) for i in range(num_walls)]

    # Construct the game state
    return GameState(
        ticker,
        hunter_position,
        hunter_velocity,
        hunter_last_wall_time,
        prey_position,
        prey_velocity,
        walls,
    )

# -------------------------------------------------------------------------------
# UTILITY FUNCTIONS FOR MOVES
# -------------------------------------------------------------------------------

def create_wall(wall: Wall) -> str:
    """Create the string to send to the server to create a wall (hunter only).

    Be very careful with the order of numbers you pass to the Wall constructor!
    The order is x1, y1, x2, y2. 

    See the Wall NamedTuple documentation for constraints on walls.
    """
    return f"create {wall.x1} {wall.y1} {wall.x2} {wall.y2}"

def remove_wall(wall: Wall) -> str:
    """Create the string to send to the server to remove a wall (hunter only).
    If this wall does not exist, then it is a no-op.
    """
    return f"remove {wall.x1} {wall.y1} {wall.x2} {wall.y2}"

def no_op() -> str:
    """Create the string to send to the server to do nothing."""
    return "none"

def change_velocity(velocity: Velocity) -> str:
    """Create the string to send to the server to change your velocity (prey only)."""
    return f"change {velocity.x} {velocity.y}"

# -------------------------------------------------------------------------------
# CLIENT
# -------------------------------------------------------------------------------


class Client:
    """A basic client for the evasion game server.

    Attributes
    ----------
    socket: socket
        The socket used to communicate with the server.
    port: int
        The port the socket is connected to.
    team_name: str
        Your team name (replace it below).
    role: Role
        Your role in the current game.
    config: Config
        The initial configuration of the game (defined by N=NextWallTime and M=MaxWalls)
    """

    def __init__(self, port=4000):
        """Initialises the client, connects to the server, and receives the initial configuration."""
        self.socket: socket = socket()
        self.port: int = port
        self.team_name: str = "YOUR TEAM NAME HERE"

        # Connect to socket
        self.socket.connect(("127.0.0.1", port))

        # Send over the team name
        self.send(f"{self.team_name} (Python)")
        print("Connected to server.")

        # Read the initial configuration
        config_string = self.read().split()
        self.role = Role.HUNTER if config_string[0] == "hunter" else Role.PREY
        self.config: Config = Config(int(config_string[1]), int(config_string[2]))

        print(f"Received initial configuration: Role={self.role}, N={self.config.next_wall_time}, M={self.config.max_walls}.")

    def run(self) -> None:
        """The main loop of the client to play the game."""
        while True:

            # Read the current game state
            state_string = self.read()
            
            # Check for completion
            if state_string == "end":
                break

            # Decode the game state
            game = decode_game(state_string)

            # Calculate and send move
            if self.role == Role.HUNTER:
                move = self.calculate_hunter_move(game)
                self.send(move)
            else:
                move = self.calculate_prey_move(game)
                self.send(move)

    def calculate_hunter_move(self, game: GameState) -> str:
        """Calculate the next move as the hunter and return the encoded move string.

        Useful variables and functions for you to use:
        - self.config: contains N and M values
        - game: contains entire game state at a given tick
        - create_wall, remove_wall, no_op: functions to create the move string
        Make sure you return the move string from this function.

        TODO: Fill this in with your code below.
        At the moment, chooses a move at random (not guaranteed to be valid).
        """

        # Dummy random player: delete the code below and replace with your player.
        roll = random.random()
        if roll <= 0.5:
            return no_op()
        elif 0.5 < roll <= 0.72:
            if len(game.walls) < self.config.max_walls and (game.hunter_last_wall_time is None or game.ticker >= game.hunter_last_wall_time + self.config.next_wall_time):
                x = game.hunter_position.x
                y1 = random.randint(0, game.hunter_position.y)
                y2 = random.randint(game.hunter_position.y, MAX_HEIGHT)
                return create_wall(Wall(x, y1, x, y2))
            else:
                return no_op()
        elif 0.72 < roll <= 0.94: 
            if len(game.walls) < self.config.max_walls and (game.hunter_last_wall_time is None or game.ticker >= game.hunter_last_wall_time + self.config.next_wall_time):
                y = game.hunter_position.y
                x1 = random.randint(0, game.hunter_position.x)
                x2 = random.randint(game.hunter_position.x, MAX_WIDTH)
                return create_wall(Wall(x1, y, x2, y))
            else:
                return no_op()
        else:
            if len(game.walls) > 0:
                return remove_wall(random.choice(game.walls))
            else:
                return no_op()

    def calculate_prey_move(self, game: GameState) -> str:
        """Calculate the next move as the prey and return the encoded move string.

        Useful variables and functions for you to use:
        - self.config: contains N and M values
        - game: contains entire game state at a given tick
        - change_velocity, no_op: functions to create the move string
        Make sure you return the move string from this function.

        TODO: Fill this in with your code below.
        At the moment, chooses a move at random (not guaranteed to be valid).
        """

        # Dummy random player: delete the code below and replace with your player.
        roll = random.random()
        if roll <= 0.5:
            return no_op()
        else:
            x = random.randint(-1, 1)
            y = random.randint(-1, 1)
            return change_velocity(Velocity(x, y))

    def send(self, message: str) -> None:
        """Send a string to the server, ensuring the message is terminated with a newline."""
        print(f"Sending: {message}")
        self.socket.send((message + "\n").encode("utf-8"))
    
    def read(self) -> str:
        """Read a message from the server."""
        return self.socket.recv(1024).decode("utf-8").strip()

# -------------------------------------------------------------------------------
# MAIN
# -------------------------------------------------------------------------------

if __name__ == '__main__':
    if len(sys.argv) == 1:
        port = 4000
    else:
        port = int(sys.argv[1])

    client = Client(port)
    client.run()

