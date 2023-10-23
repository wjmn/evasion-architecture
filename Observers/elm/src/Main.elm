port module Main exposing (..)

import Browser
import Game exposing (..)
import Html exposing (..)
import Html.Attributes exposing (..)
import Html.Events exposing (onClick)
import Json.Decode as Decode
import Json.Encode as Encode
import Json.Decode as Decode



---- PORTS ----


port messageReceiver : (String -> msg) -> Sub msg


port savePreviousString : String -> Cmd msg


savePrevious : List PreviousOutcome -> Cmd msg
savePrevious previous = Encode.list encodePrevious previous |> Encode.encode 0 |> savePreviousString


--- ENCODING/DECODING PREVIOUS OUTCOMES ----


encodePrevious : PreviousOutcome -> Encode.Value
encodePrevious previous =
    case previous of
        PreviousPreyIsCaught { hunterName, preyName, catchTime, catchPoint } ->
            Encode.object
                [ ( "outcome", Encode.string "caught" )
                , ( "hunterName", Encode.string hunterName )
                , ( "preyName", Encode.string preyName )
                , ( "catchTime", Encode.int catchTime )
                , ( "catchPoint"
                  , Encode.object
                        [ ( "x", Encode.int catchPoint.x )
                        , ( "y", Encode.int catchPoint.y )
                        ]
                  )
                ]

        PreviousPreyTimeout { hunterName, preyName, ticker } ->
            Encode.object
                [ ( "outcome", Encode.string "timeout" )
                , ( "hunterName", Encode.string hunterName )
                , ( "preyName", Encode.string preyName )
                , ( "ticker", Encode.int ticker )
                ]


decoderTimeout =
    Decode.map3 PreviousPreyTimeoutData
        (Decode.field "hunterName" Decode.string)
        (Decode.field "preyName" Decode.string)
        (Decode.field "ticker" Decode.int)
    |> Decode.map PreviousPreyTimeout


decoderCaught =
    Decode.map4 (PreviousPreyIsCaughtData)
        (Decode.field "hunterName" Decode.string)
        (Decode.field "preyName" Decode.string)
        (Decode.field "catchTime" Decode.int)
        (Decode.field "catchPoint" <|
            Decode.map2 Point
                (Decode.field "x" Decode.int)
                (Decode.field "y" Decode.int)
        )
    |> Decode.map PreviousPreyIsCaught


decodePrevious : String -> List PreviousOutcome
decodePrevious string =
    Decode.decodeString (Decode.list (Decode.oneOf [ decoderTimeout, decoderCaught ])) string
        |> Result.withDefault []



---- MODEL ----


type State
    = AwaitingConnection
    | AwaitingHunterName
    | AwaitingPreyName
    | AwaitingInitialConfigAndGame
    | GameLoop
    | FinishedCaught
    | FinishedTimeout


type alias PreviousPreyIsCaughtData =
    { hunterName : String, preyName : String, catchTime : Seconds, catchPoint : Point }


type alias PreviousPreyTimeoutData =
    { hunterName : String, preyName : String, ticker : Seconds }


type PreviousOutcome
    = PreviousPreyIsCaught PreviousPreyIsCaughtData
    | PreviousPreyTimeout PreviousPreyTimeoutData


type alias Model =
    { state : State
    , hunterName : String
    , preyName : String
    , configuration : Config
    , game : Game
    , hunterTimeRemaining : Float
    , preyTimeRemaining : Float
    , previousOutcomes : List PreviousOutcome
    }


default : List PreviousOutcome -> Model
default previous =
    { state = AwaitingConnection
    , hunterName = "HUNTER"
    , preyName = "PREY"
    , configuration = { nextWallTime = 0, maximumWalls = 0 }
    , game = dummyGame
    , hunterTimeRemaining = 0
    , preyTimeRemaining = 0
    , previousOutcomes = previous
    }


init : String -> ( Model, Cmd Msg )
init jsonString =
    ( default (decodePrevious jsonString) , Cmd.none )



---- UPDATE ----


type Msg
    = NoOp
    | ClickedDeletePreviousOutcome PreviousOutcome
    | PortMessageReceived String


withCmdNone : Model -> ( Model, Cmd Msg )
withCmdNone model =
    ( model, Cmd.none )

