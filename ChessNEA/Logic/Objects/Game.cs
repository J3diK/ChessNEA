using System;
using System.Collections.Generic;
using ChessNEA.Logic.Objects.LinkedList;

namespace ChessNEA.Logic.Objects;

public class Game
{
    /// <summary>
    ///     Each square is either an empty string, denoting a lack of a piece, or a 2 character string.
    ///     The 1st character is 'w' or 'b', denoting whether a piece is white or black.
    ///     The 2nd character denotes the piece type as follows:
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
    ///     The 3rd character for pawns is either 0, 1, or 2 to denote if a pawn can be taken en passant.
    ///     A 0 means that it cannot currently be taken, a 1 means it can, a 2 means it never can.
    ///     The 3rd character for rooks and kings is either 0 or 1 and denotes if the rook has moved or not.
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

    private (int x, int y) _whiteKingPosition = (0, 4);
    private (int x, int y) _blackKingPosition = (7, 4);
    public bool IsFinished { get; private set; }
    public double Score { get; set; }
    public bool IsWhiteTurn { get; set; }
    private int _movesSincePawnOrCapture;
    public ((int oldX, int oldY), (int newX, int newY)) LastMove { get; private set; }

    private readonly Dictionary<int[], int> _boardStates;

    public Game(bool? isWhiteTurn = null, string[,]? board = null)
    {
        if (board is not null)
        {
            Board = board;
        }
        IsWhiteTurn = isWhiteTurn is null || isWhiteTurn.Value;
        _boardStates = new Dictionary<int[], int>(new IntArrayComparer())
        {
            { EncodeBoard(Board), 1 }
        };
    }

    private void UpdateBoardStates(int[] boardState)
    {
        if (_boardStates.TryGetValue(boardState, out int value))
        {
            _boardStates[boardState] = ++value;
        }
        else
        {
            _boardStates.Add(boardState, 1);
        }
    }
    
    public void MakeNullMove()
    {
        if (IsWhiteTurn) _movesSincePawnOrCapture++;
        IsWhiteTurn = !IsWhiteTurn;
    }
    
    public int[] GetHash()
    {
        // int[] hash = new int[8];
        //
        // foreach (int[] boardState in _boardStates.Keys)
        // {
        //     for (int i = 0; i < 8; i++)
        //     {
        //         hash[i] ^= boardState[i];
        //     }
        // }
        // return hash;

        return EncodeBoard(Board);
    }
    
    private void CheckRepeatPositions()
    {
        int[] boardState = EncodeBoard(Board);
        
        UpdateBoardStates(boardState);
        if (_boardStates[boardState] != 3) return;
        Score = 0;
        IsFinished = true;
    }

