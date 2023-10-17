module Game.Field

//------------------------------------------------------------------------------
// TYPES
//------------------------------------------------------------------------------

/// The unit of distance in a field (think of this as "metres").
[<Measure>]
type m

/// A single point in a field.
[<Struct>]
type Point = { X: int<m>; Y: int<m> }

/// A wall in a field which causes bouncing when struck.
/// As per the game description, walls must not go through other walls,
/// though they may touch other walls. Therfore, walls, must be specified with
/// a start and end point. As discussed in class, we will only use horizontal
/// and vertical walls (no diagonals). The start and end points are inclusive.
type Wall =
    | Horizontal of Y: int<m> * X1: int<m> * X2: int<m>
    | Vertical of X: int<m> * Y1: int<m> * Y2: int<m>

/// The unit of measure for time (think of this as "seconds").
[<Measure>]
type s

/// A representation of velocity in m/s.
[<Struct>]
type Velocity = { X: int<m / s>; Y: int<m / s> }

//------------------------------------------------------------------------------
// CONSTANTS
//------------------------------------------------------------------------------

/// Maximum width of field.
[<Literal>]
let MaxWidth = 300<m>

/// Maximum height of field.
[<Literal>]
let MaxHeight = 300<m>

//------------------------------------------------------------------------------
// CONVERSION FUNCTIONS
//------------------------------------------------------------------------------

/// Convert an integer to metres.
let m (x: int) = x * 1<m>

/// Convert an integer to seconds.
let s (x: int) = x * 1<s>

//------------------------------------------------------------------------------
// UTILITY FUNCTIONS
//------------------------------------------------------------------------------

/// Take the signum of both elements of velocity. 
let signum (v: Velocity) : Velocity = 
    { X = if v.X > 0<m/s> then 1<m/s> elif v.X < 0<m/s> then -1<m/s> else 0<m/s>
      Y = if v.Y > 0<m/s> then 1<m/s> elif v.Y < 0<m/s> then -1<m/s> else 0<m/s>}

/// Euclidean distance between two points. 
let distance (p1: Point) (p2: Point): float = 
    sqrt ((float p1.X - float p2.X) ** 2.0 + (float p1.Y - float p2.Y) ** 2.0)

/// Next position of a point p after 1 unit of time with velocity v.
let stepBy (v: Velocity) (p: Point) : Point =
    { X = p.X + v.X * 1<s>
      Y = p.Y + v.Y * 1<s> }

/// Checks if a projected position collides with a boundary.
/// Note: p.X and p.Y will never exceed the boundaries, so it is
/// sufficient to perform an equalty check rather than full comparison.
let collidesBoundary (p: Point) : bool =
    p.X = 0<m> || p.X = MaxWidth || p.Y = 0<m> || p.Y = MaxHeight

/// Checks if a wall collides with a point. 
let wallCollidesPoint (wall: Wall) (p: Point) : bool =
    match wall with
    | Horizontal(y, x1, x2) ->
        p.Y = y && p.X >= x1 && p.X <= x2
    | Vertical(x, y1, y2) ->
        p.X = x && p.Y >= y1 && p.Y <= y2

let wallsOrBoundsCollidePoint (walls: Wall list) (p: Point) : bool = 
    collidesBoundary p || List.exists (fun w -> wallCollidesPoint w p) walls

