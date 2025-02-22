using System;
using System.Collections.Generic;
using ChessNEA.Logic.Objects.LinkedList;

namespace ChessNEA.Logic.Objects;

public class Game
{
    /// <summary>
    ///     Stores how many times a position (state) has been repeated with the
    ///     encoded board as the key and the number of times it has been
    ///     repeated as the value.
    /// </summary>
    private readonly Dictionary<int[], int> _boardStates;

    private (int x, int y) _blackKingPosition = (7, 4);
    private (int x, int y) _whiteKingPosition = (0, 4);

    private int _movesSincePawnOrCapture;

    public bool IsFinished { get; private set; }

    /// <summary>
    ///     Positive infinity if white wins, negative infinity if black wins, 0
    ///     if it is a draw.
    /// </summary>
    public int Score { get; set; }

    public bool IsWhiteTurn { get; set; }

    public ((int oldX, int oldY), (int newX, int newY)) LastMove
    {
        get;
        private set;
    } = ((-1, -1), (-1, -1));

    /// <summary>
    ///     Each square is either an empty string, denoting a lack of a piece,
    ///     or a 2 character string. The 1st character is 'w' or 'b', denoting
    ///     whether a piece is white or black. The 2nd character denotes the
    ///     piece type as follows:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>'P' for Pawn</description>
    ///         </item>
    ///         <item>
    ///             <description>'N' for Knight</description>
    ///         </item>
    ///         <item>
    ///             <description>'B' for Bishop</description>
    ///         </item>
    ///         <item>
    ///             <description>'R' for Rook</description>
    ///         </item>
    ///         <item>
    ///             <description>'Q' for Queen</description>
    ///         </item>
    ///         <item>
    ///             <description>'K' for King</description>
    ///         </item>
    ///     </list>
    ///     The 3rd character for pawns is either 0, 1, or 2 to denote if a pawn
    ///     can be taken en passant. A 0 means that it cannot currently be
    ///     taken, a 1 means it can, a 2 means it never can. The 3rd character
    ///     for rooks and kings is either 0 or 1 and denotes if the rook has
    ///     moved (1) or not (0).
    /// </summary>
    public string[,] Board { get; } =
    {
        // A     B     C     D     E     F     G     H
        { "wR0", "wN", "wB", "wQ", "wK0", "wB", "wN", "wR0" }, // 1
        { "wP0", "wP0", "wP0", "wP0", "wP0", "wP0", "wP0", "wP0" }, // 2
        { "", "", "", "", "", "", "", "" }, // 3
        { "", "", "", "", "", "", "", "" }, // 4
        { "", "", "", "", "", "", "", "" }, // 5
        { "", "", "", "", "", "", "", "" }, // 6
        { "bP0", "bP0", "bP0", "bP0", "bP0", "bP0", "bP0", "bP0" }, // 7
        { "bR0", "bN", "bB", "bQ", "bK0", "bB", "bN", "bR0" } // 8
    };
    
    public Game(bool? isWhiteTurn = null, string[,]? board = null)
    {
        if (board is not null) Board = board;
        // True by default (is null)
        IsWhiteTurn = isWhiteTurn is null || isWhiteTurn.Value;
        _boardStates = new Dictionary<int[], int>(new IntArrayComparer())
        {
            { EncodeBoard(Board), 1 }
        };
    }

    /// <summary>
    ///     Updates _boardStates with the new board state. If the board state is
    ///     already in the dictionary, the value is incremented by 1. If not,
    ///     the board state is added to the dictionary with a value of 1.
    /// </summary>
    /// <param name="boardState"></param>
    private void UpdateBoardStates(int[] boardState)
    {
        if (_boardStates.TryGetValue(boardState, out int value))
            _boardStates[boardState] = ++value;
        else
            _boardStates.Add(boardState, 1);
    }

    /// <summary>
    ///     For null move pruning. Treats the current players move as being one
    ///     where no piece is moved.
    /// </summary>
    public void MakeNullMove()
    {
        if (IsWhiteTurn) _movesSincePawnOrCapture++;
        IsWhiteTurn = !IsWhiteTurn;
    }

