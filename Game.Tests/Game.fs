module Game.Tests.Game

open Game.Field
open Game.Game
open NUnit.Framework


[<Test>]
let TestStepGameStartNoAction () =
    let gameStart =
        newGame
            { NextWallInterval = 5<s>
              MaxWalls = 3 }

    let expected =
        { gameStart with
            Ticker = 1<s>
            HunterPosition = { X = 1<m>; Y = 1<m> } }

    Assert.That(step gameStart HunterNoAction PreyNoAction, Is.EqualTo(expected))

[<Test>]
let TestHunterBounceOffCorner() =
    let game =
        { Config =
            { NextWallInterval = 10<s>
              MaxWalls = 5 }
          Ticker = 299<s>
          HunterPosition = { X = 299<m>; Y = 299<m> }
          HunterVelocity = { X = 1<m / s>; Y = 1<m / s> }
          HunterLastWall = None
          PreyPosition = { X = 230<m>; Y = 200<m> }
          PreyVelocity = { X = 0<m / s>; Y = 0<m / s> }
          Walls = [] }

    let expected =
        { game with
            Ticker = 300<s>
            HunterVelocity = { X = -1<m/s>; Y = -1<m/s> } }

    Assert.That(step game HunterNoAction PreyNoAction, Is.EqualTo(expected))

[<Test>]
let TestStepGameMiddleCreateWall () =
    let gameMiddle =
        { Config =
            { NextWallInterval = 5<s>
              MaxWalls = 3 }
          Ticker = 30<s>
          HunterPosition = { X = 31<m>; Y = 31<m> }
          HunterVelocity = { X = 1<m / s>; Y = 1<m / s> }
          HunterLastWall = None
          PreyPosition = { X = 230<m>; Y = 200<m> }
          PreyVelocity = { X = 0<m / s>; Y = 0<m / s> }
          Walls = [] }

    let expected =
        { gameMiddle with
            Ticker = 31<s>
            HunterPosition = { X = 32<m>; Y = 32<m> }
            HunterLastWall = Some(30<s>)
            Walls = [ Horizontal(31<m>, 0<m>, 299<m>) ] }

    Assert.That(step gameMiddle (CreateWall(Horizontal(31<m>, 0<m>, 299<m>))) PreyNoAction, Is.EqualTo(expected))

[<Test>]
let TestStepGameWallNoGapValid () =
    let gameMiddle =
        { Config =
            { NextWallInterval = 5<s>
              MaxWalls = 3 }
          Ticker = 30<s>
          HunterPosition = { X = 31<m>; Y = 31<m> }
          HunterVelocity = { X = 1<m / s>; Y = 1<m / s> }
          HunterLastWall = Some(0<s>)
          PreyPosition = { X = 230<m>; Y = 200<m> }
          PreyVelocity = { X = 0<m / s>; Y = 0<m / s> }
          Walls = [Horizontal(50<m>, 0<m>, 299<m>)] }

    let expected =
        { gameMiddle with
            Ticker = 31<s>
            HunterPosition = { X = 32<m>; Y = 32<m> }
            HunterLastWall = Some(30<s>)
            Walls = [ Vertical(31<m>, 0<m>, 49<m>); Horizontal(50<m>, 0<m>, 299<m>) ] }

    Assert.That(step gameMiddle (CreateWall(Vertical(31<m>, 0<m>, 49<m>))) PreyNoAction, Is.EqualTo(expected))

[<Test>]
let TestStepGameWallNoGapInvalidBy1Off () =
    let gameMiddle =
        { Config =
            { NextWallInterval = 5<s>
              MaxWalls = 3 }
          Ticker = 30<s>
          HunterPosition = { X = 31<m>; Y = 31<m> }
          HunterVelocity = { X = 1<m / s>; Y = 1<m / s> }
          HunterLastWall = Some(0<s>)
          PreyPosition = { X = 230<m>; Y = 200<m> }
          PreyVelocity = { X = 0<m / s>; Y = 0<m / s> }
          Walls = [Horizontal(50<m>, 0<m>, 299<m>)] }

    let expected =
        { gameMiddle with
            Ticker = 31<s>
            HunterPosition = { X = 32<m>; Y = 32<m> }
            HunterLastWall = Some(0<s>)
            Walls = [ Horizontal(50<m>, 0<m>, 299<m>)] }

    Assert.That(step gameMiddle (CreateWall(Vertical(31<m>, 0<m>, 50<m>))) PreyNoAction, Is.EqualTo(expected))

