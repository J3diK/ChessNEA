using System;
using ChessNEA.Logic.Objects.LinkedList;

namespace ChessNEA.Logic.Objects;

public class Game
{
    /// <summary>
    /// Each square is either an empty string, denoting a lack of a piece, or a 2 character string.
    /// The 1st character is 'w' or 'b', denoting whether or not a piece is white or black.
    /// The 2nd character denotes the piece type as follows:
    /// <list type="bullet">
    ///     <item><description>'P' for Pawn</description></item>
    ///     <item><description>'N' for Knight</description></item>
    ///     <item><description>'B' for Bishop</description></item>
    ///     <item><description>'R' for Rook</description></item>
    ///     <item><description>'Q' for Queen</description></item>
    ///     <item><description>'K' for King</description></item>
    /// </list>
    /// The 3rd character is exclusive for pawns and is either (y)es or (n)o to denote if the pawn has moved.
    /// </summary>
    public string[,] Board =
    {  // A     B     C     D     E     F     G     H
        {"wR", "wN", "wB", "wQ", "wK", "wB", "wN", "wR"}, // 1
        {"wPn", "wPn", "wPn", "wPn", "wPn", "wPn", "wPn", "wPn"}, // 2
        {"", "", "", "", "", "", "", ""},                 // 3
        {"", "", "", "", "", "", "", ""},                 // 4
        {"", "", "", "", "", "", "", ""},                 // 5
        {"", "", "", "", "", "", "", ""},                 // 6
        {"bPn", "bPn", "bPn", "bPn", "bPn", "bPn", "bPn", "bPn"}, // 7
        {"bR", "bN", "bB", "bQ", "bK", "bB", "bN", "bR"}  // 8
    };

    private bool _isWon = false;
    private bool _isWhiteTurn = true;
    
    /// <summary>
    /// Returns a list of possible moves a piece can make
    /// </summary>
    /// <param name="position">Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)</param>
    /// <returns></returns>
    public LinkedList<(int, int)>? GetMoves((int x, int y) position)
    {
        if (Board[position.x, position.y] == "")
        {
            return null;
        }
        return Board[position.x, position.y][1] switch
        {
            'P' => GetMovesPawn(position),
            'N' => GetMovesKnight(position),
            'B' => GetMovesBishop(position),
            'R' => GetMovesRook(position),
            'Q' => GetMovesQueen(position),
            'K' => GetMovesKing(position),
            _   => throw new ArgumentException($"Unknown piece type at {position}.")
        };
    }

    private LinkedList<(int, int)>? GetMovesPawn((int x, int y) position)
    {
        LinkedList<(int, int)> moves = new();
        int multiplier = _isWhiteTurn ? 1 : -1;
        int upperLimit = 1;
        
        if (Board[position.x, position.y][2] == 'n')
        {
            upperLimit = 2;
        }

        for (int i = 1; i <= upperLimit; i++)
        {
            // If square i units above is free
            if (Board[position.x + i*multiplier, position.y] == "")
            {
                moves.AddNode((position.x + i*multiplier, position.y));
            }
            else
            {
                break;
            }
        }

        for (int i = -1; i < 2; i += 2)
        {
            // If square to the left/right and up 1 is not empty AND is of opposite colour to the current player
            if (Board[position.x + 1, position.y + i] != "" && 
                (Board[position.x + 1, position.y + i][0] == 'w' ^ _isWhiteTurn))
            {
                moves.AddNode((position.x + 1, position.y + i));
            }
        }

        return moves.Head is null ? null : moves;
    }
    
    private LinkedList<(int, int)>? GetMovesKnight((int x, int y) position)
    {
        LinkedList<(int x, int y)> moves = new();
        
        // Checks all 8 possibilities of (±2, ±1) and (±1, ±2)
        for (int i = 1; i < 3; i++)
        {
            for (int j = -1; j < 2; j += 2)
            {
                for (int k = -1; k < 2; k += 2)
                {
                    if (position.x + j*i < 0 | position.x + j*i > 7)
                    {
                        continue;
                    }
                    
                    if (position.y + k*(3-i) < 0 | position.y + k*(3-i) > 7)
                    {
                        continue;
                    }
                    // If square to the left/right and up 1 is empty OR is of opposite colour to the current player
                    if (Board[position.x + j * i, position.y + k * (3 - i)] != "")
                    {
                        if (!(Board[position.x + j * i, position.y + k * (3 - i)][0] == 'w' ^ _isWhiteTurn))
                        {
                            continue;
                        }
                    }
                    
                    moves.AddNode((position.x + j*i, position.y + k*(3-i)));
                }
            }
        }

        return moves.Head is null ? null : moves;
    }
    