withCmd : Cmd Msg -> Model -> ( Model, Cmd Msg )
withCmd cmd model =
    ( model, cmd )




update : Msg -> Model -> ( Model, Cmd Msg )
update msg model =
    case msg of
        NoOp ->
            ( model, Cmd.none )

        ClickedDeletePreviousOutcome item ->
            let 
                newPrevious = List.filter (\x -> x /= item) model.previousOutcomes 
            in
            { model | previousOutcomes = newPrevious }
                |> withCmd (savePrevious newPrevious)

        PortMessageReceived message ->
            case model.state of
                AwaitingConnection ->
                    { model | state = AwaitingHunterName }
                        |> withCmdNone

                AwaitingHunterName ->
                    { model | hunterName = message, state = AwaitingPreyName }
                        |> withCmdNone

                AwaitingPreyName ->
                    { model | preyName = message, state = AwaitingInitialConfigAndGame }
                        |> withCmdNone

                AwaitingInitialConfigAndGame ->
                    let
                        ( config, game ) =
                            decodeConfigAndGame message
                    in
                    { model | configuration = config, game = game, state = GameLoop }
                        |> withCmdNone

                GameLoop ->
                    case decodeLoopState message of
                        Continues ( hunterTimeRemaining, preyTimeRemaining, game ) ->
                            { model | hunterTimeRemaining = hunterTimeRemaining, preyTimeRemaining = preyTimeRemaining, game = game, state = GameLoop }
                                |> withCmdNone

                        PreyIsCaught ( hunterTimeRemaining, preyTimeRemaining, game ) ->
                            let 
                                newPrevious = model.previousOutcomes ++ [ PreviousPreyIsCaught { hunterName = model.hunterName, preyName = model.preyName, catchTime = game.ticker, catchPoint = game.preyPosition } ] 

                            in
                            { model | hunterTimeRemaining = hunterTimeRemaining, preyTimeRemaining = preyTimeRemaining, game = game, state = FinishedCaught, previousOutcomes = newPrevious }
                                |> withCmd (savePrevious newPrevious)

                        PreyTimeout ( hunterTimeRemaining, preyTimeRemaining, game ) ->
                            let 
                                newPrevious = model.previousOutcomes ++ [ PreviousPreyTimeout { hunterName = model.hunterName, preyName = model.preyName, ticker = game.ticker} ] 
                            in

                            { model | hunterTimeRemaining = hunterTimeRemaining, preyTimeRemaining = preyTimeRemaining, game = game, state = FinishedTimeout, previousOutcomes = newPrevious }
                                |> withCmd (savePrevious newPrevious)

                        End ->
                            ( model, Cmd.none )

                _ ->
                    ( model, Cmd.none )



---- CONSTANTS ----
-- Width of indicator used for prey/hunter as percentage


dotWidth : Float
dotWidth =
    0.33


wallWidth : Float
wallWidth =
    0.33


hunterDecoratorWidth : Float
hunterDecoratorWidth =
    2


preyDecorationWidth : Float
preyDecorationWidth =
    0.67



---- UTILITY FUCTIONS ----


tw : String -> Attribute msg
tw classes =
    classList [ ( classes, True ) ]


twIf : Bool -> String -> Attribute msg
twIf bool classes =
    classList [ ( classes, bool ) ]


{-| Converts x coordinate into percentage value of grid
-}
toLeftValue : Int -> Float -> Float
toLeftValue x iconWidth =
    (toFloat x / toFloat Game.maxWidth) * 100.0 - iconWidth / 2


toTopValue : Int -> Float -> Float
toTopValue y iconWidth =
    (toFloat y / toFloat Game.maxHeight) * 100.0 - iconWidth / 2


pct : Float -> String
pct value =
    String.fromFloat value ++ "%"


viewWall : Wall -> Html Msg
viewWall wall =
    if wall.x1 == wall.x2 then
        div
            [ style "left" (pct <| toLeftValue wall.x1 wallWidth)
            , style "top" (pct <| toTopValue wall.y1 wallWidth)
            , style "height" (pct <| toTopValue wall.y2 wallWidth - toTopValue wall.y1 wallWidth + dotWidth)
            , style "width" (pct wallWidth)
            , tw "absolute wall border-l"
            ]
            []

    else
        div
            [ style "left" (pct <| toLeftValue wall.x1 wallWidth)
            , style "top" (pct <| toTopValue wall.y1 wallWidth)
            , style "width" (pct <| toLeftValue wall.x2 wallWidth - toLeftValue wall.x1 wallWidth + dotWidth)
            , style "height" (pct wallWidth)
            , tw "absolute wall border-t"
            ]
            []