    /// <summary>
    ///     Checks if a position has been repeated 3 times and if so, then marks
    ///     the game as finished and drawn.
    /// </summary>
    private void CheckRepeatPositions()
    {
        int[] boardState = EncodeBoard(Board);

        UpdateBoardStates(boardState);
        if (_boardStates[boardState] != 3) return;
        Score = 0;
        IsFinished = true;
    }

    /// <summary>
    ///     Encodes the board into an array of integers. Each integer represents
    ///     a row on the board. The leftmost bit represents the colour of the
    ///     piece, the next 3 bits represent the piece type.
    /// </summary>
    /// <param name="board">
    ///     A 2D string array representation of the current board state
    /// </param>
    /// <returns>The encoded board</returns>
    /// <exception cref="ArgumentException">
    ///     For an invalid colour or piece type
    /// </exception>
    public static int[] EncodeBoard(string[,] board)
    {
        int[] boardState = new int[8];

        for (int i = 0; i < 8; i++)
        for (int j = 0; j < 8; j++)
            if (board[i, j] == "")
            {
                boardState[i] <<= 4;
            }
            else
            {
                // Set colour
                boardState[i] = board[i, j][0] switch
                {
                    'w' => (boardState[i] << 1) + 1,
                    'b' => (boardState[i] << 1) + 0,
                    _ => throw new ArgumentException("Unknown colour")
                };

                // Set piece type
                boardState[i] = board[i, j][1] switch
                {
                    'P' => (boardState[i] << 3) + 1,
                    'N' => (boardState[i] << 3) + 2,
                    'B' => (boardState[i] << 3) + 3,
                    'R' => (boardState[i] << 3) + 4,
                    'Q' => (boardState[i] << 3) + 5,
                    'K' => (boardState[i] << 3) + 6,
                    _ => throw new ArgumentException("Unknown piece type")
                };
            }

        return boardState;
    }

    /// <summary>
    ///     Creates a deep copy of the current game.
    /// </summary>
    /// <returns>The deep copy</returns>
    public Game Copy()
    {
        return new Game(IsWhiteTurn, Board.Clone() as string[,])
        {
            _whiteKingPosition = _whiteKingPosition,
            _blackKingPosition = _blackKingPosition,
            _movesSincePawnOrCapture = _movesSincePawnOrCapture,
            Score = Score,
            IsFinished = IsFinished,
            LastMove = LastMove
        };
    }
    
    /// <summary>
    ///     Checks if a position has any legal moves.
    /// </summary>
    /// <returns>Whether a position has any legal moves.</returns>
    private bool IsAnyLegalMove()
    {
        for (int i = 0; i < 8; i++)
        for (int j = 0; j < 8; j++)
        {
            if (Board[i, j] == "" || !IsCurrentPlayerColour(Board[i, j]))
                continue;
            if (GetMoves((i, j)) is not null) return true;
        }

        return false;
    }

    /// <summary>
    ///     Checks if a piece is of the current player's colour.
    /// </summary>
    /// <param name="piece">The piece to be checked</param>
    /// <returns>If the piece is the current player's</returns>
    private bool IsCurrentPlayerColour(string piece)
    {
        return !((piece[0] == 'w') ^ IsWhiteTurn);
    }

