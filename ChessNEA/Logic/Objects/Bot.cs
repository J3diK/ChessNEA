using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChessNEA.Logic.Objects.LinkedList;

namespace ChessNEA.Logic.Objects;

// https://www.chessprogramming.org/Simplified_Evaluation_Function
public class Bot(int maxDepthPly, bool isWhite = false)
{
    private const int Alpha = -(int)Constants.Infinity;
    private const int Beta = (int)Constants.Infinity;

    private static readonly int[,] PawnTable =
    {
        { 0, 0, 0, 0, 0, 0, 0, 0 },
        { 50, 50, 50, 50, 50, 50, 50, 50 },
        { 10, 10, 20, 30, 30, 20, 10, 10 },
        { 5, 5, 10, 25, 25, 10, 5, 5 },
        { 0, 0, 0, 20, 20, 0, 0, 0 },
        { 5, -5, -10, 0, 0, -10, -5, 5 },
        { 5, 10, 10, -20, -20, 10, 10, 5 },
        { 0, 0, 0, 0, 0, 0, 0, 0 }
    };

    private static readonly int[,] KnightTable =
    {
        { -50, -40, -30, -30, -30, -30, -40, -50 },
        { -40, -20, 0, 0, 0, 0, -20, -40 },
        { -30, 0, 10, 15, 15, 10, 0, -30 },
        { -30, 5, 15, 20, 20, 15, 5, -30 },
        { -30, 0, 15, 20, 20, 15, 0, -30 },
        { -30, 5, 10, 15, 15, 10, 5, -30 },
        { -40, -20, 0, 5, 5, 0, -20, -40 },
        { -50, -40, -30, -30, -30, -30, -40, -50 }
    };

    private static readonly int[,] BishopTable =
    {
        { -20, -10, -10, -10, -10, -10, -10, -20 },
        { -10, 0, 0, 0, 0, 0, 0, -10 },
        { -10, 0, 5, 10, 10, 5, 0, -10 },
        { -10, 5, 5, 10, 10, 5, 5, -10 },
        { -10, 0, 10, 10, 10, 10, 0, -10 },
        { -10, 10, 10, 10, 10, 10, 10, -10 },
        { -10, 5, 0, 0, 0, 0, 5, -10 },
        { -20, -10, -10, -10, -10, -10, -10, -20 }
    };

    private static readonly int[,] RookTable =
    {
        { 0, 0, 0, 0, 0, 0, 0, 0 },
        { 5, 10, 10, 10, 10, 10, 10, 5 },
        { -5, 0, 0, 0, 0, 0, 0, -5 },
        { -5, 0, 0, 0, 0, 0, 0, -5 },
        { -5, 0, 0, 0, 0, 0, 0, -5 },
        { -5, 0, 0, 0, 0, 0, 0, -5 },
        { -5, 0, 0, 0, 0, 0, 0, -5 },
        { 0, 0, 0, 5, 5, 0, 0, 0 }
    };

    private static readonly int[,] QueenTable =
    {
        { -20, -10, -10, -5, -5, -10, -10, -20 },
        { -10, 0, 0, 0, 0, 0, 0, -10 },
        { -10, 0, 5, 5, 5, 5, 0, -10 },
        { -5, 0, 5, 5, 5, 5, 0, -5 },
        { 0, 0, 5, 5, 5, 5, 0, -5 },
        { -10, 5, 5, 5, 5, 5, 0, -10 },
        { -10, 0, 5, 0, 0, 0, 0, -10 },
        { -20, -10, -10, -5, -5, -10, -10, -20 }
    };