isCaught : State -> Bool
isCaught state =
    case state of
        FinishedCaught ->
            True

        _ ->
            False


isTimeout : State -> Bool
isTimeout state =
    case state of
        FinishedTimeout ->
            True

        _ ->
            False


awaitingConnection state =
    case state of
        AwaitingConnection ->
            True

        _ ->
            False


awaitingGameServer : State -> Bool
awaitingGameServer state =
    case state of
        AwaitingHunterName ->
            True

        AwaitingPreyName ->
            True

        AwaitingInitialConfigAndGame ->
            True

        _ ->
            False



---- VIEW ----


catchFlash : { a | state : State } -> Html msg
catchFlash model =
    if isCaught model.state then
        div [ tw "absolute top-0 left-0 w-screen h-screen catch-flash flex justify-center items-center pointer-events-none" ] []

    else
        div [] []


viewHistoryItem : Int -> PreviousOutcome -> Html Msg
viewHistoryItem index item =
    let
        ( mb, ruler ) =
            if modBy 2 index == 1 then
                ( "mb-3", hr [ tw "mt-3" ] [ text "RULER" ] )

            else
                ( "mb-2", div [] [] )
    in
    case item of
        PreviousPreyIsCaught { hunterName, preyName, catchTime, catchPoint } ->
            div [ tw "flex-col cursor-pointer hover:text-red-500", tw mb, onClick (ClickedDeletePreviousOutcome item) ]
                [ div [ tw "flex items-center font-bold justify-between" ]
                    [ div [ tw "mr-2" ] [ text preyName ]
                    , div [ tw "text-red-500" ] [ text (String.fromInt catchTime) ]
                    ]
                , div [ tw "text-neutral-400" ] [ text <| "vs hunter: " ++ hunterName ]
                , ruler
                ]

        PreviousPreyTimeout { hunterName, preyName, ticker } ->
            let
                ( textCol, tickerString ) =
                    if ticker >= 24000 then
                        ( "text-green-500", "INFINITY" )

                    else
                        ( "text-gray-300", String.fromInt ticker )
            in
            div [ tw "flex-col cursor-pointer hover:text-red-500", tw mb, onClick (ClickedDeletePreviousOutcome item) ]
                [ div [ tw "flex items-center font-bold justify-between" ]
                    [ div [ tw "mr-2" ] [ text preyName ]
                    , div [ tw textCol ] [ text tickerString ]
                    ]
                , div [ tw "text-neutral-400" ] [ text <| "vs hunter: " ++ hunterName ]
                , ruler
                ]


viewGrave : PreviousOutcome -> Html Msg
viewGrave item =
    case item of
        PreviousPreyIsCaught { preyName, catchPoint } ->
            div
                [ style "left" (pct <| toLeftValue catchPoint.x dotWidth)
                , style "top" (pct <| toTopValue catchPoint.y dotWidth)
                , tw "absolute rounded-full opacity-30 cross"
                , class "grave"
                ]
                [ div [ tw "text-tiny text-neutral-200" ] [ text preyName ] ]

        _ ->
            div [] []


