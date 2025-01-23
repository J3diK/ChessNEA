using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform;
using ChessNEA.Logic.Objects.LinkedList;

namespace ChessNEA.Logic.Objects;

// https://www.chessprogramming.org/Simplified_Evaluation_Function
public class Bot(bool isWhite = false)
{
    private static readonly int[,] PawnTable = {
        {  0,  0,  0,  0,  0,  0,  0,  0 },
        { 50, 50, 50, 50, 50, 50, 50, 50 },
        { 10, 10, 20, 30, 30, 20, 10, 10 },
        {  5,  5, 10, 25, 25, 10,  5,  5 },
        {  0,  0,  0, 20, 20,  0,  0,  0 },
        {  5, -5,-10,  0,  0,-10, -5,  5 },
        {  5, 10, 10,-20,-20, 10, 10,  5 },
        {  0,  0,  0,  0,  0,  0,  0,  0 }
    };
    private static readonly int[,] KnightTable = {
        { -50,-40,-30,-30,-30,-30,-40,-50 },
        { -40,-20,  0,  0,  0,  0,-20,-40 },
        { -30,  0, 10, 15, 15, 10,  0,-30 },
        { -30,  5, 15, 20, 20, 15,  5,-30 },
        { -30,  0, 15, 20, 20, 15,  0,-30 },
        { -30,  5, 10, 15, 15, 10,  5,-30 },
        { -40,-20,  0,  5,  5,  0,-20,-40 },
        { -50,-40,-30,-30,-30,-30,-40,-50 }
    };
    private static readonly int[,] BishopTable = {
        { -20,-10,-10,-10,-10,-10,-10,-20 },
        { -10,  0,  0,  0,  0,  0,  0,-10 },
        { -10,  0,  5, 10, 10,  5,  0,-10 },
        { -10,  5,  5, 10, 10,  5,  5,-10 },
        { -10,  0, 10, 10, 10, 10,  0,-10 },
        { -10, 10, 10, 10, 10, 10, 10,-10 },
        { -10,  5,  0,  0,  0,  0,  5,-10 },
        { -20,-10,-10,-10,-10,-10,-10,-20 }
    };
    private static readonly int[,] RookTable = {
        {  0,  0,  0,  0,  0,  0,  0,  0 },
        {  5, 10, 10, 10, 10, 10, 10,  5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        {  0,  0,  0,  5,  5,  0,  0,  0 }
    };
    private static readonly int[,] QueenTable = {
        { -20,-10,-10, -5, -5,-10,-10,-20 },
        { -10,  0,  0,  0,  0,  0,  0,-10 },
        { -10,  0,  5,  5,  5,  5,  0,-10 },
        { -5,  0,  5,  5,  5,  5,  0, -5 },
        {  0,  0,  5,  5,  5,  5,  0, -5 },
        { -10,  5,  5,  5,  5,  5,  0,-10 },
        { -10,  0,  5,  0,  0,  0,  0,-10 },
        { -20,-10,-10, -5, -5,-10,-10,-20 }
    };
    private static readonly int[,] KingTable = {
        { -30,-40,-40,-50,-50,-40,-40,-30 },
        { -30,-40,-40,-50,-50,-40,-40,-30 },
        { -30,-40,-40,-50,-50,-40,-40,-30 },
        { -30,-40,-40,-50,-50,-40,-40,-30 },
        { -20,-30,-30,-40,-40,-30,-30,-20 },
        { -10,-20,-20,-20,-20,-20,-20,-10 },
        {  20, 20,  0,  0,  0,  0, 20, 20 },
        {  20, 30, 10,  0,  0, 10, 30, 20 }
    };

    private static readonly int[,] KingEndgameTable =
    {
        { -50, -40, -30, -20, -20, -30, -40, -50 },
        { -30, -20, -10, 0, 0, -10, -20, -30 },
        { -30, -10, 20, 30, 30, 20, -10, -30 },
        { -30, -10, 30, 40, 40, 30, -10, -30 },
        { -30, -10, 30, 40, 40, 30, -10, -30 },
        { -30, -10, 20, 30, 30, 20, -10, -30 },
        { -30, -30, 0, 0, 0, 0, -30, -30 },
        { -50, -30, -30, -30, -30, -30, -30, -50 }
    };

    // Graph representing openings with at least 100k games played by masters, according to Lichess as of 2025-01-19.
    private static readonly Dictionary<string, List<string>> OpeningsBook = new()
    {
        // 0. Root
        { "", ["e2e4", "d2d4", "g1f3", "c2c4"] },
        // 1. White
        { "e2e4", ["e2e4 c7c5", "e2e4 e7e5", "e2e4 e7e6"] }, // King's Pawn Opening
        { "d2d4", ["d2d4 g8f6", "d2d4 d7d5"] }, // Queen's Pawn Opening
        { "g1f3", ["g1f3 g8f6"] }, // Réti Opening
        { "c2c4", [] }, // English Opening
        // 1. Black
        { "e2e4 c7c5", ["e2e4 c7c5 g1f3"] }, // Sicilian Defense
        { "e2e4 e7e5", ["e2e4 e7e5 g1f3"] }, // King's Pawn Opening
        { "e2e4 e7e6", ["e2e4 e7e6 d2d4"] }, // French Defense
        { "d2d4 g8f6", ["d2d4 g8f6 c2c4", "d2d4 g8f6 g1f3"] }, // Indian Game
        { "d2d4 d7d5", ["d2d4 d7d5 c2c4"] }, // Queen's Pawn Opening
        { "g1f3 g8f6", []}, // Réti Opening
        // 2. White
        { "e2e4 c7c5 g1f3", ["e2e4 c7c5 g1f3 d7d6", "e2e4 c7c5 g1f3 b8c6", "e2e4 c7c5 g1f3 e7e6"]}, // Sicilian Defense
        { "e2e4 e7e5 g1f3", ["e2e4 e7e5 g1f3 b8c6"]}, // King's Pawn Opening: King's Knight Variation
        { "e2e4 e7e6 d2d4", ["e2e4 e7e6 d2d4 d7d5"]}, // French Defense: Normal Variation
        { "d2d4 g8f6 c2c4", ["d2d4 g8f6 c2c4 e7e6", "d2d4 g8f6 c2c4 g7g6"]}, // Indian Game
        { "d2d4 g8f6 g1f3", []}, // Indian Game: Knights Variation
        { "d2d4 d7d5 c2c4", []}, // Queen's Gambit
        // 2. Black
        { "e2e4 c7c5 g1f3 d7d6", ["e2e4 c7c5 g1f3 d7d6 d2d4"]}, // Sicilian Defense
        { "e2e4 c7c5 g1f3 b8c6", []}, // Sicilian Defense: Old Sicilian Variation
        { "e2e4 c7c5 g1f3 e7e6", []}, // Sicilian Defense: French Variation
        { "e2e4 e7e5 g1f3 b8c6", ["e2e4 e7e5 g1f3 b8c6 f1b5"]}, // King's Pawn Opening: King's Knight Variation
        { "e2e4 e7e6 d2d4 d7d5", []}, // 
        { "d2d4 g8f6 c2c4 e7e6", ["d2d4 g8f6 c2c4 e7e6 g1f3"]}, // Indian Game: East Indian, Anti-Nimzo-Indian Variation
        { "d2d4 g8f6 c2c4 g7g6", ["d2d4 g8f6 c2c4 g7g6 b1c3"]}, // King's Indian Defense
        // 3. White
        { "e2e4 c7c5 g1f3 d7d6 d2d4", ["e2e4 c7c5 g1f3 d7d6 d2d4 c5d4"]}, // Sicilian Defense
        { "e2e4 e7e5 g1f3 b8c6 f1b5", ["e2e4 e7e5 g1f3 b8c6 f1b5 a7a6"]}, // King's Pawn Opening: King's Knight Variation
        { "d2d4 g8f6 c2c4 e7e6 g1f3", []}, // Indian Game
        { "d2d4 g8f6 c2c4 g7g6 b1c3", []}, // King's Indian Defense
        // 3. Black
        { "e2e4 c7c5 g1f3 d7d6 d2d4 c5d4", ["e2e4 c7c5 g1f3 d7d6 d2d4 c5d4 f3d4"]}, // Sicilian Defense
        { "e2e4 e7e5 g1f3 b8c6 f1b5 a7a6", []}, // King's Pawn Opening: King's Knight Variation: Normal Variation
        // 4. White // Sicilian Defense: Open Variation
        { "e2e4 c7c5 g1f3 d7d6 d2d4 c5d4 f3d4", ["e2e4 c7c5 g1f3 d7d6 d2d4 c5d4 f3d4 g8f6"]}, 
        // 4. Black // Sicilian Defense: Open Variation
        { "e2e4 c7c5 g1f3 d7d6 d2d4 c5d4 f3d4 g8f6", ["e2e4 c7c5 g1f3 d7d6 d2d4 c5d4 f3d4 g8f6 b1c3"]}, 
        // 5. White // Sicilian Defense: Open Variation
        { "e2e4 c7c5 g1f3 d7d6 d2d4 c5d4 f3d4 g8f6 b1c3", ["e2e4 c7c5 g1f3 d7d6 d2d4 c5d4 f3d4 g8f6 b1c3 a7a6"]}, 
        // 5. Black // Sicilian Defense: Open Variation: Najdorf Variation
        { "e2e4 c7c5 g1f3 d7d6 d2d4 c5d4 f3d4 g8f6 b1c3 a7a6", []}, 
    };
    private string _openingMoves = "";
    private bool _inOpeningBook = true;
    private bool _inEndgame = false;
    
    private readonly ConcurrentDictionary<((int oldX, int oldY), (int newX, int newY), char?), int> _moveVoteCount =
        new();
    // Avoid redundant calculations
    private readonly ConcurrentDictionary<int[], (double, int)> _transpositionTable = new();

    private const double Alpha = double.MinValue;
    private const double Beta = double.MaxValue;

    private const int MaxNegamaxTimeMs = 30_000;

    public bool IsWhite { get; init; } = isWhite;

    private static LinkedList.LinkedList<((int x, int y), (int x, int y))> MergeSort(Game game,
        LinkedList.LinkedList<((int x, int y), (int x, int y))> games)
    {
        if (games.Count <= 1)
        {
            return games;
        }

        (LinkedList.LinkedList<((int x, int y), (int x, int y))> left,
            LinkedList.LinkedList<((int x, int y), (int x, int y))> right) = games.SplitList(games.Count / 2);
        
        Task<LinkedList.LinkedList<((int x, int y), (int x, int y))>> leftTask = Task.Run(() => MergeSort(game, left));
        Task<LinkedList.LinkedList<((int x, int y), (int x, int y))>> rightTask = Task.Run(() => MergeSort(game, right));
        Task.WaitAll(leftTask, rightTask);
        
        return Merge(game, leftTask.Result, rightTask.Result);
    }

    private static LinkedList.LinkedList<((int x, int y), (int x, int y))> Merge(Game game,
        LinkedList.LinkedList<((int x, int y), (int x, int y))> left,
        LinkedList.LinkedList<((int x, int y), (int x, int y))> right)
    {
        LinkedList.LinkedList<((int x, int y), (int x, int y))> merged = new();
        int leftIndex = 0;
        int rightIndex = 0;
        
        Node<((int x, int y), (int x, int y))> leftNode = left.GetNode(0);
        Node<((int x, int y), (int x, int y))> rightNode = right.GetNode(0);
        
        while (leftIndex < left.Count && rightIndex < right.Count)
        {
            if (Evaluate(game, leftNode.Data) > Evaluate(game, rightNode.Data))
            {
                merged.AddNode(leftNode.Data);
                leftIndex++;
                leftNode = leftNode.NextNode!;
            }
            else
            {
                merged.AddNode(rightNode.Data);
                rightIndex++;
                rightNode = rightNode.NextNode!;
            }
        }
        
        while (leftIndex < left.Count)
        {
            merged.AddNode(leftNode.Data);
            leftIndex++;
            leftNode = leftNode.NextNode!;
        }
        
        while (rightIndex < right.Count)
        {
            merged.AddNode(rightNode.Data);
            rightIndex++;
            rightNode = rightNode.NextNode!;
        }
        
        return merged;
    }
    
    /// <summary>
    /// Returns the move that the bot determines to be the best
    /// </summary>
    /// <param name="game">The game at the position where the bot is to make the move</param>
    /// <returns>The move that the bot determines to be the best</returns>
    public Task<((int oldX, int oldY), (int newX, int newY), char? promotionPiece)> GetMove(Game game)
    {
        if (!_inOpeningBook || !OpeningsBook.ContainsKey((_openingMoves + " " + EncodeMove(game.LastMove)).Trim()))
        {
            _inOpeningBook = false;
            return FindBestMove(game, IsWhite ? 1 : -1, Environment.ProcessorCount);
        }
            
        
        _openingMoves = (_openingMoves + " " + EncodeMove(game.LastMove)).Trim();
        List<string> nextMoves = OpeningsBook[_openingMoves];
        
        if (nextMoves.Count == 0)
        {
            _inOpeningBook = false;
            return FindBestMove(game, IsWhite ? 1 : -1, Environment.ProcessorCount);
        }
        
        _openingMoves = nextMoves[new Random().Next(nextMoves.Count)];

        ((int oldX, int oldY), (int newX, int newY)) move =
            DecodeMove(_openingMoves[^4..]);
        return Task.FromResult((move.Item1, move.Item2, (char?)null));
    }
    
    private static string EncodeMove(((int oldX, int oldY), (int newX, int newY)) move)
    {
        return move.Equals(((-1, -1), (-1, -1))) ? "" :
            // +97 converts 1 to A, 2 to B, etc.
            $"{(char)(move.Item1.oldY + 97)}{move.Item1.oldX + 1}{(char)(move.Item2.newY + 97)}{move.Item2.newX + 1}";
    }
    
    private static ((int oldX, int oldY), (int newX, int newY)) DecodeMove(string move)
    {
        // -97 converts A to 1, B to 2, etc.
        // -48 converts a string number to an int (0 is 48 in Unicode), -49 to convert to 0-indexed
        return ((move[1] - 49, move[0] - 97), (move[3] - 49, move[2] - 97));
    }

    // https://www.chessprogramming.org/Iterative_Deepening
    // https://www.chessprogramming.org/Lazy_SMP
    
    /// <summary>
    /// Uses lazy SMP, iterative deepening, and negamax with alpha-beta pruning to find the best move.
    /// </summary>
    /// <param name="game">The current game</param>
    /// <param name="colour">1 if white, -1 if black</param>
    /// <param name="maxThreads">How many threads should be used</param>
    /// <returns>The move that the bot determines to be the best</returns>
    private Task<((int oldX, int oldY), (int newX, int newY), char? PromotionPiece)> FindBestMove(Game game, int colour, 
        int maxThreads)
    {
        DateTime startTime = DateTime.Now;
        List<Task> tasks = [];
        
        for (int threadId = 0; threadId < maxThreads; threadId++)
        {
            tasks.Add(Task.Run(() => SearchWorker(game, colour, startTime)));
        }

        Task.WaitAll(tasks.ToArray());

        ((int oldX, int oldY), (int newX, int newY)) bestMove;
        // Sort by score in descending order and get the best move (first item).
        (bestMove.Item1, bestMove.Item2, char? promotionPiece) =
            _moveVoteCount.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key;
        _moveVoteCount.Clear();
        
        return Task.FromResult((bestMove.Item1, bestMove.Item2, promotionPiece));
    }
    
    /// <summary>
    /// Subroutine that worker threads use to find the best move 
    /// </summary>
    /// <param name="game"></param>
    /// <param name="colour"></param>
    /// <param name="startTime"></param>
    private void SearchWorker(Game game, int colour, DateTime startTime)
    {
        // Deep copy
        Game localGame = game.Copy();
        double value = 0;

        // Iterative deepening search
        for (int depth = 1; depth < 100; depth++)
        {
            char? piece;
            if ((DateTime.Now - startTime).TotalMilliseconds > MaxNegamaxTimeMs) // TODO: add future times if needed
            {
                break;
            }

            ((int oldX, int oldY), (int newX, int newY)) bestMove;
            if (depth == 1) (value, bestMove, piece) = Negamax(localGame, depth, colour, Alpha, Beta, startTime);
            else (value, bestMove, piece) = Negamax(localGame, depth, colour, Alpha, Beta, startTime, value);

            // Update the vote count for the move
            if (bestMove != ((-1, -1), (-1, -1)))
            {
                _moveVoteCount.AddOrUpdate((bestMove.Item1, bestMove.Item2, piece), 1, (_, count) => count + 1);
            }
        }
    }
    
    private static bool IsCapture(Game game, (int newX, int newY) newPos)
    {
        return game.Board[newPos.newX, newPos.newY] != "";
    }
    
    private static bool IsQuiet(Game game, (int oldX, int oldY) oldPos, (int newX, int newY) newPos)
    {
        if (IsCapture(game, newPos)) return false;

        bool isQuiet = true;

        if (game.IsPromotingMove(oldPos, newPos))
        {
            Game copyGameQ = game.Copy();
            copyGameQ.MovePiece(oldPos, newPos, 'Q');
            isQuiet &= !IsAnyKingInCheck(copyGameQ);
            
            Game copyGameR = game.Copy();
            copyGameR.MovePiece(oldPos, newPos, 'R');
            isQuiet &= !IsAnyKingInCheck(copyGameR);
            
            Game copyGameB = game.Copy();
            copyGameB.MovePiece(oldPos, newPos, 'B');
            isQuiet &= !IsAnyKingInCheck(copyGameB);
            
            Game copyGameN = game.Copy();
            copyGameN.MovePiece(oldPos, newPos, 'N');
            isQuiet &= !IsAnyKingInCheck(copyGameN);

            return isQuiet;
        }

        Game copyGame = game.Copy();
        copyGame.MovePiece(oldPos, newPos);
        return !IsAnyKingInCheck(copyGame);
    }

    private static bool IsAnyKingInCheck(Game game)
    {
        if (game.IsKingInCheck()) return true;
        game.IsWhiteTurn = !game.IsWhiteTurn;
        bool isKingInCheck = game.IsKingInCheck();
        game.IsWhiteTurn = !game.IsWhiteTurn;
        return isKingInCheck;
    }

    private static bool IsEndgame(Game game)
    {
        // If total material value is less than 2400 centi-pawns
        int materialValue = 0;
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (game.Board[i, j] == "") continue;
                materialValue += game.Board[i, j][1] switch
                {
                    'P' => 100,
                    'N' => 320,
                    'B' => 330,
                    'R' => 500,
                    'Q' => 900,
                    'K' => 0,
                    _ => throw new ArgumentException("Invalid piece")
                };
            }
        }
        