    /// <summary>
    ///     Checks if the king is in check.
    /// </summary>
    /// <param name="newPosition">
    ///     The position the king is moving to (optional)
    /// </param>
    /// <param name="oldPosition">
    ///     The position the king is moving from (optional)
    /// </param>
    /// <returns>If the king is in check</returns>
    public bool IsKingInCheck((int x, int y)? newPosition = null,
        (int x, int y)? oldPosition = null)
    {
        (int x, int y) kingPosition =
            IsWhiteTurn ? _whiteKingPosition : _blackKingPosition;
        bool isMoving = newPosition is not null && oldPosition is not null;
        string oldPositionPiece = "";
        string newPositionPiece = "";

        if (isMoving)
        {
            // Move the king and store the piece (or lack of) at the new and old
            // positions
            oldPositionPiece = Board[oldPosition!.Value.x, oldPosition.Value.y];
            newPositionPiece = Board[newPosition!.Value.x, newPosition.Value.y];
            Board[oldPosition.Value.x, oldPosition.Value.y] = "";
            Board[newPosition.Value.x, newPosition.Value.y] = oldPositionPiece;

            if (oldPositionPiece[1] == 'K') kingPosition = newPosition.Value;
        }

        LinkedList.LinkedList<(int, int)>? moves = new();
        // Get all the moves the king can make as other pieces where the king
        // 'as' a piece is moving to a square with that piece.
        moves += GetOnlyCertainPiece(GetMovesPawn(kingPosition, true), 'P') ??
                 new LinkedList.LinkedList<(int, int)>();
        moves = (moves ?? new LinkedList.LinkedList<(int, int)>()) +
                (GetOnlyCertainPiece(GetMovesKnight(kingPosition, true), 'N') ??
                 new LinkedList.LinkedList<(int, int)>());
        moves = (moves ?? new LinkedList.LinkedList<(int, int)>()) +
                (GetOnlyCertainPiece(GetMovesBishop(kingPosition, true), 'B') ??
                 new LinkedList.LinkedList<(int, int)>());
        moves = (moves ?? new LinkedList.LinkedList<(int, int)>()) +
                (GetOnlyCertainPiece(GetMovesRook(kingPosition, true), 'R') ??
                 new LinkedList.LinkedList<(int, int)>());
        moves = (moves ?? new LinkedList.LinkedList<(int, int)>()) +
                (GetOnlyCertainPiece(GetMovesQueen(kingPosition, true), 'Q') ??
                 new LinkedList.LinkedList<(int, int)>());
        moves = (moves ?? new LinkedList.LinkedList<(int, int)>()) +
                (GetOnlyCertainPiece(GetMovesKing(kingPosition, true), 'K') ??
                 new LinkedList.LinkedList<(int, int)>());

        // Revert board to original state
        if (isMoving)
        {
            Board[oldPosition!.Value.x, oldPosition.Value.y] = oldPositionPiece;
            Board[newPosition!.Value.x, newPosition.Value.y] = newPositionPiece;
        }

        if (moves is null) return false;

        Node<(int, int)>? node = moves.Head;
        while (node is not null)
        {
            // If the king ('as' another piece) can move to a square with a
            // piece of the opposite colour
            if (!IsCurrentPlayerColour(Board[node.Data.Item1, node.Data.Item2]))
                return true;

            node = node.NextNode;
        }

        return false;
    }

    /// <summary>
    ///     Search through a list of moves and return only those which move to a
    ///     square with a certain piece.
    /// </summary>
    /// <param name="moves">The list of moves to be searched</param>
    /// <param name="piece">The certain piece</param>
    /// <returns>The filtered list of moves</returns>
    private LinkedList.LinkedList<(int, int)>? GetOnlyCertainPiece(
        LinkedList.LinkedList<(int, int)>? moves, char piece)
    {
        LinkedList.LinkedList<(int, int)> newMoves = new();

        if (moves is null) return null;

        Node<(int, int)>? node = moves.Head;
        while (node is not null)
        {
            // If the square the king is moving is occupied by the certain piece
            if (Board[node.Data.Item1, node.Data.Item2] != "" &&
                Board[node.Data.Item1, node.Data.Item2][1] == piece)
                newMoves.AddNode(node.Data);

            node = node.NextNode;
        }

        return newMoves.Head is null ? null : newMoves;
    }

    /// <summary>
    ///     Updates the en passant counter of a pawn
    /// </summary>
    /// <param name="piece">The string representing the pawn</param>
    /// <returns>The updated string</returns>
    /// <exception cref="ArgumentException">Invalid input</exception>
    private static string ChangeEnPassant(string piece)
    {
        return piece[2] switch
        {
            '0' => piece[..2] + '0',
            '1' => piece[..2] + '2',
            '2' => piece[..2] + '2',
            _ => throw new ArgumentException("Unknown counter value")
        };
    }

