port module Main exposing (..)

import Browser
import Game exposing (..)
import Html exposing (Html, div, h1, img, text)
import Html.Attributes exposing (src)



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
    , hunterName = ""
    , preyName = ""
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
                        Continues (hunterTimeRemaining, preyTimeRemaining, game) -> 
                            { model | hunterTimeRemaining = hunterTimeRemaining, preyTimeRemaining = preyTimeRemaining, game = game, state = GameLoop }
                            |> withCmdNone
                        PreyIsCaught (hunterTimeRemaining, preyTimeRemaining, game) -> 
                            { model | hunterTimeRemaining = hunterTimeRemaining, preyTimeRemaining = preyTimeRemaining, game = game, state = FinishedCaught}
                            |> withCmdNone
                        PreyTimeout (hunterTimeRemaining, preyTimeRemaining, game) -> 
                            { model | hunterTimeRemaining = hunterTimeRemaining, preyTimeRemaining = preyTimeRemaining, game = game, state = FinishedTimeout}
                            |> withCmdNone
                        End -> 
                            ( model, Cmd.none)

                _ ->
                    ( model, Cmd.none )



---- VIEW ----


view : Model -> Html Msg
view model =
    div []
        [ img [ src "/logo.svg" ] []
        , h1 [] [ text "Your Elm App is working!" ]
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
