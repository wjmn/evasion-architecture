import sys
import random
from client import EvasionClient, GameState, Wall, Velocity, MAX_WIDTH, MAX_HEIGHT

# -------------------------------------------------------------------------------
# INSTRUCTIONS 
# -------------------------------------------------------------------------------

"""
TODO list for your team if you wish to use this client:
1. Change self.team_name in the Client class to your team name
2. Implement the calculate_hunter_move function in your client 
3. Implement the calculate_prey_move function in your client
"""

# -------------------------------------------------------------------------------
# YOUR CLIENT
# -------------------------------------------------------------------------------

class MyClient(EvasionClient):
    def __init__(self, port=4000):
        # TODO: Change this to your team name!
        self.team_name = "Random Player"

        super().__init__(self.team_name, port)

    def calculate_hunter_move(self, game: GameState) -> str:
        """Calculate the next move as the hunter and return the encoded move string.

        Useful variables and functions for you to use:
        - self.config: contains next_wall_time (N) and max_walls (M) values
        - game: contains entire new game state at a given tick (passed as a parameter every tick). See the GameState class for what variables it contains.
        - self.move_create_wall, self.move_only_remove_walls, self.move_remove_walls_and_create, self.move_no_op: functions to create the move string 
          (you must return the function call from this function as it returns a string. Use only one of these options to move). 
        Make sure you return the move string from this function.

        TODO: Fill this in with your code below.
        At the moment, chooses a move at random (not guaranteed to be valid).
        """
        # Dummy random player: delete the code below and replace with your player.
        if len(game.walls) > 0:
            walls = game.walls
            random.shuffle(walls)
            num_to_remove = random.randint(1, len(walls))
            walls_to_remove = walls[0:num_to_remove]
        else:
            walls_to_remove = []
        roll = random.random()
        if roll <= 0.9:
            return self.move_no_op()
        elif 0.9 < roll <= 0.945:
            if len(game.walls) < self.config.max_walls and (game.hunter_last_wall_time is None or game.ticker >= game.hunter_last_wall_time + self.config.next_wall_time):
                x = game.hunter_position.x
                y1 = random.randint(0, game.hunter_position.y)
                y2 = random.randint(game.hunter_position.y, MAX_HEIGHT)
                if len(walls_to_remove) > 0 and roll > 0.940:
                    return self.move_remove_walls_and_create(walls_to_remove, Wall(x, y1, x, y2))
                else:
                    return self.move_create_wall(Wall(x, y1, x, y2))
            else:
                return self.move_no_op()
        elif 0.945 < roll <= 0.99: 
            if len(game.walls) < self.config.max_walls and (game.hunter_last_wall_time is None or game.ticker >= game.hunter_last_wall_time + self.config.next_wall_time):
                y = game.hunter_position.y
                x1 = random.randint(0, game.hunter_position.x)
                x2 = random.randint(game.hunter_position.x, MAX_WIDTH)
                if len(walls_to_remove) > 0 and roll > 0.985:
                    return self.move_remove_walls_and_create(walls_to_remove, Wall(x1, y, x2, y))
                else:
                    return self.move_create_wall(Wall(x1, y, x2, y))
            else:
                return self.move_no_op()
        else:
            if len(walls_to_remove) > 0:
                return self.move_only_remove_walls(walls[0:num_to_remove])
            else:
                return self.move_no_op()

    def calculate_prey_move(self, game: GameState) -> str:
        """Calculate the next move as the prey and return the encoded move string.

        Useful variables and functions for you to use:
        - self.config: contains next_wall_time (N) and max_walls (M) values
        - game: contains entire new game state at a given tick (passed as a parameter every tick). See the GameState class for what variables it contains.
        - self.move_change_velocity, self.move_no_op: helper functions to create the move string (return these from this function)
        Make sure you return the move string from this function.

        TODO: Fill this in with your code below.
        At the moment, chooses a move at random (not guaranteed to be valid).
        """

        # Dummy random player: delete the code below and replace with your player.
        roll = random.random()
        if roll <= 0.8:
            return self.move_no_op()
        else:
            x = random.randint(-1, 1)
            y = random.randint(-1, 1)
            return self.move_change_velocity(Velocity(x, y))

# -------------------------------------------------------------------------------
# MAIN
# -------------------------------------------------------------------------------

if __name__ == '__main__':
    if len(sys.argv) == 1: port = 4000
    else: port = int(sys.argv[1])
    client = MyClient(port)
    client.run()