    /// <summary>
    ///     Update the attribute storing the position of the king to the new
    ///     position.
    /// </summary>
    /// <param name="position">The position the king is moving to</param>
    private void UpdateKingPosition((int x, int y) position)
    {
        if (IsWhiteTurn)
            _whiteKingPosition = position;
        else
            _blackKingPosition = position;
    }

    /// <summary>
    ///     Checks if a pawn is taking another pawn en passant
    /// </summary>
    /// <param name="oldPosition">
    ///     The position the pawn that is moving is/was
    /// </param>
    /// <param name="newPosition">
    ///     The position the pawn that is moving to
    /// </param>
    /// <returns>Whether a pawn is taking another pawn en passant</returns>
    private bool IsTakingEnPassant((int x, int y) oldPosition,
        (int x, int y) newPosition)
    {
        // If moving one square left/right
        // and one square up/down
        // and the square being moved to is not empty (to prevent an index error
        // for the next check)
        // and the piece being moved to is a pawn
        // and the piece being moved to is of the opposite colour
        // and the piece being moved to can be taken en passant
        return Math.Abs(oldPosition.x - newPosition.x) == 1 &&
               Math.Abs(oldPosition.y - newPosition.y) == 1
               && Board[oldPosition.x, newPosition.y] != "" &&
               Board[oldPosition.x, newPosition.y][1] == 'P'
               && !IsCurrentPlayerColour(Board[oldPosition.x,
                   newPosition.y])
               && Board[oldPosition.x, newPosition.y][2] == '1';
    }

    /// <summary>
    ///     If a pawn has reached the opposite end
    /// </summary>
    /// <param name="oldPosition">The position the piece is moving from</param>
    /// <param name="newPosition">The position the piece is moving to</param>
    /// <returns>If the pawn has reached the opposite end</returns>
    public bool IsPromotingMove((int x, int y) oldPosition,
        (int x, int y) newPosition)
    {
        return Board[oldPosition.x, oldPosition.y][1] == 'P' &&
               ReachedOppositeEnd(newPosition);
    }

    /// <summary>
    ///     If a piece has reached the opposite end
    /// </summary>
    /// <param name="position">The new position</param>
    /// <returns>If the piece has reached the opposite end</returns>
    private static bool ReachedOppositeEnd((int x, int y) position)
    {
        return position.x is 7 or 0;
    }