        return materialValue < 2400;
    }
    
    private static (double, ((int oldX, int oldY), (int newX, int newY)), char?) QuiescenceSearch(Game game, int colour, 
        double alpha, double beta)
    {
        double standPat = colour * Evaluate(game);
    
        if (standPat >= beta)
        {
            return (beta, ((-1, -1), (-1, -1)), null);
        }
        if (alpha < standPat)
        {
            alpha = standPat;
        }
    
        LinkedList.LinkedList<((int x, int y), (int x, int y))> childGames = GetChildGames(game, colour);
        if (childGames.Head is null)
        {
            return (standPat, ((-1, -1), (-1, -1)), null);
        }
    
        ((int oldX, int oldY), (int newX, int newY)) move = ((-1, -1), (-1, -1));
    
        Node<((int x, int y), (int x, int y))>? currentNode = childGames.Head;
        while (currentNode != null)
        {
            if (IsQuiet(game, currentNode.Data.Item1, currentNode.Data.Item2))
            {
                currentNode = currentNode.NextNode;
                continue;
            }
    
            // Delta pruning
            const double safetyMargin = 200;
            if (standPat + GetPieceMaterialValue(game, currentNode.Data.Item2) + safetyMargin < alpha)
            {
                currentNode = currentNode.NextNode;
                continue;
            }
    
            if (game.IsPromotingMove(currentNode.Data.Item1, currentNode.Data.Item2))
            {
                Game copyGameQ = game.Copy();
                copyGameQ.Board[currentNode.Data.Item1.x, currentNode.Data.Item1.y] = 
                    copyGameQ.Board[currentNode.Data.Item1.x, currentNode.Data.Item1.y][0] + "Q";
                (alpha, beta, move) = QsAlphaBeta(copyGameQ, alpha, beta, colour, currentNode, move);
                if (alpha >= beta) return (beta, move, 'Q');
                
                Game copyGameR = game.Copy();
                copyGameR.Board[currentNode.Data.Item1.x, currentNode.Data.Item1.y] = 
                    copyGameR.Board[currentNode.Data.Item1.x, currentNode.Data.Item1.y][0] + "R1";
                (alpha, beta, move) = QsAlphaBeta(copyGameR, alpha, beta, colour, currentNode, move);
                if (alpha >= beta) return (beta, move, 'R');
                
                Game copyGameB = game.Copy();
                copyGameB.Board[currentNode.Data.Item1.x, currentNode.Data.Item1.y] = 
                    copyGameB.Board[currentNode.Data.Item1.x, currentNode.Data.Item1.y][0] + "B";
                (alpha, beta, move) = QsAlphaBeta(copyGameB, alpha, beta, colour, currentNode, move);
                if (alpha >= beta) return (beta, move, 'N');
                
                Game copyGameN = game.Copy();
                copyGameN.Board[currentNode.Data.Item1.x, currentNode.Data.Item1.y] = 
                    copyGameN.Board[currentNode.Data.Item1.x, currentNode.Data.Item1.y][0] + "N";
                (alpha, beta, move) = QsAlphaBeta(copyGameN, alpha, beta, colour, currentNode, move);
                if (alpha >= beta) return (beta, move, 'B');
            } else
            {
                (alpha, beta, move) = QsAlphaBeta(game, alpha, beta, colour, currentNode, move);
                if (alpha >= beta) return (beta, move, null);
            }
    
            currentNode = currentNode.NextNode;
        }
    
        return (alpha, move, null);
    }

    private static (double alpha, double beta, ((int oldX, int oldY), (int newX, int newY)) move) QsAlphaBeta(
        Game game, double alpha, double beta, int colour, Node<((int x, int y), (int x, int y))>? currentNode,
        ((int oldX, int oldY), (int newX, int newY)) move)
    {
        // Deep copy
        Game childGame = game.Copy();
        childGame.MovePiece(currentNode!.Data.Item1, currentNode.Data.Item2);
        double value = -QuiescenceSearch(childGame, -colour, -beta, -alpha).Item1;
        if (!(value > alpha)) return (alpha, beta, move);
        alpha = value;
        move = currentNode.Data;

        return (alpha, beta, move);
    }

    private static double GetPieceMaterialValue(Game game, (int x, int y) newPos)
    {
        try
        {
            return game.Board[newPos.x, newPos.y][1] switch
            {
                'P' => 100,
                'N' => 320,
                'B' => 330,
                'R' => 500,
                'Q' => 900,
                'K' => 0,
                _ => throw new ArgumentException("Invalid piece")
            };
        }
        catch (Exception _)
        {
            return 0;
        }
    }

    // TODO: Implement capture heuristics, killer move and history heuristics
    // https://www.duo.uio.no/bitstream/handle/10852/53769/master.pdf?sequence=1
    private (double, ((int oldX, int oldY), (int newX, int newY)), char?) Negamax(Game game, int depth, int colour,
        double alpha, double beta, DateTime startTime, double? windowCentre = null)
    {
        int[] hash = game.GetHash();
        if (_transpositionTable.TryGetValue(hash, out (double, int) entry) && entry.Item2 >= depth)
        {
            return (entry.Item1, ((-1, -1), (-1, -1)), null);
        }
        
        // Is terminal
        if (depth == 0 || (DateTime.Now - startTime).TotalMilliseconds > MaxNegamaxTimeMs)
        {
            return QuiescenceSearch(game, colour, alpha, beta);
        }
        if (game.IsFinished)
        {
            return (game.Score, ((-1, -1), (-1, -1)), null);
        }        
        if (!game.IsKingInCheck() && !_inEndgame)
        {
            Game nullMoveGame = game.Copy();
            nullMoveGame.MakeNullMove();
            double nullMoveValue = -Negamax(nullMoveGame, depth - 1, -colour, -beta, -beta + 1, startTime).Item1;
            if (nullMoveValue >= beta && depth >= 3)
            {
                return (nullMoveValue, ((-1, -1), (-1, -1)), null);
            }
        }

        double value = double.NegativeInfinity;
        LinkedList.LinkedList<((int x, int y), (int x, int y))> childGames = GetChildGames(game, colour);
        childGames = MergeSort(game, childGames);
        
        ((int oldX, int oldY), (int newX, int newY)) move = ((-1, -1), (-1, -1));

        Node<((int x, int y), (int x, int y))>? currentNode = childGames.Head;

        char? promotionPiece = null;
        while (currentNode != null)
        {
            int extension = game.Board[currentNode.Data.Item2.x, currentNode.Data.Item2.y] != "" ? 1 : 0;
            bool moveChanged;
            promotionPiece = null;

            if (game.IsPromotingMove((currentNode.Data.Item1.x, currentNode.Data.Item1.y),
                    (currentNode.Data.Item2.x, currentNode.Data.Item2.y)))
            {
                Game childGameQ = game.Copy();
                childGameQ.Board[currentNode.Data.Item1.x, currentNode.Data.Item1.y] =
                    childGameQ.Board[currentNode.Data.Item1.x, currentNode.Data.Item1.y][0] + "Q";
                childGameQ.MovePiece(currentNode.Data.Item1, currentNode.Data.Item2);
                (value, move, alpha, moveChanged) = NegamaxMain(childGameQ, depth, colour, alpha, beta, startTime,
                    windowCentre, currentNode, value, extension, move);
                if (moveChanged) promotionPiece = 'Q';
                if (alpha >= beta) break;
                
                Game childGameR = game.Copy();
                childGameR.Board[currentNode.Data.Item1.x, currentNode.Data.Item1.y] =
                    childGameR.Board[currentNode.Data.Item1.x, currentNode.Data.Item1.y][0] + "R1";
                childGameR.MovePiece(currentNode.Data.Item1, currentNode.Data.Item2);
                (value, move, alpha, moveChanged) = NegamaxMain(childGameR, depth, colour, alpha, beta, startTime,
                    windowCentre, currentNode, value, extension, move);
                if (moveChanged) promotionPiece = 'R';
                if (alpha >= beta) break;
                
                Game childGameB = game.Copy();
                childGameB.Board[currentNode.Data.Item1.x, currentNode.Data.Item1.y] =
                    childGameB.Board[currentNode.Data.Item1.x, currentNode.Data.Item1.y][0] + "B";
                childGameB.MovePiece(currentNode.Data.Item1, currentNode.Data.Item2);
                (value, move, alpha, moveChanged) = NegamaxMain(childGameB, depth, colour, alpha, beta, startTime,
                    windowCentre, currentNode, value, extension, move);
                if (moveChanged) promotionPiece = 'B';
                if (alpha >= beta) break;
                
                Game childGameN = game.Copy();
                childGameN.Board[currentNode.Data.Item1.x, currentNode.Data.Item1.y] =
                    childGameN.Board[currentNode.Data.Item1.x, currentNode.Data.Item1.y][0] + "N";
                childGameN.MovePiece(currentNode.Data.Item1, currentNode.Data.Item2);
                (value, move, alpha, moveChanged) = NegamaxMain(childGameN, depth, colour, alpha, beta, startTime,
                    windowCentre, currentNode, value, extension, move);
                if (moveChanged) promotionPiece = 'N';
                if (alpha >= beta) break;
            }
            else
            {
                Game childGame = game.Copy();
                childGame.MovePiece(currentNode.Data.Item1, currentNode.Data.Item2);
                (value, move, alpha, moveChanged) = NegamaxMain(childGame, depth, colour, alpha, beta, startTime,
                    windowCentre, currentNode, value, extension, move);
                if (moveChanged) promotionPiece = null;
                if (alpha >= beta) break;
            }
            
            currentNode = currentNode.NextNode;
        }

        _transpositionTable[hash] = (value, depth);
        return (value, move, promotionPiece);
    }

    private (double value, ((int oldX, int oldY), (int newX, int newY)) move, double alpha, bool moveChanged) 
        NegamaxMain(Game game, int depth, int colour, double alpha, double beta, DateTime startTime, 
            double? windowCentre, Node<((int x, int y), (int x, int y))> currentNode, double value, int extension,
            ((int oldX, int oldY), (int newX, int newY)) move)
    {
        bool moveChanged = false;
        const double aspirationWindow = 250;
        double windowAlpha = windowCentre.HasValue ? windowCentre.Value - aspirationWindow : alpha;
        double windowBeta = windowCentre.HasValue ? windowCentre.Value + aspirationWindow : beta;
            
        double value2 = -Negamax(game, depth - 1 + extension, -colour, -windowBeta, -windowAlpha, startTime)
            .Item1;
            
        if (value2 <= windowAlpha || value2 >= beta)
        {
            value2 = -Negamax(game, depth - 1 + extension, -colour, -beta, -alpha, startTime).Item1;
        }
            
        if (value2 > value)
        {
            value = value2;
            move = currentNode.Data;
            moveChanged = true;
        }
        alpha = Math.Max(alpha, value);
        
        return (value, move, alpha, moveChanged);
    }

    private static double Evaluate(Game game)
    {
        if (game.IsFinished)
        {
            return game.Score;
        }

        int score = 0;
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (game.Board[i, j] == "") continue;
                int pieceValue = game.Board[i, j][1] switch
                {
                    'P' => 100,
                    'N' => 320,
                    'B' => 330,
                    'R' => 500,
                    'Q' => 900,
                    'K' => 0,
                    _ => throw new ArgumentException("Invalid piece")
                };
                score += pieceValue * (game.Board[i, j][0] == 'w' ? 1 : -1);
                score += GetPieceSquareValue(game.Board[i, j], i, j);
            }
        }

        return score;
    }
    
    private static double Evaluate(Game game, ((int oldX, int oldY), (int newX, int newY)) move)
    {
        if (!game.IsPromotingMove(move.Item1, move.Item2))
        {
            Game childGame = game.Copy();
            childGame.MovePiece(move.Item1, move.Item2);
            return Evaluate(childGame);
        }

        double evaluation = double.NegativeInfinity;
        
        Game childGameQ = game.Copy();
        childGameQ.Board[move.Item1.oldX, move.Item1.oldY] =
            childGameQ.Board[move.Item1.oldX, move.Item1.oldY][0] + "Q";
        childGameQ.MovePiece(move.Item1, move.Item2);
        evaluation = Math.Max(Evaluate(childGameQ), evaluation);
        
        Game childGameR = game.Copy();
        childGameR.Board[move.Item1.oldX, move.Item1.oldY] =
            childGameR.Board[move.Item1.oldX, move.Item1.oldY][0] + "R1";
        childGameR.MovePiece(move.Item1, move.Item2);
        evaluation = Math.Max(Evaluate(childGameR), evaluation);
        
        Game childGameB = game.Copy();
        childGameB.Board[move.Item1.oldX, move.Item1.oldY] =
            childGameB.Board[move.Item1.oldX, move.Item1.oldY][0] + "B";
        childGameB.MovePiece(move.Item1, move.Item2);
        evaluation = Math.Max(Evaluate(childGameQ), evaluation);
        
        Game childGameN = game.Copy();
        childGameN.Board[move.Item1.oldX, move.Item1.oldY] =
            childGameN.Board[move.Item1.oldX, move.Item1.oldY][0] + "N";
        childGameN.MovePiece(move.Item1, move.Item2);
        evaluation = Math.Max(Evaluate(childGameQ), evaluation);

        return evaluation;
    }
    
    private static int GetPieceSquareValue(string piece, int x, int y)
    {
        // Equivalent to flipping the board for black pieces
        if (piece[0] == 'w')
        {
            x = 7 - x;
            y = 7 - y;
        }

        int value = piece[1] switch
        {
            'P' => PawnTable[x, y],
            'N' => KnightTable[x, y],
            'B' => BishopTable[x, y],
            'R' => RookTable[x, y],
            'Q' => QueenTable[x, y],
            'K' => KingTable[x, y],
            _ => 0
        };

        return piece[0] == 'w' ? value : -value;
    }
    
    private static LinkedList.LinkedList<((int x, int y), (int x, int y))> GetChildGames(Game game, int colour)
    {
        LinkedList.LinkedList<((int x, int y), (int x, int y))> childGames = new();
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (game.Board[i, j] == "") continue;
                if (game.Board[i, j][0] != (colour == 1 ? 'w' : 'b')) continue;

                LinkedList.LinkedList<(int x, int y)>? moves = game.GetMoves((i, j));
                if (moves is null) continue;

                Node<(int x, int y)>? currentMove = moves.Head;
                while (currentMove is not null)
                {
                    childGames.AddNode(((i, j), currentMove.Data));
                    currentMove = currentMove.NextNode;
                }
            }
        }
        return childGames;
    }
}