    private static readonly int[,] KingTable =
    {
        { -30, -40, -40, -50, -50, -40, -40, -30 },
        { -30, -40, -40, -50, -50, -40, -40, -30 },
        { -30, -40, -40, -50, -50, -40, -40, -30 },
        { -30, -40, -40, -50, -50, -40, -40, -30 },
        { -20, -30, -30, -40, -40, -30, -30, -20 },
        { -10, -20, -20, -20, -20, -20, -20, -10 },
        { 20, 20, 0, 0, 0, 0, 20, 20 },
        { 20, 30, 10, 0, 0, 10, 30, 20 }
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
        { "g1f3 g8f6", [] }, // Réti Opening
        // 2. White
        { "e2e4 c7c5 g1f3", ["e2e4 c7c5 g1f3 d7d6", "e2e4 c7c5 g1f3 b8c6", "e2e4 c7c5 g1f3 e7e6"] }, // Sicilian Defense
        { "e2e4 e7e5 g1f3", ["e2e4 e7e5 g1f3 b8c6"] }, // King's Pawn Opening: King's Knight Variation
        { "e2e4 e7e6 d2d4", ["e2e4 e7e6 d2d4 d7d5"] }, // French Defense: Normal Variation
        { "d2d4 g8f6 c2c4", ["d2d4 g8f6 c2c4 e7e6", "d2d4 g8f6 c2c4 g7g6"] }, // Indian Game
        { "d2d4 g8f6 g1f3", [] }, // Indian Game: Knights Variation
        { "d2d4 d7d5 c2c4", [] }, // Queen's Gambit
        // 2. Black
        { "e2e4 c7c5 g1f3 d7d6", ["e2e4 c7c5 g1f3 d7d6 d2d4"] }, // Sicilian Defense
        { "e2e4 c7c5 g1f3 b8c6", [] }, // Sicilian Defense: Old Sicilian Variation
        { "e2e4 c7c5 g1f3 e7e6", [] }, // Sicilian Defense: French Variation
        { "e2e4 e7e5 g1f3 b8c6", ["e2e4 e7e5 g1f3 b8c6 f1b5"] }, // King's Pawn Opening: King's Knight Variation
        { "e2e4 e7e6 d2d4 d7d5", [] }, // 
        {
            "d2d4 g8f6 c2c4 e7e6", ["d2d4 g8f6 c2c4 e7e6 g1f3"]
        }, // Indian Game: East Indian, Anti-Nimzo-Indian Variation
        { "d2d4 g8f6 c2c4 g7g6", ["d2d4 g8f6 c2c4 g7g6 b1c3"] }, // King's Indian Defense
        // 3. White
        { "e2e4 c7c5 g1f3 d7d6 d2d4", ["e2e4 c7c5 g1f3 d7d6 d2d4 c5d4"] }, // Sicilian Defense
        {
            "e2e4 e7e5 g1f3 b8c6 f1b5", ["e2e4 e7e5 g1f3 b8c6 f1b5 a7a6"]
        }, // King's Pawn Opening: King's Knight Variation
        { "d2d4 g8f6 c2c4 e7e6 g1f3", [] }, // Indian Game
        { "d2d4 g8f6 c2c4 g7g6 b1c3", [] }, // King's Indian Defense
        // 3. Black
        { "e2e4 c7c5 g1f3 d7d6 d2d4 c5d4", ["e2e4 c7c5 g1f3 d7d6 d2d4 c5d4 f3d4"] }, // Sicilian Defense
        { "e2e4 e7e5 g1f3 b8c6 f1b5 a7a6", [] }, // King's Pawn Opening: King's Knight Variation: Normal Variation
        // 4. White // Sicilian Defense: Open Variation
        { "e2e4 c7c5 g1f3 d7d6 d2d4 c5d4 f3d4", ["e2e4 c7c5 g1f3 d7d6 d2d4 c5d4 f3d4 g8f6"] },
        // 4. Black // Sicilian Defense: Open Variation
        { "e2e4 c7c5 g1f3 d7d6 d2d4 c5d4 f3d4 g8f6", ["e2e4 c7c5 g1f3 d7d6 d2d4 c5d4 f3d4 g8f6 b1c3"] },
        // 5. White // Sicilian Defense: Open Variation
        { "e2e4 c7c5 g1f3 d7d6 d2d4 c5d4 f3d4 g8f6 b1c3", ["e2e4 c7c5 g1f3 d7d6 d2d4 c5d4 f3d4 g8f6 b1c3 a7a6"] },
        // 5. Black // Sicilian Defense: Open Variation: Najdorf Variation
        { "e2e4 c7c5 g1f3 d7d6 d2d4 c5d4 f3d4 g8f6 b1c3 a7a6", [] }
    };

    private readonly ConcurrentDictionary<((int oldX, int oldY), (int newX, int newY), char?), int> _moveVoteCount =
        new();

    private bool _inEndgame;
    private bool _inOpeningBook = true;

    private string _openingMoves = "";

    // Avoid redundant calculations
    private ConcurrentDictionary<int[], (int, int, ((int oldX, int oldY), (int newX, int newY)))>
        _transpositionTable = new(new IntArrayComparer());

    private bool IsWhite { get; } = isWhite;