[<Test>]
let TestStepGameWallNoGapInvalidByNoTouchHunter () =
    let gameMiddle =
        { Config =
            { NextWallInterval = 5<s>
              MaxWalls = 3 }
          Ticker = 30<s>
          HunterPosition = { X = 31<m>; Y = 31<m> }
          HunterVelocity = { X = 1<m / s>; Y = 1<m / s> }
          HunterLastWall = Some(0<s>)
          PreyPosition = { X = 230<m>; Y = 200<m> }
          PreyVelocity = { X = 0<m / s>; Y = 0<m / s> }
          Walls = [Horizontal(50<m>, 0<m>, 299<m>)] }

    let expected =
        { gameMiddle with
            Ticker = 31<s>
            HunterPosition = { X = 32<m>; Y = 32<m> }
            HunterLastWall = Some(0<s>)
            Walls = [ Horizontal(50<m>, 0<m>, 299<m>)] }

    Assert.That(step gameMiddle (CreateWall(Vertical(31<m>, 51<m>, 299<m>))) PreyNoAction, Is.EqualTo(expected))




[<Test>]
let TestStepGameMiddlePreyBounceNewWall () =
    let gameMiddle =
        { Config =
            { NextWallInterval = 5<s>
              MaxWalls = 3 }
          Ticker = 31<s>
          HunterPosition = { X = 31<m>; Y = 31<m> }
          HunterVelocity = { X = 1<m / s>; Y = 1<m / s> }
          HunterLastWall = None
          PreyPosition = { X = 30<m>; Y = 200<m> }
          PreyVelocity = { X = (1<m / s>); Y = 1<m / s> }
          Walls = [] }

    let expected =
        { gameMiddle with
            Ticker = 32<s>
            HunterPosition = { X = 32<m>; Y = 32<m> }
            HunterLastWall = Some(31<s>)
            PreyPosition = { X = 30<m>; Y = 201<m> }
            PreyVelocity = { X = (-1<m / s>); Y = 1<m / s> }
            Walls = [ Vertical(31<m>, 0<m>, 299<m>) ] }

    Assert.That(step gameMiddle (CreateWall(Vertical(31<m>, 0<m>, 299<m>))) PreyNoAction, Is.EqualTo(expected))
    
[<Test>]
let TestStepGameMiddleInvalidWallByTouchPrey () =
    let gameMiddle =
        { Config =
            { NextWallInterval = 5<s>
              MaxWalls = 3 }
          Ticker = 30<s>
          HunterPosition = { X = 31<m>; Y = 31<m> }
          HunterVelocity = { X = 1<m / s>; Y = 1<m / s> }
          HunterLastWall = None
          PreyPosition = { X = 31<m>; Y = 200<m> }
          PreyVelocity = { X = (0<m / s>); Y = 0<m / s> }
          Walls = [] }

    let expected =
        { gameMiddle with
            Ticker = 31<s>
            HunterPosition = { X = 32<m>; Y = 32<m> }
        }

    Assert.That(step gameMiddle (CreateWall(Vertical(31<m>, 0<m>, 299<m>))) PreyNoAction, Is.EqualTo(expected))

[<Test>]
let TestStepGameMiddleInvalidWallByMaxWalls () =
    let gameMiddle =
        { Config =
            { NextWallInterval = 5<s>
              MaxWalls = 3 }
          Ticker = 30<s>
          HunterPosition = { X = 31<m>; Y = 31<m> }
          HunterVelocity = { X = 1<m / s>; Y = 1<m / s> }
          HunterLastWall = Some(20<s>)
          PreyPosition = { X = 30<m>; Y = 200<m> }
          PreyVelocity = { X = (0<m / s>); Y = 0<m / s> }
          Walls = [Vertical(1<m>, 0<m>, 299<m>); Vertical(2<m>, 0<m>, 299<m>); Vertical(3<m>, 0<m>, 299<m>)] }

    let expected =
        { gameMiddle with
            Ticker = 31<s>
            HunterPosition = { X = 32<m>; Y = 32<m> }
        }

    Assert.That(step gameMiddle (CreateWall(Vertical(31<m>, 0<m>, 299<m>))) PreyNoAction, Is.EqualTo(expected))

[<Test>]
let TestStepGameMiddleInvalidWallByWallInterval () =
    let gameMiddle =
        { Config =
            { NextWallInterval = 5<s>
              MaxWalls = 3 }
          Ticker = 30<s>
          HunterPosition = { X = 31<m>; Y = 31<m> }
          HunterVelocity = { X = 1<m / s>; Y = 1<m / s> }
          HunterLastWall = Some(28<s>)
          PreyPosition = { X = 30<m>; Y = 200<m> }
          PreyVelocity = { X = (0<m / s>); Y = 0<m / s> }
          Walls = [Vertical(1<m>, 0<m>, 299<m>); Vertical(2<m>, 0<m>, 299<m>)]}

    let expected =
        { gameMiddle with
            Ticker = 31<s>
            HunterPosition = { X = 32<m>; Y = 32<m> }
        }

    Assert.That(step gameMiddle (CreateWall(Vertical(31<m>, 0<m>, 299<m>))) PreyNoAction, Is.EqualTo(expected))

