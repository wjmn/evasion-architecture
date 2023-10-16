module Server

open Game
open Game.Field
open Game.Game
open System
open System.Net
open System.Net.Sockets
open System.Text
open System.IO
open System.Threading

//-------------------------------------------------------------------------------
// CONSTANTS
//-------------------------------------------------------------------------------

/// Number of milliseconds each player is allowed in total to make all their moves.
[<Literal>]
let InitialTime = 120000.0 

//-------------------------------------------------------------------------------
// TYPES & UTILITY FUNCS
//-------------------------------------------------------------------------------

/// Final possible outcome of game run on server.
type Outcome = 
    | PreyTimedOut of At: int<s>
    | PreyIsCaught of At: int<s>
    | NoOutcome

/// Wrapper around writing to streams to ensure flushing.
let writeTo (writer: StreamWriter) (message: string) = 
    writer.WriteLine(message)
    writer.Flush()

//-------------------------------------------------------------------------------
// MAIN SERVER
//-------------------------------------------------------------------------------

/// Game server. 
/// 
/// Basic structure:
/// 1. Initialise server and game
/// 2. Await connection from observer
/// 3. Await connection from hunter
/// 4. Await connection from prey
/// 5. Send observer the hunter name, prey name, then configuration + initial game state
/// 6. Send hunter and prey their role and configuration variables
/// 7. Start the main game loop after a 2 second delay
///     1. Async await the hunter action (as soon as it is received, their timer is stopped)
///     2. Async await the prey action (as soon as it is received, their timer is stopped)
///     3. Once both received, 
type Server(port: int, config: Game.Config) = 

    let listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port)

    member this.Run() =

        listener.Start()
        log $"[INIT] Server listening on 127.0.0.1:{port}."
    
        // Initialise the game
        let mutable game = Game.newGame config
        log $"[INIT] Initialized game with config [{Game.Serialization.encodeConfig config}]."
    
        // Accept socket connections in order of observer, hunter, prey.
        let observerHandle = listener.AcceptTcpClient()
        let observerStream = observerHandle.GetStream()
        let observerWriter = new StreamWriter(observerStream, new UTF8Encoding(false))
        log $"[CONN] Observer connected from {observerHandle.Client.RemoteEndPoint}"

        let hunterHandle = listener.AcceptTcpClient()
        let hunterStream = hunterHandle.GetStream()
        let hunterReader = new StreamReader(hunterStream, new UTF8Encoding(false))
        let hunterWriter = new StreamWriter(hunterStream, new UTF8Encoding(false))
        let hunterName = hunterReader.ReadLine()
        log $"[CONN] Hunter [{hunterName}] connected from {hunterHandle.Client.RemoteEndPoint}"

        let preyHandle = listener.AcceptTcpClient()
        let preyStream = preyHandle.GetStream()
        let preyReader = new StreamReader(preyStream, new UTF8Encoding(false))
        let preyWriter = new StreamWriter(preyStream, new UTF8Encoding(false))
        let preyName = preyReader.ReadLine()
        log $"[CONN] Prey [{preyName}] connected from {preyHandle.Client.RemoteEndPoint}"

        // Send the observer the initial names, configuration and game state
        writeTo observerWriter hunterName
        Thread.Sleep(10)
        writeTo observerWriter preyName
        Thread.Sleep(10)
        writeTo observerWriter $"{Game.Serialization.encodeConfig config} {Game.Serialization.encodeGame game}"
        Thread.Sleep(10)
        log $"[SEND] Game configuration and game state sent to observer."

        // Send connection message which is the role + config 
        Async.Start(async { writeTo hunterWriter ($"hunter {Serialization.encodeConfig config}")})
        Async.Start(async { writeTo preyWriter ($"prey {Serialization.encodeConfig config}") })
        log $"[SEND] Game config sent to both hunter and prey. Starting game in 2 seconds."

        // Start the game after a two second delay
        Thread.Sleep(2000)

        // TIme remaining in milliseconds for each player
        let mutable hunterTimeRemaining = InitialTime
        let mutable preyTimeRemaining = InitialTime

        // Loop variables and final outcome variable
        let mutable stop = false
        let mutable outcome = NoOutcome

        while not stop do

            let iterationStart = DateTime.Now 
            logYellow $"[GAME] Current game state: {Serialization.encodeGame game}"

            // Cancellation tokens
            let hunterCts = new CancellationTokenSource(int (Math.Round(hunterTimeRemaining)))
            let hunterCt = hunterCts.Token
            let preyCts = new CancellationTokenSource(int (Math.Round(preyTimeRemaining)))
            let preyCt = preyCts.Token

            // Initialise the taks to read the hunter string
            let hunterActionStringAsync = 
                if hunterTimeRemaining > 0.0 then 
                    task {
                        writeTo hunterWriter (Serialization.encodeGame game)
                        log $"[SEND] Sent game state to hunter. Awaiting reply."
                        let startTime = DateTime.Now
                        let! hunterActionString =  hunterReader.ReadLineAsync(hunterCt)
                        let endTime = DateTime.Now
                        let timeTaken = (endTime - startTime).TotalMilliseconds
                        hunterTimeRemaining <- hunterTimeRemaining - timeTaken
                        log $"[READ] Received action [{hunterActionString}] from hunter after %.5f{timeTaken / 1000.0}ms. Hunter has %.4f{hunterTimeRemaining/1000.0}s remaining."
                        return hunterActionString
                    }
                else
                    log $"[INFO] Hunter has timed out and is no longer allowed to make further actions."
                    task { return "none" }

            // Initialise the task to read the prey string
            let preyActionStringAsync = task {
                // Only request an action from prey every odd tick
                if game.Ticker % 2<s> = 1<s> then 
                    writeTo preyWriter (Game.Serialization.encodeGame game)
                    log $"[SEND] Sent game state to prey. Awaiting reply."
                    let startTime = DateTime.Now
                    let! preyActionString = preyReader.ReadLineAsync(preyCt) 
                    let endTime = DateTime.Now
                    let timeTaken = (endTime - startTime).TotalMilliseconds
                    preyTimeRemaining <- preyTimeRemaining - timeTaken
                    log $"[READ] Received action [{preyActionString}] from prey after %.5f{timeTaken / 1000.0}s. Prey has %.4f{preyTimeRemaining/1000.0}s remaining."
                    return preyActionString
                else
                    log $"[INFO] Prey is not allowed to make an action on this tick."
                    return "none"
            }

            // Await the hunter task 
            try hunterActionStringAsync.Wait(hunterCt) with 
                | :? OperationCanceledException -> 
                    log $"[TIME] Hunter timed out. Play continues but the hunter may make no further actions."
                    hunterTimeRemaining <- 0.0
            let hunterActionString = 
                try hunterActionStringAsync.Result with 
                | :? AggregateException -> 
                    "none"

            // Await the prey task
            try preyActionStringAsync.Wait(preyCt) with 
                | :? OperationCanceledException -> 
                    log $"[TIME] Prey timed out. The game has now concluded."
                    writeTo observerWriter $"timeout %.2f{hunterTimeRemaining} %.2f{preyTimeRemaining} {Serialization.encodeGame game}"
                    stop <- true
                    outcome <- PreyTimedOut(At = game.Ticker)
                    preyTimeRemaining <- 0.0
            let preyActionString = 
                try preyActionStringAsync.Result with 
                | :? AggregateException  -> 
                    "none"
            
            // If prey still has time remaining, then compute the next step: otherwise, conclude the game
            if preyTimeRemaining > 0.0 then 

                let hunterAction = 
                    try Serialization.decodeHunterAction hunterActionString with
                    | e -> 
                        log $"[INFO] Invalid hunter action string {hunterActionString}. Ignoring hunter action."
                        Game.HunterNoAction

                let preyAction = 
                    try Serialization.decodePreyAction preyActionString with 
                    | e -> 
                        log $"[INFO] Invalid prey action string {preyActionString}. Ignoring prey action."
                        Game.PreyNoAction

                // Check time for a single step for debugging
                let stepStart = DateTime.Now

                let stepped = stepOutcome game hunterAction preyAction

                // Timer variables
                let iterationEnd = DateTime.Now
                let stepTimeTaken = (iterationEnd - stepStart).TotalMilliseconds
                let iterationTimeTaken = (iterationEnd - iterationStart).TotalMilliseconds

                log $"[INFO] Iteration time {iterationTimeTaken}ms. Step time {stepTimeTaken}ms."

                // Check if prey is caught or if game continues
                match stepped with 
                | Game.Continues steppedGame -> 
                    game <- steppedGame
                    writeTo observerWriter $"continues %.2f{hunterTimeRemaining} %.2f{preyTimeRemaining} {Serialization.encodeGame game}"
                    // Don't update the frame any faster than 100fps
                    if iterationTimeTaken < 10.0 then 
                        Thread.Sleep(int <| Math.Round(10.0 - iterationTimeTaken))
                | Game.PreyIsCaught steppedGame -> 
                    game <- steppedGame
                    writeTo observerWriter $"caught %.2f{hunterTimeRemaining} %.2f{preyTimeRemaining} {Serialization.encodeGame game}"
                    stop <- true 
                    outcome <- PreyIsCaught(At = game.Ticker)

            // Prey has timed out: conclude the game
            else
                log $"[TIME] Prey timed out. The game has now concluded."
                writeTo observerWriter $"timeout %.2f{hunterTimeRemaining} %.2f{preyTimeRemaining} {Serialization.encodeGame game}"
                stop <- true
                outcome <- PreyTimedOut(At = game.Ticker)


        match outcome with 
        | PreyTimedOut(At = t) -> 
            logGreen $"[FINN] >>> Prey [{preyName}] escaped evasion until game ticker={t} when prey's clock-time timeout occurred <<<"
        | PreyIsCaught(At = t) -> 
            logBlue $"[FINN] >>> Prey [{preyName}] was caught at game ticker={t} <<<"
        | NoOutcome -> 
            log $"[FINN] No outcome was recorded. This should be unreachable and indicates a bug in the server."

        log $"[CLSE] Game has concluded. Shutting down server."

        // Write end to all streams
        writeTo observerWriter $"end"
        writeTo hunterWriter $"end"
        writeTo preyWriter $"end"

    interface IDisposable with 
        member this.Dispose() = 
            listener.Stop()


[<EntryPoint>]
let main args = 
    match args with 

    // 3 arguments given
    | [| port; nextWallInterval; maxWalls |] -> 
        let config = 
            { NextWallInterval = s <| int nextWallInterval
              MaxWalls = int maxWalls }
        let server = new Server(int port, config)
        server.Run()

    // 2 arguments given
    | [| nextWallInterval; maxWalls |] ->
        let config = 
            { NextWallInterval = s <| int nextWallInterval
              MaxWalls = int maxWalls }
        let server = new Server(4000, config)
        server.Run()

    // Any other number of arguments given
    | _ -> 
        printfn "Invalid number of arguments given for the server."
        printfn "Usage:"
        printfn "    evasion PORT N M"
        printfn "         Will run game on 127.0.0.1 port PORT with configuration values N and M."
        printfn "    evasion N M"
        printfn "         Will run game on 127.0.0.1 port 4000 with configuration values N and M."
    0 