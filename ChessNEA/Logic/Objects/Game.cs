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
    /// </summary>
    private string[,] _board =
    {  // A     B     C     D     E     F     G     H
        {"wR", "wK", "wB", "wQ", "wK", "wB", "wK", "wR"}, // 1
        {"wP", "wP", "wP", "wP", "wP", "wP", "wP", "wP"}, // 2
        {"", "", "", "", "", "", "", ""},                 // 3
        {"", "", "", "", "", "", "", ""},                 // 4
        {"", "", "", "", "", "", "", ""},                 // 5
        {"", "", "", "", "", "", "", ""},                 // 6
        {"bP", "bP", "bP", "bP", "bP", "bP", "bP", "bP"}, // 7
        {"bR", "bK", "bB", "bQ", "bK", "bB", "bK", "bR"}  // 8
    };

    private bool _isWon = false;
    private bool _isWhiteTurn = true;
    
    /// <summary>
    /// Returns a list of possible moves a piece can make
    /// </summary>
    /// <param name="position">Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)</param>
    /// <returns></returns>
    private LinkedList<(int, int)>? GetMoves((int x, int y) position)
    {
        return _board[position.x, position.y][1] switch
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

        for (int i = 1; i < 3; i++)
        {
            // If square i units above is free
            if (_board[position.x, position.y + 1] == "")
            {
                moves.AddNode((position.x, position.y + i));
            }
        }

        for (int i = -1; i < 2; i += 2)
        {
            // If square to the left/right and up 1 is not empty AND is of opposite colour to the current player
            if (_board[position.x + i, position.y + 1] != "" && 
                (_board[position.x + i, position.y + 1][0] == 'w' ^ _isWhiteTurn))
            {
                moves.AddNode((position.x + i, position.y + 1));
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
                    // If square to the left/right and up 1 is empty OR is of opposite colour to the current player
                    if (_board[position.x + j*i, position.y + k*(3-i)] == "" |
                        (_board[position.x + j*i, position.y + k*(3-i)][0] == 'w' ^ _isWhiteTurn))
                    {
                        moves.AddNode((position.x + j*i, position.y + k*(3-i)));
                    }
                }
            }
        }

        return moves.Head is null ? null : moves;
    }
    
    private LinkedList<(int, int)>? GetMovesBishop((int x, int y) position)
    {
        // Trailing is the / line, leading is the \ line.
        int trailingDiagonalLeftMax = Math.Min(position.x, position.y);
        int trailingDiagonalRightMax = Math.Min(7 - position.x, 7 - position.y);
        int leadingDiagonalLeftMax = Math.Min(7 - position.x, position.y);
        int leadingDiagonalRightMax = Math.Min(position.x, 7 - position.y);

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
        
        int yMultiplier = isTrailing ? 1 : -1;
        bool isCollision = false;
        
        // Check left
        for (int i = left; i >= 0 && !isCollision; i--)
        {
            if (_board[position.x - i, position.y - i*yMultiplier] != "")
            {
                isCollision = true;
            } 
            // If same colour
            if (!(_board[position.x - i, position.y - i*yMultiplier][0] == 'w' ^ _isWhiteTurn))
            {
                break;
            }
            moves.AddNode((position.x - i, position.y - i*yMultiplier));
        }
        
        // Check right
        isCollision = false;
        for (int i = 0; i <= right && !isCollision; i++)
        {
            if (_board[position.x + i, position.y + i*yMultiplier] != "")
            {
                isCollision = true;
            } 
            // If same colour
            if (!(_board[position.x + i, position.y + i*yMultiplier][0] == 'w' ^ _isWhiteTurn))
            {
                break;
            }

            moves.AddNode((position.x + i, position.y + i*yMultiplier));
        }

        return moves;
    }
    
    private LinkedList<(int, int)>? GetMovesRook((int x, int y) position)
    {
        LinkedList<(int x, int y)> moves = new();

        int verticalUpMax = 7 - position.y;
        int verticalDownMax = position.y;
        int horizontalRightMax = 7 - position.x;
        int horizontalLeftMax = position.x;
        bool isCollision = false;

        for (int i = 0; i < verticalUpMax && !isCollision; i++)
        {
            if (_board[position.x, position.y + i] != "")
            {
                isCollision = true;
            } 
            // If same colour
            if (!(_board[position.x, position.y + i][0] == 'w' ^ _isWhiteTurn))
            {
                break;
            }

            moves.AddNode((position.x, position.y + i));
        }

        isCollision = false;
        for (int i = 0; i < verticalDownMax && !isCollision; i++)
        {
            if (_board[position.x, position.y - i] != "")
            {
                isCollision = true;
            } 
            // If same colour
            if (!(_board[position.x, position.y - i][0] == 'w' ^ _isWhiteTurn))
            {
                break;
            }

            moves.AddNode((position.x, position.y - i));
        }
        
        isCollision = false;
        for (int i = 0; i < horizontalRightMax && !isCollision; i++)
        {
            if (_board[position.x + i, position.y] != "")
            {
                isCollision = true;
            } 
            // If same colour
            if (!(_board[position.x + i, position.y][0] == 'w' ^ _isWhiteTurn))
            {
                break;
            }

            moves.AddNode((position.x + i, position.y));
        }
        
        isCollision = false;
        for (int i = 0; i < horizontalLeftMax && !isCollision; i++)
        {
            if (_board[position.x - i, position.y] != "")
            {
                isCollision = true;
            } 
            // If same colour
            if (!(_board[position.x - i, position.y][0] == 'w' ^ _isWhiteTurn))
            {
                break;
            }

            moves.AddNode((position.x - i, position.y));
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
        LinkedList<(int x, int y)> moves = new();;
    }
}