[<Test>]
let TestStepGameMiddleInvalidWallByCollidesWall () =
    let gameMiddle =
        { Config =
            { NextWallInterval = 5<s>
              MaxWalls = 3 }
          Ticker = 30<s>
          HunterPosition = { X = 31<m>; Y = 31<m> }
          HunterVelocity = { X = 1<m / s>; Y = 1<m / s> }
          HunterLastWall = Some(20<s>)
          PreyPosition = { X = 30<m>; Y = 200<m> }
          PreyVelocity = { X = (0<m / s>); Y = 0<m / s> }
          Walls = [Vertical(1<m>, 0<m>, 299<m>); Horizontal(2<m>, 0<m>, 299<m>)]}

    let expected =
        { gameMiddle with
            Ticker = 31<s>
            HunterPosition = { X = 32<m>; Y = 32<m> }
        }

    Assert.That(step gameMiddle (CreateWall(Vertical(31<m>, 0<m>, 299<m>))) PreyNoAction, Is.EqualTo(expected))

[<Test>]
let TestStepGameMiddleInvalidWallByCollidesBounds() = 
    let gameMiddle =
        { Config =
            { NextWallInterval = 5<s>
              MaxWalls = 3 }
          Ticker = 30<s>
          HunterPosition = { X = 31<m>; Y = 31<m> }
          HunterVelocity = { X = 1<m / s>; Y = 1<m / s> }
          HunterLastWall = Some(20<s>)
          PreyPosition = { X = 30<m>; Y = 200<m> }
          PreyVelocity = { X = (0<m / s>); Y = 0<m / s> }
          Walls = []}

    let expected =
        { gameMiddle with
            Ticker = 31<s>
            HunterPosition = { X = 32<m>; Y = 32<m> }
        }

    Assert.That(step gameMiddle (CreateWall(Vertical(31<m>, 0<m>, 300<m>))) PreyNoAction, Is.EqualTo(expected))

[<Test>]
let TestStepGameMiddleInvalidWallByNotTouchHunter () =
    let gameMiddle =
        { Config =
            { NextWallInterval = 5<s>
              MaxWalls = 3 }
          Ticker = 30<s>
          HunterPosition = { X = 31<m>; Y = 31<m> }
          HunterVelocity = { X = 1<m / s>; Y = 1<m / s> }
          HunterLastWall = Some(20<s>)
          PreyPosition = { X = 30<m>; Y = 200<m> }
          PreyVelocity = { X = (0<m / s>); Y = 0<m / s> }
          Walls = [Vertical(1<m>, 0<m>, 299<m>); Vertical(2<m>, 0<m>, 299<m>)]}

    let expected =
        { gameMiddle with
            Ticker = 31<s>
            HunterPosition = { X = 32<m>; Y = 32<m> }
        }

    Assert.That(step gameMiddle (CreateWall(Vertical(29<m>, 0<m>, 299<m>))) PreyNoAction, Is.EqualTo(expected))

[<Test>]
let TestStepGameMiddleRemoveWall () =
    let gameMiddle =
        { Config =
            { NextWallInterval = 5<s>
              MaxWalls = 3 }
          Ticker = 30<s>
          HunterPosition = { X = 31<m>; Y = 31<m> }
          HunterVelocity = { X = 1<m / s>; Y = 1<m / s> }
          HunterLastWall = Some(20<s>)
          PreyPosition = { X = 30<m>; Y = 200<m> }
          PreyVelocity = { X = (0<m / s>); Y = 0<m / s> }
          Walls = [Vertical(1<m>, 0<m>, 299<m>); Vertical(2<m>, 0<m>, 299<m>)]}

    let expected =
        { gameMiddle with
            Ticker = 31<s>
            HunterPosition = { X = 32<m>; Y = 32<m> }
            Walls=[Vertical(2<m>, 0<m>, 299<m>)]
        }

    Assert.That(step gameMiddle (RemoveWalls[(Vertical(1<m>, 0<m>, 299<m>))]) PreyNoAction, Is.EqualTo(expected))

[<Test>]
let TestStepGameMiddleRemoveMultipleWallsAndCreate () =
    let gameMiddle =
        { Config =
            { NextWallInterval = 5<s>
              MaxWalls = 3 }
          Ticker = 30<s>
          HunterPosition = { X = 31<m>; Y = 31<m> }
          HunterVelocity = { X = 1<m / s>; Y = 1<m / s> }
          HunterLastWall = Some(20<s>)
          PreyPosition = { X = 30<m>; Y = 200<m> }
          PreyVelocity = { X = (0<m / s>); Y = 0<m / s> }
          Walls = [Vertical(1<m>, 0<m>, 299<m>); Vertical(2<m>, 0<m>, 299<m>)]}

    let expected =
        { gameMiddle with
            Ticker = 31<s>
            HunterLastWall = Some(30<s>)
            HunterPosition = { X = 32<m>; Y = 32<m> }
            Walls=[Horizontal(31<m>, 0<m>, 299<m>)]
        }

    Assert.That(step gameMiddle (RemoveAndCreate([Vertical(1<m>, 0<m>, 299<m>); Vertical(2<m>, 0<m>, 299<m>)], Horizontal(31<m>, 0<m>, 299<m>))) PreyNoAction, Is.EqualTo(expected))

