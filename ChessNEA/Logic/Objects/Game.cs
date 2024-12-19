using System;
using ChessNEA.Logic.Objects.LinkedList;

namespace ChessNEA.Logic.Objects;

public class Game
{
    /// <summary>
    /// Each square is either an empty string, denoting a lack of a piece, or a 2 character string.
    /// The 1st character is 'w' or 'b', denoting whether a piece is white or black.
    /// The 2nd character denotes the piece type as follows:
    /// <list type="bullet">
    ///     <item><description>'P' for Pawn</description></item>
    ///     <item><description>'N' for Knight</description></item>
    ///     <item><description>'B' for Bishop</description></item>
    ///     <item><description>'R' for Rook</description></item>
    ///     <item><description>'Q' for Queen</description></item>
    ///     <item><description>'K' for King</description></item>
    /// </list>
    /// The 3rd character for pawns is either 0, 1, or 2 to denote if a pawn can be taken en passant.
    /// A 0 means that it cannot currently be taken, a 1 means it can, a 2 means it never can.
    /// The 3rd character for rooks and kings is either 0 or 1 and denotes if the rook has moved or not.
    /// </summary>
    
    public string[,] Board =
    {  // A     B     C     D     E     F     G     H
        {"wR0", "wN", "wB", "wQ", "wK0", "wB", "wN", "wR0"}, // 1
        {"wP0", "wP0", "wP0", "wP0", "wP0", "wP0", "wP0", "wP0"}, // 2
        {"", "", "", "", "", "", "", ""},                 // 3
        {"", "", "", "", "", "", "", ""},                 // 4
        {"", "", "", "", "", "", "", ""},                 // 5
        {"", "", "", "", "", "", "", ""},                 // 6
        {"bP0", "bP0", "bP0", "bP0", "bP0", "bP0", "bP0", "bP0"}, // 7
        {"bR0", "bN", "bB", "bQ", "bK0", "bB", "bN", "bR0"}  // 8
    };
    
    (int x, int y) _whiteKingPosition = (0, 4);
    (int x, int y) _blackKingPosition = (7, 4);
    private bool _isWon = false;
    private bool _isWhiteTurn = true;

    private bool IsKingInCheck((int x, int y)? position=null)
    {
        // TODO: check if the piece 'checking' is the actual piece
        
        // Check if opposite colour exists in possible moves king could take as any non-king piece
        (int x, int y) kingPosition = _isWhiteTurn ? _whiteKingPosition : _blackKingPosition;

        if (position is not null)
        {
            kingPosition = ((int x, int y))position;
        }

        LinkedList<(int, int)>? moves = new();
        moves = moves + (GetMovesPawn(kingPosition, true) ?? new LinkedList<(int, int)>()); 
        moves = (moves ?? new LinkedList<(int, int)>()) + (GetMovesKnight(kingPosition, true) ?? new LinkedList<(int, int)>());
        moves = (moves ?? new LinkedList<(int, int)>()) + (GetMovesBishop(kingPosition, true) ?? new LinkedList<(int, int)>());
        moves = (moves ?? new LinkedList<(int, int)>()) + (GetMovesRook(kingPosition, true) ?? new LinkedList<(int, int)>());
        moves = (moves ?? new LinkedList<(int, int)>()) + (GetMovesQueen(kingPosition, true) ?? new LinkedList<(int, int)>());
        
        if (moves is null)
        {
            return false;
        }

        Node<(int, int)>? node = moves.Head;
        while (node is not null)
        {
            if (Board[node.Data.Item1, node.Data.Item2][0] == 'w' ^ _isWhiteTurn)
            {
                return true;
            }
            
            node = node.NextNode;
        }

        return false;
    }

    /// <summary>
    /// Increments the pawn move counter by 1, or by 0 if already at 2
    /// </summary>
    /// <param name="counter">The current move counter</param>
    /// <returns>The incremented move counter</returns>
    /// <exception cref="ArgumentException">Invalid input</exception>
    private static char IncrementMoveCounter(char counter)
    {
        return counter switch
        {
            '0' => '1',
            '1' => '2',
            '2' => '2',
            _ => throw new ArgumentException("Unknown counter value")
        };
    }
    