    private static int[] EncodeBoard(string[,] board)
    {
        int[] boardState = new int[8];

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
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
            }
        }

        return boardState;
    }

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
    
    private bool IsCheckmate()
    {
        for (int i = 0; i < 8; i++)
        for (int j = 0; j < 8; j++)
        {
            if (Board[i, j] == "" || !IsCurrentPlayerColour(Board[i, j])) continue;
            if (GetMoves((i, j)) is not null) return false;
        }

        return true;
    }

    private bool IsCurrentPlayerColour(string piece)
    {
        return !((piece[0] == 'w') ^ IsWhiteTurn);
    }

    public bool IsKingInCheck((int x, int y)? newPosition = null, (int x, int y)? oldPosition = null)
    {
        (int x, int y) kingPosition = IsWhiteTurn ? _whiteKingPosition : _blackKingPosition;
        bool isMoving = newPosition is not null && oldPosition is not null;
        string oldPositionPiece = "";
        string newPositionPiece = "";

        if (isMoving)
        {
            oldPositionPiece = Board[oldPosition!.Value.x, oldPosition.Value.y];
            newPositionPiece = Board[newPosition!.Value.x, newPosition.Value.y];
            Board[oldPosition.Value.x, oldPosition.Value.y] = "";
            Board[newPosition.Value.x, newPosition.Value.y] = oldPositionPiece;

            if (oldPositionPiece[1] == 'K') kingPosition = newPosition.Value;
        }

        LinkedList.LinkedList<(int, int)>? moves = new();


        // TODO: Modify how adding linked list works to allow null
        moves += GetOnlyCertainPiece(GetMovesPawn(kingPosition, true), 'P') ?? new LinkedList.LinkedList<(int, int)>();
        moves = (moves ?? new LinkedList.LinkedList<(int, int)>()) +
                (GetOnlyCertainPiece(GetMovesKnight(kingPosition, true), 'N') ?? new LinkedList.LinkedList<(int, int)>());
        moves = (moves ?? new LinkedList.LinkedList<(int, int)>()) +
                (GetOnlyCertainPiece(GetMovesBishop(kingPosition, true), 'B') ?? new LinkedList.LinkedList<(int, int)>());
        moves = (moves ?? new LinkedList.LinkedList<(int, int)>()) +
                (GetOnlyCertainPiece(GetMovesRook(kingPosition, true), 'R') ?? new LinkedList.LinkedList<(int, int)>());
        moves = (moves ?? new LinkedList.LinkedList<(int, int)>()) +
                (GetOnlyCertainPiece(GetMovesQueen(kingPosition, true), 'Q') ?? new LinkedList.LinkedList<(int, int)>());

        if (isMoving)
        {
            Board[oldPosition!.Value.x, oldPosition.Value.y] = oldPositionPiece;
            Board[newPosition!.Value.x, newPosition.Value.y] = newPositionPiece;
        }

        if (moves is null) return false;

        Node<(int, int)>? node = moves.Head;
        while (node is not null)
        {
            if (!IsCurrentPlayerColour(Board[node.Data.Item1, node.Data.Item2])) return true;

            node = node.NextNode;
        }

        return false;
    }

    private LinkedList.LinkedList<(int, int)>? GetOnlyCertainPiece(LinkedList.LinkedList<(int, int)>? moves, char piece)
    {
        LinkedList.LinkedList<(int, int)> newMoves = new();

        if (moves is null) return null;

        Node<(int, int)>? node = moves.Head;
        while (node is not null)
        {
            if (Board[node.Data.Item1, node.Data.Item2][1] == piece) newMoves.AddNode(node.Data);

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
    /// <param name="oldPosition">The position the pawn that is moving is/was at</param>
    /// <param name="newPosition">The position the pawn that is moving to</param>
    /// <returns>Whether a pawn is taking another pawn en passant</returns>
    private bool IsTakingEnPassant((int x, int y) oldPosition, (int x, int y) newPosition)
    {
        return Math.Abs(oldPosition.x - newPosition.x) == 1 && Math.Abs(oldPosition.y - newPosition.y) == 1
                                                            && Board[oldPosition.x, newPosition.y] != "" &&
                                                            Board[oldPosition.x, newPosition.y][1] == 'P'
                                                            && !IsCurrentPlayerColour(Board[oldPosition.x,
                                                                newPosition.y])
                                                            && Board[oldPosition.x, newPosition.y][2] == '1';
    }

    public bool IsPromotingMove((int x, int y) oldPosition, (int x, int y) newPosition)
    {
        return Board[oldPosition.x, oldPosition.y][1] == 'P' && ReachedOppositeEnd(newPosition);
    }
    
    private bool ReachedOppositeEnd((int x, int y) position)
    {
        return (IsWhiteTurn && position.x == 7) || (!IsWhiteTurn && position.x == 0);
    }

    /// <summary>
    ///     Moves a piece on the board from one position to another
    /// </summary>
    /// <param name="oldPosition">The position the piece is currently at</param>
    /// <param name="newPosition">The position the piece is to move to</param>
    /// <param name="promotion"></param>
    public void MovePiece((int x, int y) oldPosition, (int x, int y) newPosition, char? promotion = null)
    {
        LastMove = (oldPosition, newPosition);
        
        if (Board[oldPosition.x, oldPosition.y][1] == 'P')
        {
            _movesSincePawnOrCapture = -1;
            if (Math.Abs(oldPosition.x - newPosition.x) == 2)
                Board[oldPosition.x, oldPosition.y] = Board[oldPosition.x, oldPosition.y][..2] + '1';
            else
                Board[oldPosition.x, oldPosition.y] = Board[oldPosition.x, oldPosition.y][..2] + '2';

            if (IsTakingEnPassant(oldPosition, newPosition)) Board[oldPosition.x, newPosition.y] = "";

            if (ReachedOppositeEnd(newPosition))
            {
                Board[oldPosition.x, oldPosition.y] = Board[oldPosition.x, oldPosition.y][..1] + promotion;
                if (promotion == 'R')
                {
                    Board[oldPosition.x, oldPosition.y] += '1'; // Prevent castling with promoted rook
                }
            }
        }

        switch (Board[oldPosition.x, oldPosition.y][1])
        {
            case 'K':
                Board[oldPosition.x, oldPosition.y] = Board[oldPosition.x, oldPosition.y][..2] + '1';
                UpdateKingPosition(newPosition);
                break;
            case 'R':
                Board[oldPosition.x, oldPosition.y] = Board[oldPosition.x, oldPosition.y][..2] + '1';
                break;
        }

        // If castling
        if (Board[oldPosition.x, oldPosition.y][1] == 'K' && Math.Abs(oldPosition.y - newPosition.y) > 1)
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

        if (Board[newPosition.x, newPosition.y] != "") _movesSincePawnOrCapture = -1;

        Board[newPosition.x, newPosition.y] = Board[oldPosition.x, oldPosition.y];
        Board[oldPosition.x, oldPosition.y] = "";
        IsWhiteTurn = !IsWhiteTurn;

        for (int i = 0; i < 8; i++)
        for (int j = 0; j < 8; j++)
            if (Board[i, j] != "" && Board[i, j][1] == 'P' && !(i == newPosition.x && j == newPosition.y))
                Board[i, j] = ChangeEnPassant(Board[i, j]);

        if (IsWhiteTurn) _movesSincePawnOrCapture++;

        if (IsCheckmate())
        {
            Score = IsWhiteTurn ? double.NegativeInfinity : double.PositiveInfinity;
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
    /// <param name="position">Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)</param>
    /// <returns>A list of possible moves</returns>
    public LinkedList.LinkedList<(int, int)>? GetMoves((int x, int y) position)
    {
        if (Board[position.x, position.y] == "") return null;

        if (!IsCurrentPlayerColour(Board[position.x, position.y])) return null;

        LinkedList.LinkedList<(int, int)>? moves = Board[position.x, position.y][1] switch
        {
            'P' => GetMovesPawn(position),
            'N' => GetMovesKnight(position),
            'B' => GetMovesBishop(position),
            'R' => GetMovesRook(position),
            'Q' => GetMovesQueen(position),
            'K' => GetMovesKing(position),
            _ => throw new ArgumentException($"Unknown piece type at {position}.")
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
    /// <param name="position">Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)</param>
    /// <param name="checkingCheck">If the moves are being fetched to check for a check</param>
    /// <returns>A list of possible moves</returns>
    private LinkedList.LinkedList<(int, int)>? GetMovesPawn((int x, int y) position, bool checkingCheck = false)
    {
        LinkedList.LinkedList<(int, int)> moves = new();
        int multiplier = IsWhiteTurn ? 1 : -1;
        int upperLimit = 1;


        if (checkingCheck)
            upperLimit = 0;
        // Pawn can move 2 squares forwards if not yet moved
        else if (Board[position.x, position.y][2] == '0') upperLimit = 2;

        for (int i = 1; i <= upperLimit; i++)
            // If square i units above is free
            if (Board[position.x + i * multiplier, position.y] == "")
                moves.AddNode((position.x + i * multiplier, position.y));
            else
                break;

        for (int i = -1; i < 2; i += 2)
        {
            if ((position.y + i < 0) | (position.y + i > 7)) continue;
            // If square to the left/right and up 1 is not empty AND is of opposite colour to the current player
            if (Board[position.x + multiplier, position.y + i] != "" &&
                !IsCurrentPlayerColour(Board[position.x + multiplier, position.y + i]))
                moves.AddNode((position.x + multiplier, position.y + i));
        }

        // Check for en passant
        for (int i = -1; i < 2 && !checkingCheck; i += 2)
        {
            if ((position.y + i < 0) | (position.y + i > 7)) continue;

            // If there is a pawn next to the pawn we are getting the moves for
            if (Board[position.x, position.y + i] != "" && Board[position.x, position.y + i][1] == 'P')
                // If opposite colour
                if (!IsCurrentPlayerColour(Board[position.x, position.y + i]))
                    // If can be taken en passant
                    if (Board[position.x, position.y + i][2] == '1')
                        moves.AddNode((position.x + multiplier, position.y + i));
        }

        return moves.Head is null ? null : moves;
    }

    /// <summary>
    ///     Returns a list of possible moves a knight can make
    /// </summary>
    /// <param name="position">Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)</param>
    /// <param name="checkingCheck">If checking moves to determine if king is in check</param>
    /// <returns>A list of possible moves</returns>
    private LinkedList.LinkedList<(int, int)>? GetMovesKnight((int x, int y) position, bool checkingCheck = false)
    {
        LinkedList.LinkedList<(int x, int y)> moves = new();

        // Checks all 8 possibilities of (±2, ±1) and (±1, ±2)
        for (int i = 1; i < 3; i++)
        for (int j = -1; j < 2; j += 2)
        for (int k = -1; k < 2; k += 2)
        {
            if ((position.x + j * i < 0) | (position.x + j * i > 7)) continue;

            if ((position.y + k * (3 - i) < 0) | (position.y + k * (3 - i) > 7)) continue;
            // If square to the left/right and up 1 is empty OR is of opposite colour to the current player
            if (Board[position.x + j * i, position.y + k * (3 - i)] != "")
            {
                if (IsCurrentPlayerColour(Board[position.x + j * i, position.y + k * (3 - i)])) continue;
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
    /// <param name="position">Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)</param>
    /// <param name="checkingCheck">If checking moves to determine if king is in check</param>
    /// <returns>A list of possible moves</returns>
    private LinkedList.LinkedList<(int, int)>? GetMovesBishop((int x, int y) position, bool checkingCheck = false)
    {
        // Trailing is the / line, leading is the \ line.
        int trailingDiagonalLeftMax = Math.Min(position.x, position.y);
        int trailingDiagonalRightMax = Math.Min(7 - position.x, 7 - position.y);
        int leadingDiagonalLeftMax = Math.Min(7 - position.x, position.y);
        int leadingDiagonalRightMax = Math.Min(position.x, 7 - position.y);

        LinkedList.LinkedList<(int x, int y)> trailingMoves = GetMovesBishopLine(
            position,
            trailingDiagonalLeftMax,
            trailingDiagonalRightMax,
            false,
            checkingCheck
        );

        LinkedList.LinkedList<(int x, int y)> leadingMoves = GetMovesBishopLine(
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
    /// <param name="position">Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)</param>
    /// <param name="left">The furthest number of units left the bishop can go</param>
    /// <param name="right">The furthest number of units right the bishop can go</param>
    /// <param name="isLeading">If the diagonal to be checked is the leading diagonal or not (trailing).</param>
    /// <param name="checkingCheck">If checking moves to determine if king is in check</param>
    /// <returns>A list of possible moves</returns>
    private LinkedList.LinkedList<(int, int)> GetMovesBishopLine((int x, int y) position, int left, int right, bool isLeading,
        bool checkingCheck)
    {
        LinkedList.LinkedList<(int x, int y)> moves = new();

        int xMultiplier = isLeading ? 1 : -1;
        bool isCollision = false;

        // Check left
        for (int i = 1; i <= left && !isCollision; i++)
            if (Board[position.x + i * xMultiplier, position.y - i] != "")
            {
                isCollision = true;

                if (IsCurrentPlayerColour(Board[position.x + i * xMultiplier, position.y - i])) break;
                moves.AddNode((position.x + i * xMultiplier, position.y - i));
            }
            else if (!checkingCheck)
            {
                moves.AddNode((position.x + i * xMultiplier, position.y - i));
            }

        // Check right
        isCollision = false;
        for (int i = 1; i <= right && !isCollision; i++)
            if (Board[position.x - i * xMultiplier, position.y + i] != "")
            {
                isCollision = true;

                if (IsCurrentPlayerColour(Board[position.x - i * xMultiplier, position.y + i])) break;
                moves.AddNode((position.x - i * xMultiplier, position.y + i));
            }
            else if (!checkingCheck)
            {
                moves.AddNode((position.x - i * xMultiplier, position.y + i));
            }

        return moves;
    }

    /// <summary>
    ///     Returns a list of possible moves a rook can make
    /// </summary>
    /// <param name="position">Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)</param>
    /// <param name="checkingCheck">If checking moves to determine if king is in check</param>
    /// <returns>A list of possible moves</returns>
    private LinkedList.LinkedList<(int, int)>? GetMovesRook((int x, int y) position, bool checkingCheck = false)
    {
        LinkedList.LinkedList<(int x, int y)> moves = new();

        int verticalDownMax = 7 - position.x;
        int verticalUpMax = position.x;
        int horizontalRightMax = 7 - position.y;
        int horizontalLeftMax = position.y;
        bool isCollision = false;

        for (int i = 1; i <= verticalUpMax && !isCollision; i++)
        {
            if (Board[position.x - i, position.y] == "")
            {
                if (!checkingCheck) moves.AddNode((position.x - i, position.y));
                continue;
            }

            isCollision = true;
            // If same colour
            if (IsCurrentPlayerColour(Board[position.x - i, position.y])) break;

            moves.AddNode((position.x - i, position.y));
        }

        isCollision = false;
        for (int i = 1; i <= verticalDownMax && !isCollision; i++)
        {
            if (Board[position.x + i, position.y] == "")
            {
                if (!checkingCheck) moves.AddNode((position.x + i, position.y));
                continue;
            }

            isCollision = true;
            // If same colour
            if (IsCurrentPlayerColour(Board[position.x + i, position.y])) break;

            moves.AddNode((position.x + i, position.y));
        }

        isCollision = false;
        for (int i = 1; i <= horizontalRightMax && !isCollision; i++)
        {
            if (Board[position.x, position.y + i] == "")
            {
                if (!checkingCheck) moves.AddNode((position.x, position.y + i));
                continue;
            }

            isCollision = true;
            // If same colour
            if (IsCurrentPlayerColour(Board[position.x, position.y + i])) break;

            moves.AddNode((position.x, position.y + i));
        }

        isCollision = false;
        for (int i = 1; i <= horizontalLeftMax && !isCollision; i++)
        {
            if (Board[position.x, position.y - i] == "")
            {
                if (!checkingCheck) moves.AddNode((position.x, position.y - i));
                continue;
            }

            isCollision = true;
            // If same colour
            if (IsCurrentPlayerColour(Board[position.x, position.y - i])) break;

            moves.AddNode((position.x, position.y - i));
        }

        return moves.Head is null ? null : moves;
    }

    /// <summary>
    ///     Returns a list of possible moves a queen can make
    /// </summary>
    /// <param name="position">Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)</param>
    /// <param name="checkingCheck">If checking moves to determine if king is in check</param>
    /// <returns>A list of possible moves</returns>
    private LinkedList.LinkedList<(int, int)>? GetMovesQueen((int x, int y) position, bool checkingCheck = false)
    {
        return (GetMovesBishop(position, checkingCheck) ?? new LinkedList.LinkedList<(int, int)>()) +
               (GetMovesRook(position, checkingCheck) ?? new LinkedList.LinkedList<(int, int)>());
    }

    /// <summary>
    ///     Returns a list of possible moves a king can make
    /// </summary>
    /// <param name="position">Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)</param>
    /// <returns>A list of possible moves</returns>
    private LinkedList.LinkedList<(int, int)>? GetMovesKing((int x, int y) position)
    {
        (int, int) kingPosition = IsWhiteTurn ? _whiteKingPosition : _blackKingPosition;
        LinkedList.LinkedList<(int x, int y)> moves = new();

        for (int i = -1; i <= 1; i++)
        for (int j = -1; j <= 1; j++)
        {
            if (i == 0 && j == 0) continue;

            if ((position.x + i < 0) | (position.x + i > 7) |
                (position.y + j < 0) | (position.y + j > 7)) continue;

            // If opposite colour OR empty
            if (Board[position.x + i, position.y + j] == "")
                moves.AddNode((position.x + i, position.y + j));
            else if (!IsCurrentPlayerColour(Board[position.x + i, position.y + j]))
                moves.AddNode((position.x + i, position.y + j));
        }

        // Castling
        if (Board[position.x, position.y][2] != '0') return moves.Head is null ? null : moves;
        // If rook hasn't moved and there is no piece between the king and the rook
        if (Board[position.x, 0] != "" &&
            Board[position.x, 0][1..] == "R0" &&
            Board[position.x, 1] == "" && !IsKingInCheck((position.x, 1), kingPosition) &&
            Board[position.x, 2] == "" && !IsKingInCheck((position.x, 2), kingPosition) &&
            Board[position.x, 3] == "" && !IsKingInCheck((position.x, 3), kingPosition)
           )
            moves.AddNode((position.x, 2));

        if (Board[position.x, 7] != "" &&
            Board[position.x, 7][1..] == "R0" &&
            Board[position.x, 5] == "" && !IsKingInCheck((position.x, 5), kingPosition) &&
            Board[position.x, 6] == "" && !IsKingInCheck((position.x, 6), kingPosition)
           )
            moves.AddNode((position.x, 6));

        return moves.Head is null ? null : moves;
    }
}