/// Checks the bounce direction of a point which moves a single step by velocity. 
/// Not the most efficient...
let stepAndBounce (v: Velocity) (walls: Wall list) (p: Point) : (Point * Velocity) = 
    // First, check if it collides with projected position
    let projected = stepBy v p 
    let projectedCollides = wallsOrBoundsCollidePoint walls projected
    if projectedCollides then 
        // Check if the velocity is exactly horizontal or exactly vertical, in which case it just bounces directly back with opposite velocity
        if v.X = 0<m/s> then
            (p, { v with Y = -v.Y })
        elif v.Y = 0<m/s> then
            (p, { v with X = -v.X })
        else
            // Check the two points adjacent to the projected point on the velocity's side
            let vAdjacent = { projected with Y = projected.Y - v.Y * 1<s>}
            let hAdjacent = { projected with X = projected.X - v.X * 1<s>}
            let vAdjCollides = wallsOrBoundsCollidePoint walls vAdjacent
            let hAdjCollides = wallsOrBoundsCollidePoint walls hAdjacent
            // If both adjacent points collide, then the projected point is a corner and the velocity is flipped in both directions with same position
            if vAdjCollides && hAdjCollides then 
                (p, { v with X = -v.X; Y = -v.Y })
            // If just v collides, then the point moves vertically with flipped x velocity
            elif vAdjCollides then 
                ({p with Y = projected.Y}, { v with X = -v.X })
            // If just h collides, then the point moves horizontally with flipped y velocity
            elif hAdjCollides then
                ({p with X = projected.X}, { v with Y = -v.Y })
            // If neither collides, then need to check the points beyond
            else
                let vExtended = { projected with Y = projected.Y + v.Y * 1<s>}
                let hExtended = { projected with X = projected.X + v.X * 1<s>}
                let vExtCollides = wallsOrBoundsCollidePoint walls vExtended
                let hExtCollides = wallsOrBoundsCollidePoint walls hExtended
                // If both extended points collide, then the projected point is a corner and the velocity is flipped in both directions with same position
                if vExtCollides && hExtCollides then 
                    (p, { v with X = -v.X; Y = -v.Y })
                elif vExtCollides then 
                    ({p with Y = projected.Y}, { v with X = -v.X })
                elif hExtCollides then 
                    ({p with X = projected.X}, { v with Y = -v.Y })
                // If neither extended points collide, the wall is a single point and acts like a corner
                else
                    (p, { v with X = -v.X; Y = -v.Y })
    else
        // if the projected point doesn't collide with anything, it moves to that point with the same velocity
        (projected, v)

/// Checks if a wall collides with any another wall in a wall list.
/// Walls may touch other walls but must not go through them; however
/// as wall positions are specified by inclusive start and end points, 
/// these start and end points must be strictly exclusive. 
let wallCollidesWalls (newWall: Wall) (walls: Wall list) : bool =
    let rec loop collides walls =
        if collides then
            true
        else
            match (newWall, walls) with
            | (_, []) -> collides
            | (Horizontal(yNew, x1New, x2New), Horizontal(y, x1, x2) :: remaining) ->
                // Beginning of horizontal wall within an existing wall. 
                if yNew = y && x1New >= x1 && x1New <= x2 then
                    loop true remaining
                // End of horizontal wall within an existing wall.
                elif yNew = y && x2New >= x1 && x2New <= x2 then
                    loop true remaining
                // Begins before and ends after an existing wall on the same line.
                elif yNew = y && x1New <= x1 && x2New >= x2 then
                    loop true remaining
                else
                    loop collides remaining
            | (Vertical(xNew, y1New, y2New), Vertical(x, y1, y2) :: remaining) ->
                if xNew = x && y1New >= y1 && y1New <= y2 then
                    loop true remaining
                elif xNew = x && y2New >= y1 && y2New <= y2 then
                    loop true remaining
                elif xNew = x && y1New <= y1 && y2New >= y2 then
                    loop true remaining
                else
                    loop collides remaining
            | (Horizontal(yNew, x1New, x2New), Vertical(x, y1, y2) :: remaining) ->
                if x >= x1New && x <= x2New && yNew >= y1 && yNew <= y2 then
                    loop true remaining
                else
                    loop collides remaining
            | (Vertical(xNew, y1New, y2New), Horizontal(y, x1, x2) :: remaining) ->
                if y >= y1New && y <= y2New && xNew >= x1 && xNew <= x2 then
                    loop true remaining
                else
                    loop collides remaining

    loop false walls

/// Check if a wall is within the bounds of the field. 
let wallInBounds (wall: Wall) : bool = 
    match wall with 
    | Horizontal(y, x1, x2) ->
        y >= 0<m> && y <= MaxHeight && x1 >= 0<m> && x1 <= MaxWidth && x2 >= 0<m> && x2 <= MaxWidth
    | Vertical(x, y1, y2) ->
        x >= 0<m> && x <= MaxWidth && y1 >= 0<m> && y1 <= MaxHeight && y2 >= 0<m> && y2 <= MaxHeight

/// Remove a wall from a list (if it is not present, returns the original list).
/// Requires that the wall only be present in this list once.
let removeWall (toRemove: Wall) (walls: Wall list) : Wall list = 
    let rec loop remaining acc =
        match remaining with
        | [] -> List.rev acc
        | wall :: remaining ->
            if wall = toRemove then
                (List.rev acc) @ remaining
            else
                loop remaining (wall :: acc)

    loop walls []

let removeWalls (toRemove: Wall list) (walls: Wall list) : Wall list = 
    List.fold (fun acc w -> removeWall w acc) walls toRemove