    /// <summary>
    /// Updates the en passant counter of a pawn
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
        if (_isWhiteTurn)
        {
            _whiteKingPosition = position;
        }
        else
        {
            _blackKingPosition = position;
        }
    }
    
    /// <summary>
    /// Checks if a pawn is taking another pawn en passant
    /// </summary>
    /// <param name="oldPosition">The position the pawn that is moving is/was at</param>
    /// <param name="newPosition">The position the pawn that is moving to</param>
    /// <returns>Whether a pawn is taking another pawn en passant</returns>
    private bool IsTakingEnPassant((int x, int y) oldPosition, (int x, int y) newPosition)
    {
        return Math.Abs(oldPosition.x - newPosition.x) == 1 && Math.Abs(oldPosition.y - newPosition.y) == 1
            && Board[oldPosition.x, newPosition.y] != "" && Board[oldPosition.x, newPosition.y][1] == 'P'
            && (Board[oldPosition.x, newPosition.y][0] == 'w' ^ _isWhiteTurn)
            && Board[oldPosition.x, newPosition.y][2] == '1';
    }

    /// <summary>
    /// Moves a piece on the board from one position to another
    /// </summary>
    /// <param name="oldPosition">The position the piece is currently at</param>
    /// <param name="newPosition">The position the piece is to move to</param>
    public void MovePiece((int x, int y) oldPosition, (int x, int y) newPosition)
    {
        if (Board[oldPosition.x, oldPosition.y][1] == 'P')
        {
            if (Math.Abs(oldPosition.x - newPosition.x) == 2)
            {
                Board[oldPosition.x, oldPosition.y] = Board[oldPosition.x, oldPosition.y][..2] + '1';
            }
            else
            {
                Board[oldPosition.x, oldPosition.y] = Board[oldPosition.x, oldPosition.y][..2] + '2';
            }

            if (IsTakingEnPassant(oldPosition, newPosition))
            {
                Board[oldPosition.x, newPosition.y] = "";
            }
        }

        if (Board[oldPosition.x, oldPosition.y][1] == 'K' | Board[oldPosition.x, oldPosition.y][1] == 'R')
        {
            Board[oldPosition.x, oldPosition.y] = Board[oldPosition.x, oldPosition.y][..2] + '1';
            UpdateKingPosition(newPosition);
        }
        
        // If castling
        if (Board[oldPosition.x, oldPosition.y][1] == 'K' && Math.Abs(oldPosition.y - newPosition.y) > 1 )
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
        
        Board[newPosition.x, newPosition.y] = Board[oldPosition.x, oldPosition.y];
        Board[oldPosition.x, oldPosition.y] = "";
        _isWhiteTurn = !_isWhiteTurn;

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (Board[i, j] != "" && Board[i, j][1] == 'P' && !(i == newPosition.x && j == newPosition.y))
                {
                    Board[i, j] = ChangeEnPassant(Board[i, j]);
                }
            }
        }
    }
    
    /// <summary>
    /// Returns a list of possible moves a piece can make
    /// </summary>
    /// <param name="position">Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)</param>
    /// <returns>A list of possible moves</returns>
    public LinkedList<(int, int)>? GetMoves((int x, int y) position)
    {
        if (Board[position.x, position.y] == "")
        {
            return null;
        }

        if (Board[position.x, position.y][0] == 'w' ^ _isWhiteTurn)
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
            _ => throw new ArgumentException($"Unknown piece type at {position}.")
        };
    }

    /// <summary>
    /// Returns a list of possible moves a pawn can make
    /// </summary>
    /// <param name="position">Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)</param>
    /// <param name="checkingCheck">If the moves are being fetched to check for a check</param>
    /// <returns>A list of possible moves</returns>
    private LinkedList<(int, int)>? GetMovesPawn((int x, int y) position, bool checkingCheck = false)
    {
        LinkedList<(int, int)> moves = new();
        int multiplier = _isWhiteTurn ? 1 : -1;
        int upperLimit = 1;
        
        
        if (checkingCheck)
        {
            upperLimit = 0;
        }
        // Pawn can move 2 squares forwards if not yet moved
        else if (Board[position.x, position.y][2] == '0')
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
            if (position.y + i < 0 | position.y + i > 7)
            {
                continue;
            }
            // If square to the left/right and up 1 is not empty AND is of opposite colour to the current player
            if (Board[position.x + multiplier, position.y + i] != "" && 
                (Board[position.x + multiplier, position.y + i][0] == 'w' ^ _isWhiteTurn))
            {
                moves.AddNode((position.x + multiplier, position.y + i));
            }
        }
        
        // Check for en passant
        for (int i = -1; i < 2 && !checkingCheck; i += 2)
        {  
            if (position.y + i < 0 | position.y + i > 7)
            {
                continue;
            }
            
            // If there is a pawn next to the pawn we are getting the moves for
            if (Board[position.x, position.y + i] != "" && Board[position.x, position.y + i][1] == 'P')
            {
                // If opposite colour
                if (Board[position.x, position.y + i][0] == 'w' ^ _isWhiteTurn)
                {  // If can be taken en passant
                    if (Board[position.x, position.y + i][2] == '1')
                    {
                        moves.AddNode((position.x + multiplier, position.y + i));
                    }
                }
            }
        }

        return moves.Head is null ? null : moves;
    }
    
    /// <summary>
    /// Returns a list of possible moves a knight can make
    /// </summary>
    /// <param name="position">Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)</param>
    /// <param name="checkingCheck">If checking moves to determine if king is in check</param>
    /// <returns>A list of possible moves</returns>
    private LinkedList<(int, int)>? GetMovesKnight((int x, int y) position, bool checkingCheck = false)
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
                        moves.AddNode((position.x + j*i, position.y + k*(3-i)));
                    }
                    else if (!checkingCheck)
                    {
                        moves.AddNode((position.x + j*i, position.y + k*(3-i)));
                    }
                }
            }
        }

        return moves.Head is null ? null : moves;
    }
    
    /// <summary>
    /// Returns a list of possible moves a bishop can make
    /// </summary>
    /// <param name="position">Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)</param>
    /// <param name="checkingCheck">If checking moves to determine if king is in check</param>
    /// <returns>A list of possible moves</returns>
    private LinkedList<(int, int)>? GetMovesBishop((int x, int y) position, bool checkingCheck = false)
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
            false,
            checkingCheck
            );
        
        LinkedList<(int x, int y)> leadingMoves = GetMovesBishopLine(
            position,
            leadingDiagonalLeftMax,
            leadingDiagonalRightMax,
            true,
            checkingCheck
        );

        
        return trailingMoves + leadingMoves;
    }

    /// <summary>
    /// Returns a list of possible moves a bishop can make on a diagonal
    /// </summary>
    /// <param name="position">Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)</param>
    /// <param name="left">The furthest number of units left the bishop can go</param>
    /// <param name="right">The furthest number of units right the bishop can go</param>
    /// <param name="isLeading">If the diagonal to be checked is the leading diagonal or not (trailing).</param>
    /// <param name="checkingCheck">If checking moves to determine if king is in check</param>
    /// <returns>A list of possible moves</returns>
    private LinkedList<(int, int)> GetMovesBishopLine((int x, int y) position, int left, int right, bool isLeading, bool checkingCheck)
    {
        LinkedList<(int x, int y)> moves = new();
        
        int xMultiplier = isLeading ? 1 : -1;
        bool isCollision = false;
        
        // Check left
        for (int i = 1; i <= left && !isCollision; i++)
        {
            if (Board[position.x + i * xMultiplier, position.y - i] != "")
            {
                isCollision = true;

                if (!(Board[position.x + i * xMultiplier, position.y - i][0] == 'w' ^ _isWhiteTurn))
                {
                    break;
                }
                moves.AddNode((position.x + i*xMultiplier, position.y - i));
            } else if (!checkingCheck)
            {
                moves.AddNode((position.x + i*xMultiplier, position.y - i));
            }
        }
        
        // Check right
        isCollision = false;
        for (int i = 1; i <= right && !isCollision; i++)
        {
            if (Board[position.x - i * xMultiplier, position.y + i] != "")
            {
                isCollision = true;

                if (!(Board[position.x - i * xMultiplier, position.y + i][0] == 'w' ^ _isWhiteTurn))
                {
                    break;
                }
                moves.AddNode((position.x - i*xMultiplier, position.y + i));
            } else if (!checkingCheck)
            {
                moves.AddNode((position.x - i * xMultiplier, position.y + i));
            }
        }

        return moves;
    }
    
    /// <summary>
    /// Returns a list of possible moves a rook can make
    /// </summary>
    /// <param name="position">Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)</param>
    /// <param name="checkingCheck">If checking moves to determine if king is in check</param>
    /// <returns>A list of possible moves</returns>
    private LinkedList<(int, int)>? GetMovesRook((int x, int y) position, bool checkingCheck = false)
    {
        LinkedList<(int x, int y)> moves = new();

        int verticalDownMax = 7 - position.x;
        int verticalUpMax = position.x;
        int horizontalRightMax = 7 - position.y;
        int horizontalLeftMax = position.y;
        bool isCollision = false;

        for (int i = 1; i <= verticalUpMax && !isCollision; i++)
        {
            if (Board[position.x - i, position.y] == "")
            {
                if (!checkingCheck)
                {
                    moves.AddNode((position.x - i, position.y));
                }
                continue;
            } 
            isCollision = true;
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
            if (Board[position.x + i, position.y] == "")
            {
                if (!checkingCheck)
                {
                    moves.AddNode((position.x + i, position.y));
                }
                continue;
            } 
            isCollision = true;
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
            if (Board[position.x, position.y + i] == "")
            {
                if (!checkingCheck)
                {
                    moves.AddNode((position.x, position.y + i));
                }
                continue;
            } 
            isCollision = true;
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
            if (Board[position.x, position.y - i] == "")
            {
                if (!checkingCheck)
                {
                    moves.AddNode((position.x, position.y - i));
                }
                continue;
            } 
            isCollision = true;
            // If same colour
            if (!(Board[position.x, position.y - i][0] == 'w' ^ _isWhiteTurn))
            {
                break;
            }

            moves.AddNode((position.x, position.y - i));
        }

        return moves.Head is null ? null : moves;
    }

    /// <summary>
    /// Returns a list of possible moves a queen can make
    /// </summary>
    /// <param name="position">Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)</param>
    /// <param name="checkingCheck">If checking moves to determine if king is in check</param>
    /// <returns>A list of possible moves</returns>
    private LinkedList<(int, int)>? GetMovesQueen((int x, int y) position, bool checkingCheck = false)
    {
        return (GetMovesBishop(position, checkingCheck) ?? new LinkedList<(int, int)>()) + 
               (GetMovesRook(position, checkingCheck) ?? new LinkedList<(int, int)>());
    }
    
    /// <summary>
    /// Returns a list of possible moves a king can make
    /// </summary>
    /// <param name="position">Coordinates of the current piece from (0,0) to (7,7) (A1 to H8)</param>
    /// <returns>A list of possible moves</returns>
    private LinkedList<(int, int)>? GetMovesKing((int x, int y) position)
    {
        // TODO: fix
        if (IsKingInCheck())
        {
            return null;
        }
        
        LinkedList<(int x, int y)> moves = new();

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
        
        // Castling
        if (Board[position.x, position.y][2] == '0')
        {
            // If rook hasn't moved and there is no piece between the king and the rook
            if (Board[position.x, 0][1..] == "R0" && 
                Board[position.x, 1] == "" && !IsKingInCheck((position.x, 1)) &&
                Board[position.x, 2] == "" && !IsKingInCheck((position.x, 2)) &&
                Board[position.x, 3] == "" && !IsKingInCheck((position.x, 3))
                )
            {
                moves.AddNode((position.x, 2));
            }

            if (Board[position.x, 7][1..] == "R0" && 
                Board[position.x, 5] == "" && !IsKingInCheck((position.x, 5)) &&
                Board[position.x, 6] == "" && !IsKingInCheck((position.x, 6))
                )
            {
                moves.AddNode((position.x, 6));
            }
        }

        return moves.Head is null ? null : moves;
    }
}