    /// <summary>
    ///     Moves a piece on the board from one position to another
    /// </summary>
    /// <param name="oldPosition">The position the piece is currently at</param>
    /// <param name="newPosition">The position the piece is to move to</param>
    /// <param name="promotion">
    ///     The piece the pawn is promoting into (optional)
    /// </param>
    public void MovePiece((int x, int y) oldPosition,
        (int x, int y) newPosition, char? promotion = null)
    {
        LastMove = (oldPosition, newPosition);

        if (Board[oldPosition.x, oldPosition.y][1] == 'P')
        {
            _movesSincePawnOrCapture = -1;
            // If moving en passant
            if (Math.Abs(oldPosition.x - newPosition.x) == 2)
                Board[oldPosition.x, oldPosition.y] =
                    Board[oldPosition.x, oldPosition.y][..2] + '1';
            else
                Board[oldPosition.x, oldPosition.y] =
                    Board[oldPosition.x, oldPosition.y][..2] + '2';

            // Remove piece captures en passant
            if (IsTakingEnPassant(oldPosition, newPosition))
                Board[oldPosition.x, newPosition.y] = "";

            if (LastMove == ((1, 3), (0, 3)))
            {
                Console.WriteLine();
            }
            
            if (ReachedOppositeEnd(newPosition))
            {
                Board[oldPosition.x, oldPosition.y] =
                    Board[oldPosition.x, oldPosition.y][..1] + promotion;
                if (promotion == 'R')
                    Board[oldPosition.x, oldPosition.y] +=
                        '1'; // Prevent castling with promoted rook
            }
        }

        // Update the flag for castling
        switch (Board[oldPosition.x, oldPosition.y][1])
        {
            case 'K':
                Board[oldPosition.x, oldPosition.y] =
                    Board[oldPosition.x, oldPosition.y][..2] + '1';
                UpdateKingPosition(newPosition);
                break;
            case 'R':
                Board[oldPosition.x, oldPosition.y] =
                    Board[oldPosition.x, oldPosition.y][..2] + '1';
                break;
        }

        // If castling
        if (Board[oldPosition.x, oldPosition.y][1] == 'K' &&
            Math.Abs(oldPosition.y - newPosition.y) > 1)
        {
            // If castling left or right
            if (newPosition.y == 2)
            {
                Board[oldPosition.x, 3] = Board[oldPosition.x, 0][..2] + '1';
                Board[oldPosition.x, 0] = "";
            }
            else
            {
                Board[oldPosition.x, 5] = Board[oldPosition.x, 7][..2] + '1';
                Board[oldPosition.x, 7] = "";
            }
        }

        // If capturing
        if (Board[newPosition.x, newPosition.y] != "")
            _movesSincePawnOrCapture = -1;

        Board[newPosition.x, newPosition.y] =
            Board[oldPosition.x, oldPosition.y];
        Board[oldPosition.x, oldPosition.y] = "";
        IsWhiteTurn = !IsWhiteTurn;

        // Update en passant counters for all pawns
        for (int i = 0; i < 8; i++)
        for (int j = 0; j < 8; j++)
            if (Board[i, j] != "" && Board[i, j][1] == 'P' &&
                !(i == newPosition.x && j == newPosition.y))
                Board[i, j] = ChangeEnPassant(Board[i, j]);

        if (IsWhiteTurn) _movesSincePawnOrCapture++;

        if (!IsAnyLegalMove())
        {
            if (IsKingInCheck())
            {
                Score = IsWhiteTurn
                    ? -(int)Constants.Infinity
                    : (int)Constants.Infinity;
            }
            else
            {
                Score = 0;
            }
            
            IsFinished = true;
        }
        else if (_movesSincePawnOrCapture == 50)
        {
            Score = 0;
            IsFinished = true;
        }

        CheckRepeatPositions();
    }

    /// <summary>
    ///     Returns a list of possible moves a piece can make
    /// </summary>
    /// <param name="position">
    ///     Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)
    /// </param>
    /// <returns>A list of possible moves</returns>
    public LinkedList.LinkedList<(int, int)>? GetMoves((int x, int y) position)
    {
        if (Board[position.x, position.y] == "") return null;

        if (!IsCurrentPlayerColour(Board[position.x, position.y])) return null;

        LinkedList.LinkedList<(int, int)>? moves =
            Board[position.x, position.y][1] switch
            {
                'P' => GetMovesPawn(position),
                'N' => GetMovesKnight(position),
                'B' => GetMovesBishop(position),
                'R' => GetMovesRook(position),
                'Q' => GetMovesQueen(position),
                'K' => GetMovesKing(position),
                _ => throw new ArgumentException(
                    $"Unknown piece type at {position}.")
            };

        if (moves is null) return null;

        Node<(int, int)>? node = moves.Head;

        while (node is not null)
        {
            if (IsKingInCheck(node.Data, position)) moves.RemoveNode(node.Data);

            node = node.NextNode;
        }

        return moves.Head is null ? null : moves;
    }

