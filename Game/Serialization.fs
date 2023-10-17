module Game.Serialization

open Game.Game
open Game.Field

exception ParseException of string

/// Walls are encoded in the form:
/// 
/// `x1 y1 x2 y2`
/// 
/// For convenience, each wall is prepended with a space for easy concatenation into a wall list.
let encodeWall (wall: Wall) =
    match wall with 
    | Horizontal(y, x1, x2) -> $" {x1} {y} {x2} {y}"
    | Vertical(x, y1, y2) -> $" {x} {y1} {x} {y2}"

/// Note: this assumes either x1 = x2 or y1 = y2. 
/// If x1 != x2 and y1 != y2 then this will raise an exception.
/// Other checks of validity are done at game step calculation time. 
/// 
/// Will raise an exception if input is invalid!
let decodeWall (x1: string) (y1: string) (x2: string) (y2: string) =
    if x1 = x2 then Vertical(m (int x1), m (int y1), m (int y2))
    elif y1 = y2 then Horizontal(m (int y1), m (int x1), m (int x2))
    else raise <| ParseException("Invalid wall on parsing: neither x1!=x2 nor y1!=y2.")

/// The config is encoded in the form: 
/// 
/// `N M`
let encodeConfig (config: Config) = 
    $"{config.NextWallInterval} {config.MaxWalls}"

/// The game is encoded in the form:
/// 
/// `Time HunterX HunterY HunterVelocityX HunterVelocityY HunterLastWall PreyX PreyY PreyVelocityX PreyVelocityY NumWalls Walls`
/// 
/// Examples:
/// - `0 0 0 1 1 null 230 200 0 0 0` for the initial game state
/// - `30 31 31 1 1 20 30 200 1 1 1 0 31 300 31` for a game state with a single horizontal wall
/// 
/// Please note the following:
/// - HunterLastWall will be either a number or the word null indicating no wall has yet been placed.
/// - Walls is a list of walls encoded as described in encodeWall (space-separated x1 y1 x2 y2)
let encodeGame (state: State) =
    let numWalls = state.Walls |> List.length
    let walls = state.Walls |> List.map encodeWall |> String.concat ""
    let hunterLastWall = match state.HunterLastWall with Some(t) -> t.ToString() | None -> "null"
    $"{state.Ticker} {state.HunterPosition.X} {state.HunterPosition.Y} {state.HunterVelocity.X} {state.HunterVelocity.Y} {hunterLastWall} {state.PreyPosition.X} {state.PreyPosition.Y} {state.PreyVelocity.X} {state.PreyVelocity.Y} {numWalls}{walls}"

/// Decodes the game as encoded by encodeGame.
let decodeGame (N: int) (M: int) (json: string) =
    let split = json.Split()
    let ticker = s <| int split.[0]
    let hunterPosition: Point = { X = m <| int split.[1]; Y = m <| int split.[2] }
    let hunterVelocity = { X = int split.[3] * 1<m/s>; Y = int split.[4] * 1<m/s> }
    let hunterLastWall = if split.[5] = "null" then None else Some(s <| int split.[5])
    let preyPosition: Point = { X = m <| int split.[6]; Y = m <|int split.[7] }
    let preyVelocity = { X = int split.[8] * 1<m/s>; Y = int split.[9] * 1<m/s> }
    let numWalls = int split.[10]
    let walls = 
        let rec loop (i: int) (acc: Wall list) =
            if i = numWalls then acc
            else loop (i + 1) (decodeWall split[11+i*4] split[12+i*4] split[13+i*4] split[14+i*4] :: acc)
        loop 0 []
    { Config = { NextWallInterval = s N; MaxWalls = M}
      Ticker = ticker
      HunterPosition = hunterPosition
      HunterVelocity = hunterVelocity
      HunterLastWall = hunterLastWall
      PreyPosition = preyPosition
      PreyVelocity = preyVelocity
      Walls = walls }


let encodeHunterAction (action: HunterAction) =
    match action with
    | RemoveAndCreate(remove, create) -> ([ for wall in remove do $"remove{encodeWall wall}" ] |> String.concat "") + $"create{encodeWall create}"
    | CreateWall(wall) -> $"create{encodeWall wall}"
    | RemoveWalls(walls) -> [ for wall in walls do $"remove{encodeWall wall}" ] |> String.concat ""
    | HunterNoAction -> "none"

let encodePreyAction (action: PreyAction) =
    match action with 
    | ChangeVelocity(velocity) -> $"change {velocity.X} {velocity.Y}"
    | PreyNoAction -> "none"

/// A hunter can remove any number of walls (including none), plus either create a wall or do nothing. 
/// 
/// Below shows how each of these actions should be encoded for the decoder to work:
/// - `remove x1 y1 x2 y2 remove x1 y1 x2 y2 create x1 y1 x2 y2`
/// - `create x1 y1 x2 y2`
/// - `remove x1 y1 x2 y2 remove x1 y1 x2 y2`
/// - `none`
/// 
/// You MUST put all the walls you want to remove before the create action. After the create word is found, 
/// it will process the create action and then ignore any further words.
/// 
/// Can raise an exception if input is invalid!
/// 
/// Any other words count as no action.
/// 
/// Please ensure you do not send an exorbitant amount of non-existent walls to remove...
let decodeHunterAction (string: string) =
    let split = string.Split()
    match split[0] with 
    | "create" -> CreateWall(decodeWall split.[1] split.[2] split.[3] split.[4])
    | "none" -> HunterNoAction
    | "remove" -> 
        let wallsToRemove = ResizeArray()
        let mutable i = 0 
        while i < split.Length && split[i] = "remove" do 
            wallsToRemove.Add(decodeWall split.[i+1] split.[i+2] split.[i+3] split.[i+4])
            i <- i + 5
        if i < split.Length && split[i] = "create" then
            let wallToCreate = decodeWall split.[i+1] split.[i+2] split.[i+3] split.[i+4]
            RemoveAndCreate(wallsToRemove |> List.ofSeq, wallToCreate)
        else
            RemoveWalls(wallsToRemove |> List.ofSeq)
    | _ -> HunterNoAction

/// There are two possible prey actions: change velocity or do nothing.
/// 
/// Below shows how each of these actions should be encoded for the decoder to work:
/// - `change vx vy`
/// - `none`
/// 
/// Will raise an exception if input is invalid!
/// 
/// vx and vy can only be -1, 1 or 0. If they are not, they will be coerced to these values via signum.
/// 
/// Any other words count as no action.
let decodePreyAction (json: string) =
    let split = json.Split()
    match split[0] with 
    | "change" -> ChangeVelocity(signum { X = int split.[1] * 1<m/s>; Y = int split.[2] * 1<m/s> })
    | "none" -> PreyNoAction
    | _ -> PreyNoAction