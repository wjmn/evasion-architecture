module Game.Game

open Game.Field
open System

//------------------------------------------------------------------------------
// TYPES
//------------------------------------------------------------------------------

/// A game configuration consists of configuration constants:
/// - NextWallInterval (N): the number of seconds before the hunter can make a new wall.
/// - MaxWalls (M): the maximum number of walls a hunter can have at any point in time.
[<Struct>]
type Config =
    { NextWallInterval: int<s>
      MaxWalls: int }

/// The game at any single point in time, represented by:
/// - Config: the game configuration constants.
/// - Ticker: the number of "seconds" passed, in game time.
/// - HunterPosition: the current position of the hunter.
/// - HunterVelocity: the current velocity of the hunter.
/// - HunterLastWall: the ticker time when the hunter last made a wall (or None if no wall made).
/// - PreyPosition: the current position of the prey.
/// - PreyVelocity: the current velocity of the prey.
/// - Walls: the current set of walls in the game.
[<Struct>]
type State =
    { Config: Config
      Ticker: int<s>
      HunterPosition: Point
      HunterVelocity: Velocity
      HunterLastWall: Option<int<s>>
      PreyPosition: Point
      PreyVelocity: Velocity
      Walls: Wall list }

/// Actions that can be taken by the hunter.
type HunterAction =
    | CreateWall of Wall
    | RemoveWall of Wall
    | HunterNoAction

/// Actions that can be taken by the prey.
type PreyAction =
    | ChangeVelocity of Velocity
    | PreyNoAction

/// Possible outcomes after a single step action. 
type Outcome = 
    | Continues of State
    | PreyIsCaught of State

//------------------------------------------------------------------------------
// LOGGING FUNCTIONS FOR DEBUGGING
//------------------------------------------------------------------------------

let now() = 
    DateTime.Now.ToLongTimeString()

let log (message: string) = 
    printfn $"[{now()}] {message}"

let logYellow (message: string) = 
    printfn $"\u001B[33m[{now()}] {message}\u001B[0m"

let logGreen (message: string) = 
    printfn $"\u001B[32m[{now()}] {message}\u001B[0m"

let logBlue (message: string) = 
    printfn $"\u001B[34m[{now()}] {message}\u001B[0m"

let logRed (message: string) = 
    printfn $"\u001B[31m[{DateTime.Now.ToLongTimeString()}] {message}\u001B[0m"

//------------------------------------------------------------------------------
// UTILITY FUNCTIONS
//------------------------------------------------------------------------------


/// Initialise a game given the configuration constants.
/// Note that:
/// - The prey starts at (230,200) with (0,0) velocity
/// - The hunter starts at (0,0) with (1,1) velocity
let newGame (config: Config) : State =
    { Config = config
      Ticker = 0<s>
      HunterPosition = { X = 0<m>; Y = 0<m> }
      HunterVelocity = { X = 1<m / s>; Y = 1<m / s> }
      HunterLastWall = None
      PreyPosition = { X = 230<m>; Y = 200<m> }
      PreyVelocity = { X = 0<m / s>; Y = 0<m / s> }
      Walls = [] }

