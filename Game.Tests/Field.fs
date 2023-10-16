module Game.Tests.Field

open Game.Field
open NUnit.Framework


[<Test>]
let TestStepBy () =
    let p: Point = { X = 200<m>; Y = 150<m> }
    let v: Velocity = { X = 1<m / s>; Y = 1<m / s> }
    let expected: Point = { X = 201<m>; Y = 151<m> }
    Assert.That(stepBy v p, Is.EqualTo(expected))

[<Test>]
let TestCollidesNoBoundary () =
    let p: Point = { X = 200<m>; Y = 150<m> }
    let expected = false
    Assert.That(collidesBoundary p, Is.EqualTo(expected))

[<Test>]
let TestCollidesVerticalBoundary () =
    let p: Point = { X = 200<m>; Y = 300<m> }
    let expected = true
    Assert.That(collidesBoundary p, Is.EqualTo(expected))

[<Test>]
let TestCollidesHorizontalBoundary () =
    let p: Point = { X = 0<m>; Y = 50<m> }
    let expected = true
    Assert.That(collidesBoundary p, Is.EqualTo(expected))


[<Test>]
let TestNoBounce () =
    let p: Point = { X = 10<m>; Y = 10<m> }
    let v: Velocity = { X = 1<m / s>; Y = 1<m / s> }

    let walls =
        [ Horizontal(50<m>, 0<m>, 300<m>)
          Vertical(50<m>, 20<m>, 60<m>)
          Vertical(60<m>, 0<m>, 300<m>) ]

    let pExpect: Point = { X = 11<m>; Y = 11<m> }
    let expected = (pExpect, v)
    Assert.That(stepAndBounce v walls p, Is.EqualTo(expected))

[<Test>]
let TestBounceVerticalWall () =
    let p: Point = { X = 10<m>; Y = 10<m> }
    let v: Velocity = { X = 1<m / s>; Y = 1<m / s> }

    let walls =
        [ Horizontal(50<m>, 0<m>, 300<m>)
          Vertical(11<m>, 0<m>, 300<m>)
          Vertical(60<m>, 0<m>, 300<m>) ]

    let pExpect: Point = { X = 10<m>; Y = 11<m> }
    let vExpect: Velocity = { X = (-1<m / s>); Y = 1<m / s> }
    let expected = (pExpect, vExpect)
    Assert.That(stepAndBounce v walls p, Is.EqualTo(expected))

[<Test>]
let TestBounceHorizontalWall () =
    let p: Point = { X = 10<m>; Y = 10<m> }
    let v: Velocity = { X = 1<m / s>; Y = 1<m / s> }

    let walls =
        [ Horizontal(11<m>, 0<m>, 300<m>)
          Vertical(40<m>, 0<m>, 300<m>)
          Vertical(60<m>, 0<m>, 300<m>) ]

    let pExpect: Point = { X = 11<m>; Y = 10<m> }
    let vExpect: Velocity = { X = 1<m / s>; Y = (-1<m / s>) }
    let expected = (pExpect, vExpect)
    Assert.That(stepAndBounce v walls p, Is.EqualTo(expected))

[<Test>]
let TestBounceBoundaryCorner () =
    let p: Point = { X = 299<m>; Y = 299<m> }
    let v: Velocity = { X = 1<m / s>; Y = 1<m / s> }

    let walls =
        [ Horizontal(11<m>, 0<m>, 300<m>)
          Vertical(40<m>, 0<m>, 300<m>)
          Vertical(60<m>, 0<m>, 300<m>) ]

    let pExpect: Point = { X = 299<m>; Y = 299<m> }
    let vExpect: Velocity = { X = (-1<m / s>); Y = (-1<m / s>) }
    let expected = (pExpect, vExpect)
    Assert.That(stepAndBounce v walls p, Is.EqualTo(expected))

[<Test>]
let TestBounceSinglePoint () =
    let p: Point = { X = 59<m>; Y = 49<m> }
    let v: Velocity = { X = 1<m / s>; Y = 1<m / s> }
    let walls = [ Horizontal(50<m>, 60<m>, 60<m>); Vertical(40<m>, 0<m>, 300<m>) ]
    let pExpect: Point = { X = 59<m>; Y = 49<m> }
    let vExpect: Velocity = { X = (-1<m / s>); Y = (-1<m / s>) }
    let expected = (pExpect, vExpect)
    Assert.That(stepAndBounce v walls p, Is.EqualTo(expected))

[<Test>]
let TestBounceEdgeCaseHorizontalPartial () =
    let p: Point = { X = 20<m>; Y = 20<m> }
    let v: Velocity = { X = 1<m / s>; Y = 1<m / s> }
    let walls = [ Horizontal(21<m>, 0<m>, 21<m>) ]
    let pExpect: Point = { X = 21<m>; Y = 20<m> }
    let vExpect: Velocity = { X = (1<m / s>); Y = (-1<m / s>) }
    let expected = (pExpect, vExpect)
    Assert.That(stepAndBounce v walls p, Is.EqualTo(expected))