[<Test>]
let TestStepGameMiddleRemoveMultipleWallsAndCreateInvalid () =
    let gameMiddle =
        { Config =
            { NextWallInterval = 5<s>
              MaxWalls = 3 }
          Ticker = 30<s>
          HunterPosition = { X = 31<m>; Y = 31<m> }
          HunterVelocity = { X = 1<m / s>; Y = 1<m / s> }
          HunterLastWall = Some(20<s>)
          PreyPosition = { X = 30<m>; Y = 200<m> }
          PreyVelocity = { X = (0<m / s>); Y = 0<m / s> }
          Walls = [Vertical(1<m>, 0<m>, 299<m>); Vertical(2<m>, 0<m>, 299<m>)]}

    let expected =
        { gameMiddle with
            Ticker = 31<s>
            HunterPosition = { X = 32<m>; Y = 32<m> }
            Walls=[]
        }

    Assert.That(step gameMiddle (RemoveAndCreate([Vertical(1<m>, 0<m>, 299<m>); Vertical(2<m>, 0<m>, 299<m>)], Horizontal(30<m>, 0<m>, 299<m>))) PreyNoAction, Is.EqualTo(expected))



[<Test>]
let TestStepGameMiddleRemoveMultipleWalls () =
    let gameMiddle =
        { Config =
            { NextWallInterval = 5<s>
              MaxWalls = 3 }
          Ticker = 30<s>
          HunterPosition = { X = 31<m>; Y = 31<m> }
          HunterVelocity = { X = 1<m / s>; Y = 1<m / s> }
          HunterLastWall = Some(20<s>)
          PreyPosition = { X = 30<m>; Y = 200<m> }
          PreyVelocity = { X = (0<m / s>); Y = 0<m / s> }
          Walls = [Vertical(1<m>, 0<m>, 299<m>); Vertical(2<m>, 0<m>, 299<m>)]}

    let expected =
        { gameMiddle with
            Ticker = 31<s>
            HunterPosition = { X = 32<m>; Y = 32<m> }
            Walls=[]
        }

    Assert.That(step gameMiddle (RemoveWalls[Vertical(1<m>, 0<m>, 299<m>); Vertical(2<m>, 0<m>, 299<m>)]) PreyNoAction, Is.EqualTo(expected))



[<Test>]
let TestStepGameWithinDistanceWithoutWallBetweenHunterAndPrey() =
    let game =
        { Config =
            { NextWallInterval = 5<s>
              MaxWalls = 3 }
          Ticker = 31<s>
          HunterPosition = { X = 31<m>; Y = 31<m> }
          HunterVelocity = { X = 1<m / s>; Y = 1<m / s> }
          HunterLastWall = Some(20<s>)
          PreyPosition = { X = 35<m>; Y = 35<m> }
          PreyVelocity = { X = (-1<m / s>); Y = -1<m / s> }
          Walls = []}

    let expected =
        PreyIsCaught
            { game with
                Ticker = 32<s>
                HunterPosition = { X = 32<m>; Y = 32<m> }
                PreyPosition = { X = 34<m>; Y = 34<m> }
            }

    Assert.That(stepOutcome game HunterNoAction PreyNoAction, Is.EqualTo(expected))


[<Test>]
let TestStepGameWithinDistanceWithWallBetweenHunterAndPrey() =
    let game =
        { Config =
            { NextWallInterval = 5<s>
              MaxWalls = 3 }
          Ticker = 31<s>
          HunterPosition = { X = 31<m>; Y = 31<m> }
          HunterVelocity = { X = 1<m / s>; Y = 1<m / s> }
          HunterLastWall = Some(20<s>)
          PreyPosition = { X = 35<m>; Y = 35<m> }
          PreyVelocity = { X = (-1<m / s>); Y = -1<m / s> }
          Walls = [Horizontal(33<m>, 0<m>, 299<m>);]}

    let expected =
        Continues
            { game with
                Ticker = 32<s>
                HunterPosition = { X = 32<m>; Y = 32<m> }
                PreyPosition = { X = 34<m>; Y = 34<m> }
            }

    Assert.That(stepOutcome game HunterNoAction PreyNoAction, Is.EqualTo(expected))


