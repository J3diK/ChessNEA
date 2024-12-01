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
    private LinkedList<string>? GetMoves((int x, int y) position)
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

    private LinkedList<string>? GetMovesPawn((int x, int y) position)
    {
        ;
    }
    
    private LinkedList<string>? GetMovesKnight((int x, int y) position)
    {
        ;
    }
    
    private LinkedList<string>? GetMovesBishop((int x, int y) position)
    {
        ;
    }
    
    private LinkedList<string>? GetMovesRook((int x, int y) position)
    {
        ;
    }
    
    private LinkedList<string>? GetMovesQueen((int x, int y) position)
    {
        ;
    }
    
    private LinkedList<string>? GetMovesKing((int x, int y) position)
    {
        ;
    }
}