    private LinkedList.LinkedList<((int x, int y), (int x, int y))> MergeSort(Game game, int colour,
        LinkedList.LinkedList<((int x, int y), (int x, int y))> games)
    {
        if (games.Count <= 1) return games;

        (LinkedList.LinkedList<((int x, int y), (int x, int y))> left,
            LinkedList.LinkedList<((int x, int y), (int x, int y))> right) = games.SplitList(games.Count / 2);

        Task<LinkedList.LinkedList<((int x, int y), (int x, int y))>> leftTask =
            Task.Run(() => MergeSort(game, colour, left));
        Task<LinkedList.LinkedList<((int x, int y), (int x, int y))>> rightTask =
            Task.Run(() => MergeSort(game, colour, right));
        Task.WaitAll(leftTask, rightTask);

        return Merge(game, colour, leftTask.Result, rightTask.Result);
    }
    
    private LinkedList.LinkedList<((int x, int y), (int x, int y))> Merge(Game game, int colour,
        LinkedList.LinkedList<((int x, int y), (int x, int y))> left,
        LinkedList.LinkedList<((int x, int y), (int x, int y))> right)
    {
        LinkedList.LinkedList<((int x, int y), (int x, int y))> merged = new();
        int leftIndex = 0;
        int rightIndex = 0;

        Node<((int x, int y), (int x, int y))> leftNode = left.GetNode(0);
        Node<((int x, int y), (int x, int y))> rightNode = right.GetNode(0);

        while (leftIndex < left.Count && rightIndex < right.Count)
            if (Evaluate(game, leftNode.Data) * colour > Evaluate(game, rightNode.Data) * colour)
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
    ///     Returns the move that the bot determines to be the best
    /// </summary>
    /// <param name="game">The game at the position where the bot is to make the move</param>
    /// <returns>The move that the bot determines to be the best</returns>
    public Task<((int oldX, int oldY), (int newX, int newY), char? promotionPiece)> GetMove(Game game)
    {
        if (!_inOpeningBook || !OpeningsBook.ContainsKey((_openingMoves + " " + EncodeMove(game.LastMove)).Trim()))
        {
            if (!_inEndgame) _inEndgame = IsEndgame(game);
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
        return move.Equals(((-1, -1), (-1, -1)))
            ? ""
            :
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
    ///     Uses lazy SMP, iterative deepening, and negamax with alpha-beta pruning to find the best move.
    /// </summary>
    /// <param name="game">The current game</param>
    /// <param name="colour">1 if white, -1 if black</param>
    /// <param name="maxThreads">How many threads should be used</param>
    /// <returns>The move that the bot determines to be the best</returns>
    private Task<((int oldX, int oldY), (int newX, int newY), char? PromotionPiece)> FindBestMove(Game game, int colour,
        int maxThreads)
    {
        List<Task> tasks = [];

        for (int threadId = 0; threadId < maxThreads; threadId++)
        {
            tasks.Add(Task.Run(() => SearchWorker(game, colour)));
        }

        Task.WaitAll(tasks.ToArray());

        ((int oldX, int oldY), (int newX, int newY)) bestMove;
        // Sort by score in descending order and get the best move (first item).
        (bestMove.Item1, bestMove.Item2, char? promotionPiece) =
            _moveVoteCount.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key;
        _moveVoteCount.Clear();

        _transpositionTable =
            new ConcurrentDictionary<int[], (int, int, ((int oldX, int oldY), (int newX, int newY)))>(
                new IntArrayComparer());
        return Task.FromResult((bestMove.Item1, bestMove.Item2, promotionPiece));
    }

    /// <summary>
    ///     Subroutine that worker threads use to find the best move
    /// </summary>
    /// <param name="game"></param>
    /// <param name="colour"></param>
    private void SearchWorker(Game game, int colour)
    {
        // Deep copy
        Game localGame = game.Copy();
        int value = 0;

        // Iterative deepening search
        for (int depth = 1; depth <= maxDepthPly; depth++)
        {
            (value, ((int oldX, int oldY), (int newX, int newY)) bestMove, char? piece) = depth == 1
                ? Negamax(localGame, depth, colour, Alpha, Beta)
                : Negamax(localGame, depth, colour, Alpha, Beta, value);
            
            // Update the vote count for the move
            if (bestMove == ((-1, -1), (-1, -1))) continue;
            
            int depthLocal = depth;
            _moveVoteCount.AddOrUpdate((bestMove.Item1, bestMove.Item2, piece), depth,
                (_, count) => count + depthLocal);
        }
    }

    private static bool IsCapture(Game game, (int newX, int newY) newPos)
    {
        return game.Board[newPos.newX, newPos.newY] != "";
    }

    // private static bool AnyNextMoveIsCapture(Game game, int colour)
    // {
    //     LinkedList.LinkedList<((int x, int y), (int x, int y))> childGames =
    //         GetChildGames(game, colour);
    //     
    //     Node<((int x, int y), (int x, int y))>? currentNode = childGames.Head;
    //     while (currentNode != null)
    //     {
    //         if (IsCapture(game, currentNode.Data.Item2)) return true;
    //         currentNode = currentNode.NextNode;
    //     }
    //     
    //     return false;
    // }

    private static bool IsQuiet(Game game, (int oldX, int oldY) oldPos, (int newX, int newY) newPos)
    {
        if (IsCapture(game, newPos)) return false;
        if (game.IsPromotingMove(oldPos, newPos)) return false;

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

        return materialValue < 2400;
    }

    private int QuiescenceSearch(Game game, int colour, int alpha, int beta)
    {
        int standPat = colour * Evaluate(game);

        if (standPat >= beta) return beta;
        if (alpha < standPat) alpha = standPat;

        LinkedList.LinkedList<((int x, int y), (int x, int y))> childGames = GetChildGames(game, colour);
        if (childGames.Head is null) return standPat;

        Node<((int x, int y), (int x, int y))>? currentNode = childGames.Head;
        while (currentNode != null)
        {
            if (IsQuiet(game, currentNode.Data.Item1, currentNode.Data.Item2))
            {
                currentNode = currentNode.NextNode;
                continue;
            }

            // Delta pruning
            const int safetyMargin = 200;
            if (standPat + GetPieceMaterialValue(game, currentNode.Data.Item2) + safetyMargin < alpha)
            {
                currentNode = currentNode.NextNode;
                continue;
            }

            if (game.IsPromotingMove(currentNode.Data.Item1, currentNode.Data.Item2))
            {
                foreach (string promotion in (string[]) ["Q", "R1", "B", "N"])
                {
                    Game copyGame = game.Copy();
                    copyGame.Board[currentNode.Data.Item1.x, currentNode.Data.Item1.y] =
                        copyGame.Board[currentNode.Data.Item1.x, currentNode.Data.Item1.y][0] + promotion;
                    alpha = QsAlphaBeta(copyGame, alpha, beta, colour, currentNode);
                    if (alpha >= beta) return beta;
                }
            }
            else
            {
                alpha = QsAlphaBeta(game, alpha, beta, colour, currentNode);
                if (alpha >= beta) return beta;
            }

            currentNode = currentNode.NextNode;
        }

        return alpha;
    }

    private int QsAlphaBeta(Game game, int alpha, int beta, int colour,
        Node<((int x, int y), (int x, int y))>? currentNode)
    {
        // Deep copy
        Game childGame = game.Copy();
        childGame.MovePiece(currentNode!.Data.Item1, currentNode.Data.Item2);
        int value = -QuiescenceSearch(childGame, -colour, -beta, -alpha);
        return value >= beta ? beta : Math.Max(alpha, value);
    }

    private static int GetPieceMaterialValue(Game game, (int x, int y) newPos)
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
        catch (IndexOutOfRangeException)
        {
            return 0;
        }
    }

    // TODO: Implement capture heuristics, killer move and history heuristics
    // https://www.duo.uio.no/bitstream/handle/10852/53769/master.pdf?sequence=1
    private (int, ((int oldX, int oldY), (int newX, int newY)), char?) Negamax(Game game, int depth, int colour,
        int alpha, int beta, int? windowCentre = null)
    {
        int[] hash = game.GetHash();
        if (_transpositionTable.TryGetValue(hash,
                out (int, int, ((int oldX, int oldY), (int newX, int newY))) entry) && entry.Item2 > depth)
            return (entry.Item1, entry.Item3, null);

        // Is terminal
        if (depth == 0)
        {
            int result = QuiescenceSearch(game, colour, alpha, beta);
            return (result, ((-1, -1), (-1, -1)), null);
        }

        if (game.IsFinished) return (game.Score, ((-1, -1), (-1, -1)), null);

        // depth >= 3 ensures (depth - 3) > 0. -3 is used as the fixed reduction value.
        if (!game.IsKingInCheck() && !_inEndgame && depth >= 3)
        {
            Game nullMoveGame = game.Copy();
            nullMoveGame.MakeNullMove();
            int nullMoveValue = -Negamax(nullMoveGame, depth - 3, -colour, -beta, -beta + 1).Item1;
            if (nullMoveValue >= beta) return (nullMoveValue, ((-1, -1), (-1, -1)), null);
        }

        int value = -(int)Constants.Infinity;
        LinkedList.LinkedList<((int x, int y), (int x, int y))> childGames = GetChildGames(game, colour);
        childGames = MergeSort(game, colour, childGames);

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
                foreach (string promotion in (string[]) ["Q", "R1", "B", "N"])
                {
                    Game childGame = game.Copy();
                    childGame.Board[currentNode.Data.Item1.x, currentNode.Data.Item1.y] =
                        childGame.Board[currentNode.Data.Item1.x, currentNode.Data.Item1.y][0] + promotion;
                    childGame.MovePiece(currentNode.Data.Item1, currentNode.Data.Item2);
                    (value, move, alpha, moveChanged) = NegamaxMain(childGame, depth, colour, alpha, beta,
                        windowCentre, currentNode, value, extension, move);
                    if (moveChanged) promotionPiece = promotion[0];
                    if (alpha >= beta) break;
                }
            }
            else
            {
                Game childGame = game.Copy();
                childGame.MovePiece(currentNode.Data.Item1, currentNode.Data.Item2);
                (value, move, alpha, moveChanged) = NegamaxMain(childGame, depth, colour, alpha, beta,
                    windowCentre, currentNode, value, extension, move);
                if (moveChanged) promotionPiece = null;
                if (alpha >= beta) break;
            }

            currentNode = currentNode.NextNode;
        }

        if (!move.Equals(((-1, -1), (-1, -1)))) _transpositionTable[hash] = (value, depth, move);


        return alpha == -(int)Constants.Infinity
            ? (-(int)Constants.Infinity, childGames.Head!.Data,
                game.IsPromotingMove(childGames.Head!.Data.Item1, childGames.Head!.Data.Item2) ? 'Q' : null)
            : (value, move, promotionPiece);
    }