view : Model -> Html Msg
view model =
    div [ tw "bg-black h-screen flex justify-center items-center text-white p-4" ]
        [ div [ tw "flex justify-center" ]
            [ div [ tw "h-full w-72 mr-8 overflow-auto" ]
                [ div [ tw "flex justify-between items-center mb-6" ] [ h1 [ tw "uppercase font-bold" ] [ text "History" ], div [ tw "text-xs" ] [ text "(click to delete)" ] ]
                , div [ tw "text-xs" ] (List.indexedMap viewHistoryItem model.previousOutcomes)
                ]
            , div [ tw "h-full flex justify-center items-center mr-8" ]
                [ div [ tw "border-white border-2" ]
                    [ div [ style "height" "90vh", style "width" "90vh", tw "relative overflow-hidden" ]
                        [ --- Graveyard
                          div [] (List.map viewGrave model.previousOutcomes)
                        , --- WALS
                          div [] (List.map viewWall model.game.walls)

                        -- PREY INDICATOR
                        , div
                            [ style "left" (pct <| toLeftValue model.game.preyPosition.x dotWidth)
                            , style "top" (pct <| toTopValue model.game.preyPosition.y dotWidth)
                            , style "width" (pct dotWidth)
                            , style "height" (pct dotWidth)
                            , tw "absolute bg-white rounded-full relative"
                            , twIf (isCaught model.state) "bg-red-500 cross"
                            , class "prey-indicator"
                            ]
                            []
                        , div
                            [ style "left" (pct <| toLeftValue model.game.preyPosition.x preyDecorationWidth)
                            , style "top" (pct <| toTopValue model.game.preyPosition.y preyDecorationWidth)
                            , style "width" (pct preyDecorationWidth)
                            , style "height" (pct preyDecorationWidth)
                            , tw "absolute bg-white rounded-full"
                            , twIf (isCaught model.state) "hidden"
                            , class "prey-decorator"
                            ]
                            []

                        -- HUNTER INDICATOR
                        , div
                            [ style "left" (pct <| toLeftValue model.game.hunterPosition.x dotWidth)
                            , style "top" (pct <| toTopValue model.game.hunterPosition.y dotWidth)
                            , style "width" (pct dotWidth)
                            , style "height" (pct dotWidth)
                            , tw "absolute"
                            , class "hunter-indicator"
                            ]
                            []

                        -- HUNTER DECORATION
                        , div
                            [ style "left" (pct <| toLeftValue model.game.hunterPosition.x hunterDecoratorWidth)
                            , style "top" (pct <| toTopValue model.game.hunterPosition.y hunterDecoratorWidth)
                            , style "width" (pct hunterDecoratorWidth)
                            , style "height" (pct hunterDecoratorWidth)
                            , tw "absolute border-2 border-cyan-500"
                            , class "hunter-decorator-blue"
                            ]
                            []

                        -- HUNTER DECORATION
                        , div
                            [ style "left" (pct <| toLeftValue model.game.hunterPosition.x hunterDecoratorWidth)
                            , style "top" (pct <| toTopValue model.game.hunterPosition.y hunterDecoratorWidth)
                            , style "width" (pct hunterDecoratorWidth)
                            , style "height" (pct hunterDecoratorWidth)
                            , tw "absolute border-2 border-red-500"
                            , class "hunter-decorator-red"
                            ]
                            []
                        ]
                    ]
                ]
            , div [ tw "border-white flex flex-col w-72 justify-between" ]
                [ div [ tw "w-full flex items-center flex-col text-center p-4" ]
                    [ img [ src "hunter.png", tw "w-56" ] []
                    , div [ tw "flex flex-col items-center" ]
                        [ div [ tw "font-bold text-xl" ] [ text model.hunterName ]
                        , div [ tw "mb-2" ] [ text (String.fromFloat model.hunterTimeRemaining ++ "ms remaining.") ]
                        , div [ tw "w-48 flex text-sm justify-between" ]
                            [ div [ tw "text-left w-24 mr-8" ] [ text "Location: " ]
                            , div [ tw "flex" ]
                                [ div [ tw "w-6 text-right" ] [ text (String.fromInt model.game.hunterPosition.x) ]
                                , div [] [ text "," ]
                                , div [ tw "w-6 text-right" ] [ text (String.fromInt model.game.hunterPosition.y) ]
                                ]
                            ]
                        , div [ tw "w-48 flex text-sm justify-between" ]
                            [ div [ tw "text-left w-24 mr-8" ] [ text "Velocity:" ]
                            , div [ tw "flex" ]
                                [ div [ tw "w-6 text-right" ] [ text (String.fromInt model.game.hunterVelocity.x) ]
                                , div [] [ text "," ]
                                , div [ tw "w-6 text-right" ] [ text (String.fromInt model.game.hunterVelocity.y) ]
                                ]
                            ]
                        , div [ tw "w-48 flex text-sm justify-between" ]
                            [ div [ tw "w-24 text-left mr-8" ] [ text "Walls: " ]
                            , div [ tw "text-right" ] [ text (String.fromInt <| List.length model.game.walls) ]
                            ]
                        , div [ tw "w-48 flex text-sm justify-between" ]
                            [ div [ tw "w-24 text-left mr-8" ] [ text "Last wall: " ]
                            , div [ tw "text-right " ] [ text (model.game.hunterLastWallTime |> Maybe.map String.fromInt |> Maybe.withDefault "None") ]
                            ]
                        ]
                    ]
                , div [ tw "grow flex justify-center items-center flex-col" ]
                    [ div [ tw "w-full text-center flex items-center justify-center flex-col mb-2" ]
                        [ div [ tw "text-sm", style "font-variant" "small-caps" ] [ text "game ticks" ]
                        , div
                            [ tw "text-4xl font-bold"
                            , twIf (isCaught model.state) "text-red-500"
                            , twIf (isTimeout model.state) "text-green-500"
                            ]
                            [ text (String.fromInt model.game.ticker) ]
                        ]
                    , div [ tw "text-xs text-center", style "font-variant" "small-caps" ] [ text ("N=" ++ String.fromInt model.configuration.nextWallTime ++ " & M=" ++ String.fromInt model.configuration.maximumWalls) ]
                    ]
                , div [ tw "w-full flex items-center flex-col text-center p-4" ]
                    [ img [ src "prey.png", tw "w-56", twIf (isCaught model.state) "hidden" ] []
                    , img [ src "prey-dead.png", tw "w-56", twIf (not <| isCaught model.state) "hidden" ] []
                    , div [ tw "flex flex-col items-center" ]
                        [ div [ tw "font-bold text-xl" ] [ text model.preyName ]
                        , div [ tw "mb-2" ] [ text (String.fromFloat model.preyTimeRemaining ++ "ms remaining.") ]
                        , div [ tw "w-48 flex text-sm justify-between" ]
                            [ div [ tw "text-left w-24 mr-8" ] [ text "Location: " ]
                            , div [ tw "flex" ]
                                [ div [ tw "w-6 text-right" ] [ text (String.fromInt model.game.preyPosition.x) ]
                                , div [] [ text "," ]
                                , div [ tw "w-6 text-right" ] [ text (String.fromInt model.game.preyPosition.y) ]
                                ]
                            ]
                        , div [ tw "w-48 flex text-sm justify-between" ]
                            [ div [ tw "text-left w-24 mr-8" ] [ text "Velocity:" ]
                            , div [ tw "flex" ]
                                [ div [ tw "w-6 text-right" ] [ text (String.fromInt model.game.preyVelocity.x) ]
                                , div [] [ text "," ]
                                , div [ tw "w-6 text-right" ] [ text (String.fromInt model.game.preyVelocity.y) ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        , div [ tw "absolute top-0 left-0 w-screen h-screen flex justify-center items-center pointer-events-none", twIf (not <| awaitingConnection model.state) "hidden" ]
            [ div [ tw "h-screen opacity-50 bg-neutral-700 w-screen absolute top-0 left-0 z-0" ] []
            , div [ tw "w-96 p-8 rounded bg-black z-50 shadow" ]
                [ div [ tw "text-sm uppercase text-center font-bold mb-4" ] [ text "Not Connected to Game Server" ]
                , div [ tw "text-center text-sm" ] [ text "The game server needs to be started before this page is loaded. Please start the game server and then refresh this page." ]
                ]
            ]
        , div [ tw "absolute top-0 left-0 w-screen h-screen flex justify-center items-center pointer-events-none", twIf (not <| awaitingGameServer model.state) "hidden" ]
            [ div [ tw "h-screen opacity-50 bg-neutral-700 w-screen absolute top-0 left-0 z-0" ] []
            , div [ tw "w-96 p-8 rounded bg-black z-50 shadow" ]
                [ div [ tw "text-sm uppercase text-center font-bold mb-4" ] [ text "Connected & Awaiting Game Start" ]
                , div [ tw "text-center text-sm" ] [ text "The observer is connected; waiting for hunter and prey to connect for the game to start. Make sure you connect to the server in the order Observer, Hunter, Prey." ]
                ]
            ]
        , catchFlash model
        ]



---- PROGRAM ----


main : Program String Model Msg
main =
    Browser.element
        { view = view
        , init = init
        , update = update
        , subscriptions = always <| messageReceiver PortMessageReceived
        }