    private LinkedList<(int, int)>? GetMovesBishop((int x, int y) position)
    {
        // Trailing is the / line, leading is the \ line.
        int leadingDiagonalLeftMax = Math.Min(position.x, position.y);
        int leadingDiagonalRightMax = Math.Min(7 - position.x, 7 - position.y);
        int trailingDiagonalLeftMax = Math.Min(7 - position.x, position.y);
        int trailingDiagonalRightMax = Math.Min(position.x, 7 - position.y);

        LinkedList<(int x, int y)> trailingMoves = GetMovesBishopLine(
            position,
            trailingDiagonalLeftMax,
            trailingDiagonalRightMax,
            true
            );
        
        LinkedList<(int x, int y)> leadingMoves = GetMovesBishopLine(
            position,
            leadingDiagonalLeftMax,
            leadingDiagonalRightMax,
            false
        );

        
        return trailingMoves + leadingMoves;
    }

    private LinkedList<(int, int)> GetMovesBishopLine((int x, int y) position, int left, int right, bool isTrailing)
    {
        LinkedList<(int x, int y)> moves = new();
        
        int xMultiplier = isTrailing ? 1 : -1;
        bool isCollision = false;
        
        // Check left
        for (int i = 1; i < left && !isCollision; i++)
        {
            if (Board[position.x + i * xMultiplier, position.y - i] != "")
            {
                isCollision = true;

                if (!(Board[position.x + i * xMultiplier, position.y - i][0] == 'w' ^ _isWhiteTurn))
                {
                    break;
                }
            }
            
            moves.AddNode((position.x + i*xMultiplier, position.y - i));
        }
        
        // Check right
        isCollision = false;
        for (int i = 1; i < right && !isCollision; i++)
        {
            if (Board[position.x - i * xMultiplier, position.y + i] != "")
            {
                isCollision = true;

                if (!(Board[position.x - i * xMultiplier, position.y + i][0] == 'w' ^ _isWhiteTurn))
                {
                    break;
                }
            }
            
            moves.AddNode((position.x - i*xMultiplier, position.y + i));
        }

        return moves;
    }
    
    private LinkedList<(int, int)>? GetMovesRook((int x, int y) position)
    {
        LinkedList<(int x, int y)> moves = new();

        int verticalDownMax = 7 - position.x;
        int verticalUpMax = position.x;
        int horizontalRightMax = 7 - position.y;
        int horizontalLeftMax = position.y;
        bool isCollision = false;

        for (int i = 1; i <= verticalUpMax && !isCollision; i++)
        {
            if (Board[position.x - i, position.y] != "")
            {
                isCollision = true;
            } 
            // If same colour
            if (!(Board[position.x - i, position.y][0] == 'w' ^ _isWhiteTurn))
            {
                break;
            }

            moves.AddNode((position.x - i, position.y));
        }

        isCollision = false;
        for (int i = 1; i <= verticalDownMax && !isCollision; i++)
        {
            if (Board[position.x + i, position.y] != "")
            {
                isCollision = true;
            } 
            // If same colour
            if (!(Board[position.x + i, position.y][0] == 'w' ^ _isWhiteTurn))
            {
                break;
            }

            moves.AddNode((position.x + i, position.y));
        }
        
        isCollision = false;
        for (int i = 1; i <= horizontalRightMax && !isCollision; i++)
        {
            if (Board[position.x, position.y + i] != "")
            {
                isCollision = true;
            } 
            // If same colour
            if (!(Board[position.x, position.y + i][0] == 'w' ^ _isWhiteTurn))
            {
                break;
            }

            moves.AddNode((position.x, position.y + i));
        }
        
        isCollision = false;
        for (int i = 1; i <= horizontalLeftMax && !isCollision; i++)
        {
            if (Board[position.x, position.y - i] != "")
            {
                isCollision = true;
            } 
            // If same colour
            if (!(Board[position.x, position.y - i][0] == 'w' ^ _isWhiteTurn))
            {
                break;
            }

            moves.AddNode((position.x, position.y - i));
        }

        return moves.Head is null ? null : moves;
    }
    
    private LinkedList<(int, int)>? GetMovesQueen((int x, int y) position)
    {
        return (GetMovesBishop(position) ?? new LinkedList<(int, int)>()) + 
               (GetMovesRook(position) ?? new LinkedList<(int, int)>());
    }
    
    private LinkedList<(int, int)>? GetMovesKing((int x, int y) position)
    {
        LinkedList<(int x, int y)> moves = new();
        
        // -1 0 1
        // -1 0 1
        // not 0, 0

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0)
                {
                    continue;
                }

                if (position.x + i < 0 | position.x + i > 7)
                {
                    continue;
                }
                if (position.y + i < 0 | position.y + i > 7)
                {
                    continue;
                }
                
                // If opposite colour OR empty
                if (Board[position.x + i, position.y + j] == "")
                {
                    moves.AddNode((position.x + i, position.y + j));
                } else if (Board[position.x + i, position.y + j][0] == 'w' ^ _isWhiteTurn)
                {
                    moves.AddNode((position.x + i, position.y + j));
                }
            }
        }

        return moves.Head is null ? null : moves;
    }
}