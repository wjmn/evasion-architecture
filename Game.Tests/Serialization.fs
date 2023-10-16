module Game.Tests.Serialization

open NUnit.Framework
open Game.Field
open Game.Game
open Game.Serialization

[<Test>]
let TestConfigEncode() = 
    let config = { NextWallInterval = 5<s>; MaxWalls = 3 }
    let expected = "5 3"
    Assert.That(encodeConfig config, Is.EqualTo(expected))

[<Test>]
let TestInitialGameEncode() = 
    let game = newGame { NextWallInterval = 5<s>; MaxWalls = 3 }
    let expected = "0 0 0 1 1 null 230 200 0 0 0"
    Assert.That(encodeGame game, Is.EqualTo(expected))

[<Test>]
let TestGameEncodeWithWalls() = 
    let game = 
        { Config = { NextWallInterval = 5<s>; MaxWalls = 3 }
          Ticker = 30<s>
          HunterPosition = { X = 31<m>; Y = 31<m> }
          HunterVelocity = { X = 1<m / s>; Y = 1<m / s> }
          HunterLastWall = Some(20<s>)
          PreyPosition = { X = 30<m>; Y = 200<m> }
          PreyVelocity = { X = (1<m / s>); Y = 1<m / s> }
          Walls = [ Horizontal(31<m>, 0<m>, 300<m>) ] }
    let expected = "30 31 31 1 1 20 30 200 1 1 1 0 31 300 31"
    Assert.That(encodeGame game, Is.EqualTo(expected))

[<Test>]
let TestInitialGameDecode() = 
    let string = "0 0 0 1 1 null 230 200 0 0 0"
    let expected = newGame { NextWallInterval = 5<s>; MaxWalls = 3 }
    Assert.That(decodeGame 5 3 string, Is.EqualTo(expected))

[<Test>]
let TestGameDecodeWithWalls() = 
    let string = "30 31 31 1 1 20 30 200 1 1 1 0 31 300 31"
    let expected = 
        { Config = { NextWallInterval = 5<s>; MaxWalls = 3 }
          Ticker = 30<s>
          HunterPosition = { X = 31<m>; Y = 31<m> }
          HunterVelocity = { X = 1<m / s>; Y = 1<m / s> }
          HunterLastWall = Some(20<s>)
          PreyPosition = { X = 30<m>; Y = 200<m> }
          PreyVelocity = { X = (1<m / s>); Y = 1<m / s> }
          Walls = [ Horizontal(31<m>, 0<m>, 300<m>) ] }
    Assert.That(decodeGame 5 3 string, Is.EqualTo(expected))

[<Test>]
let TestEncodeHunterActionCreateWall() = 
    let action = CreateWall(Horizontal(31<m>, 0<m>, 300<m>))
    let expected = "create 0 31 300 31"
    Assert.That(encodeHunterAction action, Is.EqualTo(expected))

[<Test>]
let TestDecodeHunterActionCreateHorizontalWall() = 
    let string = "create 0 31 300 31"
    let expected = CreateWall(Horizontal(31<m>, 0<m>, 300<m>))
    Assert.That(decodeHunterAction string, Is.EqualTo(expected))

[<Test>]
let TestDecodeHunterActionCreateVerticalWall() = 
    let string = "create 31 0 31 300"
    let expected = CreateWall(Vertical(31<m>, 0<m>, 300<m>))
    Assert.That(decodeHunterAction string, Is.EqualTo(expected))

[<Test>]
let TestEncodePreyActionChangeVelocity() = 
    let action = ChangeVelocity({ X = 1<m / s>; Y = 1<m / s> })
    let expected = "change 1 1"
    Assert.That(encodePreyAction action, Is.EqualTo(expected))

[<Test>]
let TestDecodePreyActionChangeVelocity() = 
    let string = "change 1 1"
    let expected = ChangeVelocity({ X = 1<m / s>; Y = 1<m / s> })
    Assert.That(decodePreyAction string, Is.EqualTo(expected))

[<Test>]
let TestEncodeHunterActionRemoveWall() = 
    let action = RemoveWall(Horizontal(31<m>, 0<m>, 300<m>))
    let expected= "remove 0 31 300 31"
    Assert.That(encodeHunterAction action, Is.EqualTo(expected))

[<Test>]
let TestDecodeHunterActionRemoveWall() = 
    let string= "remove 0 31 300 31"
    let expected = RemoveWall(Horizontal(31<m>, 0<m>, 300<m>))
    Assert.That(decodeHunterAction string, Is.EqualTo(expected))

[<Test>]
let TestEncodePreyNoAction() = 
    let action = PreyNoAction
    let expected = "none"
    Assert.That(encodePreyAction action, Is.EqualTo(expected))

[<Test>]
let TestDecodePreyNoAction() = 
    let string = "none"
    let expected = PreyNoAction
    Assert.That(decodePreyAction string, Is.EqualTo(expected))

[<Test>]
let TestEncodeHunterNoAction() = 
    let action = HunterNoAction
    let expected = "none"
    Assert.That(encodeHunterAction action, Is.EqualTo(expected))

[<Test>]
let TestDecodeHunterNoAction() = 
    let string = "none"
    let expected = HunterNoAction
    Assert.That(decodeHunterAction string, Is.EqualTo(expected))
