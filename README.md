# Evasion Architecture

## Quickstart

1. Start the game server `./Server N M` (download from Releases tab).
2. Open `Observers/elm/index.html` in your browser.
3. Connect your hunter e.g. `python random_player.py`.
4. Connect your prey e.g. `python random_player.py`.
5. The game will play, with live updates shown on the open Observer page.

Running server on Crackle with local display:
1. SSH local tunnel from local computer to access node: `ssh -L LOCAL_PORT:localhost:ACCESS_PORT username@access.cims.nyu.edu`
2. SSH local tunnel from access node to crackle3: `ssh -L ACCESS_PORT:localhost:CRACKLE_PORT username@crackle3.cims.nyu.edu`
3. Run the server on crackle3 on `CRACKLE_PORT` e.g. `./Server CRACKLE_PORT 50 10`
4. Open the Observer page locally (making sure the port number is `LOCAL_PORT` in the JavaScript connection code)
5. Connect your hunter on crackle3 on CRACKLE_PORT e.g. `python3 random_player CRACKLE_PORT` (making sure Python 3.10 is loaded)
6. Connect your prey on crackle3
7. The game should play on crackle3, with display on the local observer.

## Your Client

### Using the provided Python client

If you wish to use the example Python client, then copy the `Clients/python/random_player.py` file and make the following changes:
1. Change the team name in Line 23 to your team name. 
2. Implement the method `self.calculate_hunter_move(game)` for your client (gets called when you play as the hunter). 
3. Implement the method `self.calculate_prey_move(game)` for your client (gets called when you play as the prey). 

Both the `calculate_hunter_move` and `calculate_prey_move` methods need to return a string, which is what gets sent to the server. To help you, the following convenience functions are available to construct the move strings, so you can just return one of these from your function:
1. If you are the hunter, your available moves per tick are:
    1. `self.move_create_wall(wall)` to construct the move to create a single wall
    2. `self.move_only_remove_walls(walls)` to construct the move to remove one or more walls (but do nothing else)
    3. `self.move_remove_walls_and_create(walls_to_remove, wall_to_create)` to construct the move to remove one or more walls and then create a single wall
    4. `self.move_no_op()` to construct the move to do nothing. 
2. If you are the prey, your available moves per odd tick are:
    1. `self.move_change_velocity(new_velocity)` to construct the move to change your velocity. 
    2. `self.move_no_op()` to construct the move to do nothing. 

Relevant types and type signatures for these functions that you will find helpful are provided in the file `client.py`.

### Making your own client

Your client must perform the following, in this order:
1. Connect via socket to the running game server on `localhost:port`
2. Send your team name to the server (e.g. `My Team Name (Rust)`)
3. Recv from socket your role and initial N and M variables from the server (in the form `[hunter|prey] [N] [M]` e.g. `hunter 50 10` for N=50, M=10)
4. Loop:
    1. Recv from socket the current game state (see below on format of game state). 
    2. Send your move. 
5. The end of the game is signalled by receiving `end` instead of the game state. 

**All messages sent must be terminated with a newline (`\n`) and all messages received will be terminated with a newline (`\n`)**.

#### Decoding the Game State

The game state sent to the hunter and prey consists of space-delimited positive and negative integers (or, for one field, possibly the string `null`) in the following format:

```
Time HunterX HunterY HunterVelocityX HunterVelocityY HunterLastWall PreyX PreyY PreyVelocityX PreyVelocityY NumWalls Walls
```

where `Walls` (if `NumWalls > 0`) is in the format `x1 y1 x2 y2` for a wall joining points (x1,y1) to (x2,y2). Because there are only horizontal and vertical walls in this game, it is guaranteed that either `x1 = x2` or `y1 = y2` (or possibly both). 

For example:

```
0 0 0 1 1 null 230 200 0 0 0
```

