port module Main exposing (..)

import Browser
import Game exposing (..)
import Html exposing (..)
import Html.Attributes exposing (..)



---- PORTS ----


port messageReceiver : (String -> msg) -> Sub msg



---- MODEL ----


type State
    = AwaitingHunterName
    | AwaitingPreyName
    | AwaitingInitialConfigAndGame
    | GameLoop
    | FinishedCaught
    | FinishedTimeout


type alias Model =
    { state : State
    , hunterName : String
    , preyName : String
    , configuration : Config
    , game : Game
    , hunterTimeRemaining : Float
    , preyTimeRemaining : Float
    }


default : Model
default =
    { state = AwaitingHunterName
    , hunterName = "HUNTER"
    , preyName = "PREY"
    , configuration = { nextWallTime = 0, maximumWalls = 0 }
    , game = dummyGame
    , hunterTimeRemaining = 0
    , preyTimeRemaining = 0
    }


init : ( Model, Cmd Msg )
init =
    ( default, Cmd.none )



---- UPDATE ----


type Msg
    = NoOp
    | PortMessageReceived String


withCmdNone : Model -> ( Model, Cmd Msg )
withCmdNone model =
    ( model, Cmd.none )


update : Msg -> Model -> ( Model, Cmd Msg )
update msg model =
    case msg of
        NoOp ->
            ( model, Cmd.none )

        PortMessageReceived message ->
            case model.state of
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
                            { model | hunterTimeRemaining = hunterTimeRemaining, preyTimeRemaining = preyTimeRemaining, game = game, state = FinishedCaught }
                                |> withCmdNone

                        PreyTimeout ( hunterTimeRemaining, preyTimeRemaining, game ) ->
                            { model | hunterTimeRemaining = hunterTimeRemaining, preyTimeRemaining = preyTimeRemaining, game = game, state = FinishedTimeout }
                                |> withCmdNone

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
            , style "height" (pct <| toTopValue wall.y2 wallWidth - toTopValue wall.y1 wallWidth)
            , style "width" (pct wallWidth)
            , tw "absolute wall border-l"
            ]
            []

    else
        div
            [ style "left" (pct <| toLeftValue wall.x1 wallWidth)
            , style "top" (pct <| toTopValue wall.y1 wallWidth)
            , style "width" (pct <| toLeftValue wall.x2 wallWidth - toLeftValue wall.x1 wallWidth)
            , style "height" (pct wallWidth)
            , tw "absolute wall border-t"
            ]
            []


isCaught : State -> Bool
isCaught state = 
    case state of 
        FinishedCaught -> True
        _ -> False

isTimeout : State -> Bool 
isTimeout state = 
    case state of 
        FinishedTimeout  -> True
        _ -> False

---- VIEW ----


view : Model -> Html Msg
view model =
    div [ tw "bg-black h-screen flex justify-center items-center text-white p-4" ]
        [ div [ tw "flex justify-center" ]
            [ div [ tw "h-full flex justify-center items-center mr-8" ]
                [ div [ tw "border-white border-2 p-1" ]
                    [ div [ style "height" "90vh", style "width" "90vh", tw "relative overflow-hidden" ]
                        [ --- WALS
                          div [] (List.map viewWall model.game.walls)

                        -- PREY INDICATOR
                        , div
                            [ style "left" (pct <| toLeftValue model.game.preyPosition.x dotWidth)
                            , style "top" (pct <| toTopValue model.game.preyPosition.y dotWidth)
                            , style "width" (pct dotWidth)
                            , style "height" (pct dotWidth)
                            , tw "absolute bg-white rounded-full"
                            , class "prey-indicator"
                            ]
                            []
                        , div
                            [ style "left" (pct <| toLeftValue model.game.preyPosition.x preyDecorationWidth)
                            , style "top" (pct <| toTopValue model.game.preyPosition.y preyDecorationWidth)
                            , style "width" (pct preyDecorationWidth)
                            , style "height" (pct preyDecorationWidth)
                            , tw "absolute bg-white rounded-full"
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
                            , tw "absolute border-2 border-red-500"
                            , class "hunter-decorator-red"
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
                        ]
                    ]
                ]
            , div [ tw "border-white flex flex-col w-96 justify-between" ]
                [ div [ tw "w-full border-2 border-white flex items-center flex-col text-center p-4" ]
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
                        , div [ tw "text-4xl font-bold" , twIf (isCaught model.state) "text-red-500", twIf (isTimeout model.state) "text-green-500"
                        ] [ text (String.fromInt model.game.ticker) ]
                        ]
                    , div [ tw "text-xs text-center", style "font-variant" "small-caps" ] [ text ("N=" ++ String.fromInt model.configuration.nextWallTime ++ " & M=" ++ String.fromInt model.configuration.maximumWalls) ]
                    ]
                , div [ tw "w-full border-2 border-white flex items-center flex-col text-center p-4" ]
                    [ img [ src "prey.png", tw "w-56" ] []
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
        ]



---- PROGRAM ----


main : Program () Model Msg
main =
    Browser.element
        { view = view
        , init = \_ -> init
        , update = update
        , subscriptions = always <| messageReceiver PortMessageReceived
        }