    private (int value, ((int oldX, int oldY), (int newX, int newY)) move, int alpha, bool moveChanged)
        NegamaxMain(Game game, int depth, int colour, int alpha, int beta,
            int? windowCentre, Node<((int x, int y), (int x, int y))> currentNode, int value, int extension,
            ((int oldX, int oldY), (int newX, int newY)) move)
    {
        bool moveChanged = false;
        const int aspirationWindow = 250;
        int windowAlpha = windowCentre.HasValue ? windowCentre.Value - aspirationWindow : alpha;
        int windowBeta = windowCentre.HasValue ? windowCentre.Value + aspirationWindow : beta;

        int value2 = -Negamax(game, depth - 1 + extension, -colour, -windowBeta, -windowAlpha).Item1;

        if (value2 < windowAlpha || value2 > windowBeta)
            value2 = -Negamax(game, depth - 1 + extension, -colour, -beta, -alpha).Item1;

        if (value2 > value)
        {
            value = value2;
            move = currentNode.Data;
            moveChanged = true;
        }

        alpha = Math.Max(alpha, value);

        return (value, move, alpha, moveChanged);
    }

    private int Evaluate(Game game)
    {
        if (game.IsFinished) return game.Score;

        int score = 0;
        for (int i = 0; i < 8; i++)
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

        return score;
    }

    private int Evaluate(Game game, ((int oldX, int oldY), (int newX, int newY)) move)
    {
        if (!game.IsPromotingMove(move.Item1, move.Item2))
        {
            Game childGame = game.Copy();
            childGame.MovePiece(move.Item1, move.Item2);
            return Evaluate(childGame);
        }

        int evaluation = -(int)Constants.Infinity;

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

    private int GetPieceSquareValue(string piece, int x, int y)
    {
        // Equivalent to flipping the board for black pieces
        if (piece[0] == 'w') x = 7 - x;

        int value = piece[1] switch
        {
            'P' => PawnTable[x, y],
            'N' => KnightTable[x, y],
            'B' => BishopTable[x, y],
            'R' => RookTable[x, y],
            'Q' => QueenTable[x, y],
            'K' => !_inEndgame ? KingTable[x, y] : KingEndgameTable[x, y],
            _ => 0
        };

        return piece[0] == 'w' ? value : -value;
    }

    private static LinkedList.LinkedList<((int x, int y), (int x, int y))> GetChildGames(Game game, int colour)
    {
        LinkedList.LinkedList<((int x, int y), (int x, int y))> childGames = new();
        for (int i = 0; i < 8; i++)
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

        return childGames;
    }
}