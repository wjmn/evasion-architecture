module Game exposing (..)

import Array exposing (Array)



--------------------------------------------------------------------------------
-- BASIC TYPES
--------------------------------------------------------------------------------


type alias Seconds =
    Int


type alias Metres =
    Int


type alias MetresPerSecond =
    Int


type alias Point =
    { x : Metres
    , y : Metres
    }


type alias Velocity =
    { x : MetresPerSecond
    , y : MetresPerSecond
    }


type alias Wall =
    { x1 : Metres
    , x2 : Metres
    , y1 : Metres
    , y2 : Metres
    }


type Role
    = Hunter
    | Prey


type alias Config =
    { nextWallTime : Seconds
    , maximumWalls : Int
    }


type alias Game =
    { ticker : Seconds
    , hunterPosition : Point
    , hunterVelocity : Velocity
    , hunterLastWallTime : Maybe Seconds
    , preyPosition : Point
    , preyVelocity : Velocity
    , walls : List Wall
    }

type Outcome
    = Continues (Float, Float, Game)
    | PreyIsCaught (Float, Float, Game)
    | PreyTimeout (Float, Float, Game)
    | End


--------------------------------------------------------------------------------
-- UTILITY FUCNTIONS
--------------------------------------------------------------------------------


{-| Just for convenience - this should be unreachable from the main code as it
the server guarantees the output is valid.
-}
dummyGame : Game
dummyGame =
    { ticker = 0
    , hunterPosition = { x = 0, y = 0 }
    , hunterVelocity = { x = 0, y = 0 }
    , hunterLastWallTime = Nothing
    , preyPosition = { x = 0, y = 0 }
    , preyVelocity = { x = 0, y = 0 }
    , walls = []
    }


{-| Decodes int at position in array, with default 0.
-}
decodeIntAt : Array String -> Int -> Int
decodeIntAt array index =
    Array.get index array
        |> Maybe.andThen String.toInt
        |> Maybe.withDefault 0

decodeConfig : String -> Config
decodeConfig string =
    let
        splits =
            String.split " " string |> Array.fromList

        nextWallTime =
            decodeIntAt splits 0

        maximumWalls =
            decodeIntAt splits 1
    in
    { nextWallTime = nextWallTime
    , maximumWalls = maximumWalls
    }

{-| Decodes a game from a string.
-}
decodeGame : String -> Game
decodeGame string =
    let
        splits =
            String.split " " string |> Array.fromList

        ticker =
            decodeIntAt splits 0

        hunterPosition =
            { x = decodeIntAt splits 1
            , y = decodeIntAt splits 2
            }

        hunterVelocity =
            { x = decodeIntAt splits 3
            , y = decodeIntAt splits 4
            }

        hunterLastWallTime =
            if Array.get 5 splits == Just "null" then
                Nothing

            else
                Just (decodeIntAt splits 5)

        preyPosition =
            { x = decodeIntAt splits 6
            , y = decodeIntAt splits 7
            }

        preyVelocity =
            { x = decodeIntAt splits 8
            , y = decodeIntAt splits 9
            }

        numWalls =
            decodeIntAt splits 10

        walls =
            List.range 0 numWalls
                |> List.map
                    (\i ->
                        { x1 = decodeIntAt splits (11 + i * 4)
                        , x2 = decodeIntAt splits (12 + i * 4)
                        , y1 = decodeIntAt splits (13 + i * 4)
                        , y2 = decodeIntAt splits (14 + i * 4)
                        }
                    )
    in
    { ticker = ticker
    , hunterPosition = hunterPosition
    , hunterVelocity = hunterVelocity
    , hunterLastWallTime = hunterLastWallTime
    , preyPosition = preyPosition
    , preyVelocity = preyVelocity
    , walls = walls
    }

decodeConfigAndGame : String -> (Config, Game)
decodeConfigAndGame string =
    let
        splits =
            String.split " " string |> Array.fromList

        config =
            Array.slice 0 2 splits
            |> Array.toList
            |> String.join " "
            |> decodeConfig

        game =
            Array.slice 2 (Array.length splits) splits
            |> Array.toList
            |> String.join " "
            |> decodeGame
    in
    (config, game)

decodeGameAndTimeRemaining : String -> (Float, Float, Game)
decodeGameAndTimeRemaining string =
    let
        splits =
            String.split " " string |> Array.fromList

        timeRemainingHunter =
            Array.get 0 splits
            |> Maybe.andThen String.toFloat
            |> Maybe.withDefault 0.0
        
        timeRemainingPrey = 
            Array.get 1 splits
            |> Maybe.andThen String.toFloat
            |> Maybe.withDefault 0.0

        game =
            Array.slice 2 (Array.length splits) splits
            |> Array.toList
            |> String.join " "
            |> decodeGame
    in
    (timeRemainingHunter, timeRemainingPrey, game)

decodeLoopState : String -> Outcome
decodeLoopState string = 
    let
        splits =
            String.split " " string |> Array.fromList

        discriminator =
            Array.get 0 splits
            |> Maybe.withDefault "end"
   in
    case discriminator of
        "continues" ->
            Continues (decodeGameAndTimeRemaining (Array.slice 1 (Array.length splits) splits |> Array.toList |> String.join " "))

        "preyIsCaught" ->
            PreyIsCaught (decodeGameAndTimeRemaining (Array.slice 1 (Array.length splits) splits |> Array.toList |> String.join " "))

        "preyTimeout" ->
            PreyTimeout (decodeGameAndTimeRemaining (Array.slice 1 (Array.length splits) splits |> Array.toList |> String.join " "))

        _ ->
            End