    /// <summary>
    ///     Returns a list of possible moves a pawn can make
    /// </summary>
    /// <param name="position">
    ///     Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)
    /// </param>
    /// <param name="checkingCheck">
    ///     If the moves are being fetched to check for a check
    /// </param>
    /// <returns>A list of possible moves</returns>
    private LinkedList.LinkedList<(int, int)>? GetMovesPawn(
        (int x, int y) position, bool checkingCheck = false)
    {
        LinkedList.LinkedList<(int, int)> moves = new();
        int multiplier = IsWhiteTurn ? 1 : -1;
        int upperLimit = 1;


        if (checkingCheck)
            upperLimit = 0;
        // Pawn can move 2 squares forwards if not yet moved
        else if (Board[position.x, position.y][2] == '0') upperLimit = 2;

        for (int i = 1; i <= upperLimit; i++)
        {
            // If out of range
            if ((position.x + i * multiplier < 0) |
                (position.x + i * multiplier > 7))
                Console.WriteLine();
            
            // If square i units above is free
            if (Board[position.x + i * multiplier, position.y] == "")
                moves.AddNode((position.x + i * multiplier, position.y));
            else
                break;
        }
            

        for (int i = -1; i < 2; i += 2)
        {
            if ((position.y + i < 0) | (position.y + i > 7)) continue;
            if ((position.x + multiplier < 0) | (position.x + multiplier > 7))
                continue;
            // If square to the left/right and up 1 is not empty AND is of
            // opposite colour to the current player
            if (Board[position.x + multiplier, position.y + i] != "" &&
                !IsCurrentPlayerColour(Board[position.x + multiplier,
                    position.y + i]))
                moves.AddNode((position.x + multiplier, position.y + i));
        }

        // Check for en passant
        for (int i = -1; i < 2 && !checkingCheck; i += 2)
        {
            if ((position.y + i < 0) | (position.y + i > 7)) continue;

            // If there is not a pawn next to the pawn we are getting the moves
            // for
            if (Board[position.x, position.y + i] == "" ||
                Board[position.x, position.y + i][1] != 'P') continue;
            // If same colour
            if (IsCurrentPlayerColour(Board[position.x, position.y + i]))
                continue;
            // If it cannot be taken en passant
            if (Board[position.x, position.y + i][2] != '1') continue;
            moves.AddNode((position.x + multiplier, position.y + i));
        }

        return moves.Head is null ? null : moves;
    }

    /// <summary>
    ///     Returns a list of possible moves a knight can make
    /// </summary>
    /// <param name="position">
    ///     Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)
    /// </param>
    /// <param name="checkingCheck">
    ///     If checking moves to determine if king is in check
    /// </param>
    /// <returns>A list of possible moves</returns>
    private LinkedList.LinkedList<(int, int)>? GetMovesKnight(
        (int x, int y) position, bool checkingCheck = false)
    {
        LinkedList.LinkedList<(int x, int y)> moves = new();

        // Checks all 8 possibilities of (+-2, +-1) and (+-1, +-2)
        for (int i = 1; i < 3; i++)
        for (int j = -1; j < 2; j += 2)
        for (int k = -1; k < 2; k += 2)
        {
            if ((position.x + j * i < 0) | (position.x + j * i > 7)) continue;

            if ((position.y + k * (3 - i) < 0) | (position.y + k * (3 - i) > 7))
                continue;
            if (Board[position.x + j * i, position.y + k * (3 - i)] != "")
            {
                if (IsCurrentPlayerColour(Board[position.x + j * i,
                        position.y + k * (3 - i)])) continue;
                moves.AddNode((position.x + j * i, position.y + k * (3 - i)));
            }
            else if (!checkingCheck)
            {
                moves.AddNode((position.x + j * i, position.y + k * (3 - i)));
            }
        }

        return moves.Head is null ? null : moves;
    }

    /// <summary>
    ///     Returns a list of possible moves a bishop can make
    /// </summary>
    /// <param name="position">
    ///     Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)
    /// </param>
    /// <param name="checkingCheck">
    ///     If checking moves to determine if king is in check
    /// </param>
    /// <returns>A list of possible moves</returns>
    private LinkedList.LinkedList<(int, int)>? GetMovesBishop(
        (int x, int y) position, bool checkingCheck = false)
    {
        // Trailing is the / line, leading is the \ line.
        int trailingDiagonalLeftMax = Math.Min(position.x, position.y);
        int trailingDiagonalRightMax = Math.Min(7 - position.x, 7 - position.y);
        int leadingDiagonalLeftMax = Math.Min(7 - position.x, position.y);
        int leadingDiagonalRightMax = Math.Min(position.x, 7 - position.y);

        LinkedList.LinkedList<(int x, int y)> trailingMoves =
            GetMovesBishopLine(
                position,
                trailingDiagonalLeftMax,
                trailingDiagonalRightMax,
                false,
                checkingCheck
            );

        LinkedList.LinkedList<(int x, int y)> leadingMoves =
            GetMovesBishopLine(
                position,
                leadingDiagonalLeftMax,
                leadingDiagonalRightMax,
                true,
                checkingCheck
            );

        return trailingMoves + leadingMoves;
    }