[<Test>]
let TestBounceEdgeCaseParallelHorizontals () =
    let p: Point = { X = 41<m>; Y = 41<m> }
    let v: Velocity = { X = -1<m / s>; Y = -1<m / s> }
    let walls = [ Horizontal(40<m>, 0<m>, 40<m>); Horizontal(41<m>, 0<m>, 40<m>) ]
    let pExpect: Point = { X = 41<m>; Y = 40<m> }
    let vExpect: Velocity = { X = (1<m / s>); Y = (-1<m / s>) }
    let expected = (pExpect, vExpect)
    Assert.That(stepAndBounce v walls p, Is.EqualTo(expected))

[<Test>]
let TestBounceExampleHorizontal () =
    let p: Point = { X = 100<m>; Y = 199<m> }
    let v: Velocity = { X = 1<m / s>; Y = 1<m / s> }
    let walls = [ Horizontal(200<m>, 10<m>, 300<m>) ]
    let pExpect: Point = { X = 101<m>; Y = 199<m> }
    let vExpect: Velocity = { X = (1<m / s>); Y = (-1<m / s>) }
    let expected = (pExpect, vExpect)
    Assert.That(stepAndBounce v walls p, Is.EqualTo(expected))

[<Test>]
let TestBounceExampleAvoidsNarrow () =
    let p: Point = { X = 9<m>; Y = 200<m> }
    let v: Velocity = { X = 1<m / s>; Y = 1<m / s> }
    let walls = [ Horizontal(200<m>, 10<m>, 300<m>) ]
    let pExpect: Point = { X = 10<m>; Y = 201<m> }
    let vExpect: Velocity = { X = (1<m / s>); Y = (1<m / s>) }
    let expected = (pExpect, vExpect)
    Assert.That(stepAndBounce v walls p, Is.EqualTo(expected))

[<Test>]
let TestBounceExampleCorner () =
    let p: Point = { X = 9<m>; Y = 199<m> }
    let v: Velocity = { X = 1<m / s>; Y = 1<m / s> }
    let walls = [ Horizontal(200<m>, 10<m>, 300<m>) ]
    let pExpect: Point = { X = 10<m>; Y = 199<m> }
    let vExpect: Velocity = { X = (1<m / s>); Y = (-1<m / s>) }
    let expected = (pExpect, vExpect)
    Assert.That(stepAndBounce v walls p, Is.EqualTo(expected))

[<Test>]
let TestNewWallNoCollision () =
    let newWall = Horizontal(50<m>, 20<m>, 80<m>)

    let walls =
        [ Horizontal(50<m>, 90<m>, 300<m>)
          Vertical(10<m>, 0<m>, 300<m>)
          Horizontal(80<m>, 0<m>, 300<m>) ]

    let expected = false
    Assert.That(wallCollidesWalls newWall walls, Is.EqualTo(expected))

[<Test>]
let TestNewWallOverlapCollisionHorizontal () =
    let newWall = Horizontal(50<m>, 20<m>, 80<m>)

    let walls =
        [ Horizontal(50<m>, 70<m>, 300<m>)
          Vertical(10<m>, 0<m>, 300<m>)
          Horizontal(80<m>, 0<m>, 300<m>) ]

    let expected = true
    Assert.That(wallCollidesWalls newWall walls, Is.EqualTo(expected))

[<Test>]
let TestNewWallIntersectCollisionHorizontal () =
    let newWall = Horizontal(50<m>, 20<m>, 80<m>)

    let walls =
        [ Horizontal(50<m>, 100<m>, 300<m>)
          Vertical(50<m>, 0<m>, 300<m>)
          Horizontal(80<m>, 0<m>, 300<m>) ]

    let expected = true
    Assert.That(wallCollidesWalls newWall walls, Is.EqualTo(expected))

[<Test>]
let TestNewWallOverlapCollisionVertical () =
    let newWall = Vertical(50<m>, 20<m>, 80<m>)

    let walls =
        [ Vertical(50<m>, 70<m>, 300<m>)
          Horizontal(10<m>, 0<m>, 300<m>)
          Vertical(80<m>, 0<m>, 300<m>) ]

    let expected = true
    Assert.That(wallCollidesWalls newWall walls, Is.EqualTo(expected))

[<Test>]
let TestNewWallIntersectCollisionVertical () =
    let newWall = Vertical(50<m>, 20<m>, 80<m>)

    let walls =
        [ Vertical(50<m>, 100<m>, 300<m>)
          Horizontal(50<m>, 0<m>, 300<m>)
          Vertical(80<m>, 0<m>, 300<m>) ]

    let expected = true
    Assert.That(wallCollidesWalls newWall walls, Is.EqualTo(expected))

[<Test>]
let TestWallCollidesPoint () =
    let p: Point = { X = 200<m>; Y = 150<m> }
    let wall = Horizontal(150<m>, 100<m>, 300<m>)
    let expected = true
    Assert.That(wallCollidesPoint wall p, Is.EqualTo(expected))