/// Checks the validity of creating a new wall in a game.
let wallIsValid (game: State) (wall: Wall) : bool =
    // The wall collides the current hunter (i.e. touches hunter's position before moving)
    let touchesHunterBeforeMove = wallCollidesPoint wall game.HunterPosition
    // The wall does not collide with the hunter after the hunter moves (i.e. checks edge case if hunter bounces off wall)
    let doesNotCollideHunter =
        not (wallCollidesPoint wall (stepAndBounce game.HunterVelocity game.Walls game.HunterPosition |> fst))
    // The hunter can create a wall at this time step
    let afterWallInterval =
        match game.HunterLastWall with
        | Some lastWall -> game.Ticker - lastWall >= game.Config.NextWallInterval
        | None -> true
    // The hunter has not exceeded the maximum number of walls
    let hasNotExceededMaxWalls = List.length game.Walls < game.Config.MaxWalls
    // The wall does not collide with the current position of the prey
    let doesNotCollidePrey = not (wallCollidesPoint wall game.PreyPosition)
    // The wall is within bounds
    let withinBounds = wallInBounds wall
    // The wall does not collide any other wall
    let doesNotCollideWalls = not (wallCollidesWalls wall game.Walls)
    // The end points of the wall are greater than or equal to the start points
    let endPointsAfterStartPoints = 
        match wall with 
        | Horizontal(_, startX, endX) -> endX >= startX
        | Vertical(_, startY, endY) -> endY >= startY
    
    // Debugging helpers:
    if not touchesHunterBeforeMove then 
        logRed "[HBUG] Created wall is invalid: does not touch hunter before move. Ignoring."
    elif not doesNotCollideHunter then 
        logRed "[HBUG] Created wall is invalid: collides hunter after move. Ignoring."
    elif not afterWallInterval then 
        logRed $"[HBUG] Created wall is invalid: attempted to create wall too soon after last wall time ({game.HunterLastWall |> Option.get}). Ignoring."
    elif not hasNotExceededMaxWalls then 
        logRed "[HBUG] Created wall is invalid: exceeded maximum number of walls. Ignoring."
    elif not doesNotCollidePrey then 
        logRed "[HBUG] Created wall is invalid: collides current prey position. Ignoring."
    elif not withinBounds then 
        logRed "[HBUG] Created wall is invalid: out of bounds. Ignoring."
    elif not doesNotCollideWalls then 
        logRed "[HBUG] Created wall is invalid: collides with other walls. Ignoring."
    elif not endPointsAfterStartPoints then 
        logRed "[HBUG] Created wall is invalid: end points are before start points. Ignoring."
    else ()


    touchesHunterBeforeMove
    && doesNotCollideHunter
    && afterWallInterval
    && hasNotExceededMaxWalls
    && doesNotCollidePrey
    && withinBounds
    && doesNotCollideWalls
    && endPointsAfterStartPoints


/// Step the game forward by one unit of time, given a hunter action and a prey action.
/// Note that:
/// - Prey actions can only be taken on odd ticks. Actions on even ticks are ignored.
/// - The order of calculation is create/destroy walls, move the hunter, move the prey.
/// Signum of velocity is taken in this function.
let step (game: State) (hunterAction: HunterAction) (preyAction: PreyAction) : State =

    // Create and destroy walls
    let (walls, lastWall) =
        match hunterAction with
        | CreateWall wall ->
            if wallIsValid game wall then
                (wall :: game.Walls, Some(game.Ticker))
            else
                (game.Walls, game.HunterLastWall)
        | RemoveWall wall -> (removeWall wall game.Walls, game.HunterLastWall)
        | HunterNoAction -> (game.Walls, game.HunterLastWall)

    // Calculate the new hunter position
    let (newHunterPosition, newHunterVelocity) =
        stepAndBounce game.HunterVelocity walls game.HunterPosition

    // Change the prey velocity if the prey action is valid i.e. ticker is odd tick
    let proposedPreyVelocity =
        if game.Ticker % 2<s> = 1<s> then
            match preyAction with
            | ChangeVelocity velocity -> velocity
            | PreyNoAction -> game.PreyVelocity
        else
            game.PreyVelocity

    // Calculate the new prey position
    let (newPreyPosition, newPreyVelocity) =
        stepAndBounce proposedPreyVelocity walls game.PreyPosition

    // Return the new game state
    { game with
        Ticker = game.Ticker + 1<s>
        HunterPosition = newHunterPosition
        HunterVelocity = newHunterVelocity
        HunterLastWall = lastWall
        PreyPosition = newPreyPosition
        PreyVelocity = newPreyVelocity
        Walls = walls }

/// Steps the game and returns an outcome.
let stepOutcome (game: State) (hunterAction: HunterAction) (preyAction: PreyAction) : Outcome =
    let newState = step game hunterAction preyAction
    if distance newState.HunterPosition newState.PreyPosition <= 4 then 
        PreyIsCaught newState
    else
        Continues newState