    /// <summary>
    ///     Returns a list of possible moves a bishop can make on a diagonal
    /// </summary>
    /// <param name="position">
    ///     Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)
    /// </param>
    /// <param name="left">
    ///     The furthest number of units left the bishop can go
    /// </param>
    /// <param name="right">
    ///     The furthest number of units right the bishop can go
    /// </param>
    /// <param name="isLeading">
    ///     If the diagonal to be checked is the leading diagonal or not
    ///     (trailing).
    /// </param>
    /// <param name="checkingCheck">
    ///     If checking moves to determine if king is in check
    /// </param>
    /// <returns>A list of possible moves</returns>
    private LinkedList.LinkedList<(int, int)> GetMovesBishopLine(
        (int x, int y) position, int left, int right,
        bool isLeading,
        bool checkingCheck)
    {
        int xMultiplier = isLeading ? 1 : -1;
        return GetMovesBishopHalfLine(position, left, xMultiplier, true,
                   checkingCheck) +
               GetMovesBishopHalfLine(position, right, xMultiplier, false,
                   checkingCheck)
               ?? new LinkedList.LinkedList<(int, int)>();
    }

    /// <summary>
    ///     Checks half of a diagonal for possible moves
    /// </summary>
    /// <param name="position">Coordinates of the current piece</param>
    /// <param name="max">The furthest distance the bishop can travel</param>
    /// <param name="xMultiplier">The multiplier for the x direction</param>
    /// <param name="isLeft">Whether the half line is to the left</param>
    /// <param name="checkingCheck">
    ///     If checking moves to determine if king is in check
    /// </param>
    /// <returns>The possible moves for the half of the diagonal</returns>
    private LinkedList.LinkedList<(int, int)> GetMovesBishopHalfLine(
        (int x, int y) position, int max, int xMultiplier,
        bool isLeft, bool checkingCheck)
    {
        int directionMultiplier = isLeft ? 1 : -1;
        LinkedList.LinkedList<(int x, int y)> moves = new();
        bool isCollision = false;

        for (int i = 1; i <= max && !isCollision; i++)
        {
            int posX = position.x + i * xMultiplier * directionMultiplier;
            int posY = position.y - i * directionMultiplier;

            if (Board[posX, posY] != "")
            {
                isCollision = true;

                if (IsCurrentPlayerColour(Board[posX, posY])) break;
                moves.AddNode((posX, posY));
            }
            else if (!checkingCheck)
            {
                moves.AddNode((posX, posY));
            }
        }

        return moves;
    }

    /// <summary>
    ///     Returns a list of possible moves a rook can make
    /// </summary>
    /// <param name="position">
    ///     Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)
    /// </param>
    /// <param name="checkingCheck">
    ///     If checking moves to determine if king is in check
    /// </param>
    /// <returns>A list of possible moves</returns>
    private LinkedList.LinkedList<(int, int)>? GetMovesRook(
        (int x, int y) position, bool checkingCheck = false)
    {
        int verticalDownMax = 7 - position.x;
        int verticalUpMax = position.x;
        int horizontalRightMax = 7 - position.y;
        int horizontalLeftMax = position.y;

        LinkedList.LinkedList<(int, int)>? moves =
            GetMovesRookHalfLine(position, verticalUpMax, true, false,
                checkingCheck);
        moves += GetMovesRookHalfLine(position, verticalDownMax, true, true,
            checkingCheck);
        moves = (moves ?? new LinkedList.LinkedList<(int, int)>()) +
                GetMovesRookHalfLine(position, horizontalLeftMax, false, false,
                    checkingCheck);
        moves = (moves ?? new LinkedList.LinkedList<(int, int)>()) +
                GetMovesRookHalfLine(position, horizontalRightMax, false, true,
                    checkingCheck);

        return moves;
    }