Indicates:
- Time = 0 ticks
- HunterX = 0 units
- HunterY = 0 units
- HunterVelocityX = 1 units/tick
- HunterVelocityY = 1 units/tick
- HunterLastWall = null (hasn't made any walls)
- PreyX = 230 units
- PreyY = 300 units
- PreyVelocityX = 0 units/odd tick
- PreyVelocityY = 0 units/odd tick
- NumWalls = 0
- walls = [] (no walls)

Another example:
```
30 31 31 -1 1 20 30 200 1 -1 2 0 31 300 31 5 10 5 20
```

Indicates:
- Time = 30 ticks
- HunterX = 31 units
- HunterY = 31 units
- HunterVelocityX = -1 units/tick
- HunterVelocityY = 1 units/tick
- HunterLastWall = 20 ticks (last wall was made at 20 ticks)
- PreyX = 30 units
- PreyY = 200 units
- PreyVelocityX = 1 unit/odd tick
- PreyVelocityY = -1 unit/odd tick
- NumWalls = 2 walls made
- Walls = [Horizontal wall from (0,31) to (300,31), Vertical wall from (5,10) to (5,20)] where coordinates are (x,y)


#### Sending Moves

As the hunter, you can send the following moves:
1. `create x1 y1 x2 y2` to create a wall, e.g. `create 0 31 300 31`
2. `remove x1 y1 x2 y2 ...` to remove one or more walls, e.g. `remove 0 31 300 31` to remove one wall, `remove 0 31 300 31 remove 5 10 5 20` to remove two walls etc.
3. `remove x1 y2 x2 y2 ... create x1 y1 x2 y2` to remove one or more walls then create a single wall, e.g. `remove 0 31 300 31 remove 5 10 5 20 create 0 35 300 35`
4. `none` for no action this tick. 

If you remove walls and create a wall in one move, you must use the format above; removes must come before the create in the string. You cannot create more than one wall in a move. 

As the prey, you can send the following moves:
1. `change x y` to change velocity to (x,y). If you want to stop moving, send `change 0 0`. 
2. `none` for no action this tick. 

A move will be requested from the hunter every tick. A move will be requested from the prey **only every odd tick**.

You must always respond with an action, so if you want to take no action, please explicitly send `none`. 

Invalid actions are ignored and considered as `none`. You can inspect the server logs to verify your actions were received correctly and considered valid. 

# Architecture

## Running the Game Server

To run the game server, download the release appropriate for your system from the Releases tab and run it with your values of N and M. For example, for N=50 and M=10, run:

```bash
./Server 50 10
```

This will start the game server on localhost port 4000. You can optionally specify the port number:

```bash
./Server 4000 50 10
```

You must then connect the observer, hunter and prey to the game server, **in that order**. 

## Connecting the Observer

Two observers are provided: a live graphical observer as a HTML file and a command-line Python observer that will output a video file. I recommend the HTML live graphical observer as I have tested it more thoroughly. Only one observer can be used for a single game. 

1. To connect the HTML graphical observer, start the game server first (as above) and then open the HTML file in your browser. It should show a message indicating that it is connected and awaiting for the game to start. If it does not, double check the game server is running. 
2. To connect the Python observer, start the game server first (as above), and then run `python observer.py` from the Observers/python directory. You will need `numpy`, `websockets`, `ffmpeg-python` installed and `ffmpeg` must be available on your system. This will save a video file to the current directory.

If you try connecting multiple observers to a single game, it will register these as hunter/prey and the game will not run. 

## Connecting the Hunter and Prey

I have provided an example Python client in the Clients folder. Run the example Python client by running `python random_player.py` from the Clients/python directory. If you are using your own client, see above on how to connect it. 

## Server Architecture

The server is implemented as follows:
- `Game` contains all the Game logic. 
    - `Field` contains all logic relating to points and collisions in the game field.
    - `Game` contains all logic relating to running and stepping the game.
    - `Serialization` contains all logic used to serialize and deserialize the game to strings. 
- `Server` contains the server logic. 

## Tests

Tests of the Game logic are stored in `Game.Tests`. 

Run `dotnet test` to run all test cases. 

# Contact

Send me a message on the Heuristic Problem Solving Google Space if you have any questions or notice any issues. 