    /// <summary>
    ///     Checks half of a line for possible moves
    /// </summary>
    /// <param name="position">Coordinates of the current piece</param>
    /// <param name="max">The furthest distance the rook can travel</param>
    /// <param name="isX">Whether the line to check is in the x</param>
    /// <param name="isPositive">
    ///     Whether the half of the line to check is in the positive or negative
    /// </param>
    /// <param name="checkingCheck">
    ///     If checking moves to determine if king is in check
    /// </param>
    /// <returns>A list of possible moves for the half of the line</returns>
    private LinkedList.LinkedList<(int, int)> GetMovesRookHalfLine(
        (int x, int y) position, int max,
        bool isX, bool isPositive, bool checkingCheck)
    {
        LinkedList.LinkedList<(int, int)> moves = new();
        bool isCollision = false;

        for (int i = 1; i <= max && !isCollision; i++)
        {
            int posX =
                isX ? position.x + i * (isPositive ? 1 : -1) : position.x;
            int posY =
                isX ? position.y : position.y + i * (isPositive ? 1 : -1);

            if (Board[posX, posY] == "")
            {
                if (!checkingCheck) moves.AddNode((posX, posY));
                continue;
            }

            isCollision = true;
            if (IsCurrentPlayerColour(Board[posX, posY])) break;

            moves.AddNode((posX, posY));
        }

        return moves;
    }

    /// <summary>
    ///     Returns a list of possible moves a queen can make
    /// </summary>
    /// <param name="position">
    ///     Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)
    /// </param>
    /// <param name="checkingCheck">
    ///     If checking moves to determine if king is in check
    /// </param>
    /// <returns>A list of possible moves</returns>
    private LinkedList.LinkedList<(int, int)>? GetMovesQueen(
        (int x, int y) position, bool checkingCheck = false)
    {
        return (GetMovesBishop(position, checkingCheck) ??
                new LinkedList.LinkedList<(int, int)>()) +
               (GetMovesRook(position, checkingCheck) ??
                new LinkedList.LinkedList<(int, int)>());
    }

    /// <summary>
    ///     Returns a list of possible moves a king can make
    /// </summary>
    /// <param name="position">
    ///     Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)
    /// </param>
    /// <param name="checkingCheck">For if checking for check</param>
    /// <returns>A list of possible moves</returns>
    private LinkedList.LinkedList<(int, int)>? GetMovesKing(
        (int x, int y) position, bool checkingCheck = false)
    {
        (int, int) kingPosition =
            IsWhiteTurn ? _whiteKingPosition : _blackKingPosition;
        LinkedList.LinkedList<(int x, int y)> moves = new();

        for (int i = -1; i <= 1; i++)
        for (int j = -1; j <= 1; j++)
        {
            if (i == 0 && j == 0) continue;

            if ((position.x + i < 0) | (position.x + i > 7) |
                (position.y + j < 0) | (position.y + j > 7)) continue;
            
            // If opposite colour OR empty
            if (Board[position.x + i, position.y + j] == "" ||
                !IsCurrentPlayerColour(Board[position.x + i, position.y + j]))
                moves.AddNode((position.x + i, position.y + j));
        }

        // Castling
        if (checkingCheck || Board[position.x, position.y][2] != '0')
            return moves.Head is null ? null : moves;
        // If rook hasn't moved and there is no piece between the king and the
        // rook
        if (Board[position.x, 0] != "" &&
            Board[position.x, 0][1..] == "R0" &&
            Board[position.x, 1] == "" &&
            !IsKingInCheck((position.x, 1), kingPosition) &&
            Board[position.x, 2] == "" &&
            !IsKingInCheck((position.x, 2), kingPosition) &&
            Board[position.x, 3] == "" &&
            !IsKingInCheck((position.x, 3), kingPosition)
           )
            moves.AddNode((position.x, 2));

        if (Board[position.x, 7] != "" &&
            Board[position.x, 7][1..] == "R0" &&
            Board[position.x, 5] == "" &&
            !IsKingInCheck((position.x, 5), kingPosition) &&
            Board[position.x, 6] == "" &&
            !IsKingInCheck((position.x, 6), kingPosition)
           )
            moves.AddNode((position.x, 6));

        return moves.Head is null ? null : moves;
    }
}