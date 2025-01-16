using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChessNEA.Logic.Objects.LinkedList;

namespace ChessNEA.Logic.Objects;

// https://www.chessprogramming.org/Simplified_Evaluation_Function
public class Bot
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
    
    private static readonly ConcurrentDictionary<((int oldX, int oldY), (int newX, int newY)), int> MoveVoteCount =
        new();
    // Avoid redundant calculations
    private static readonly ConcurrentDictionary<int[], (double, int)> TranspositionTable = new();

    private const double Alpha = double.MinValue;
    private const double Beta = double.MaxValue;
    
    public bool IsWhite;

    private const int MaxNegamaxTimeMs = 30_000;

    // private static List<((int x, int y), (int x, int y))> MergeSort(Game game, List<((int x, int y), (int x, int y))> games)
    // {
    //     if (games.Count <= 1)
    //     {
    //         return games;
    //     }
    //     
    //     List<((int x, int y), (int x, int y))> left = games.GetRange(0, games.Count / 2);
    //     List<((int x, int y), (int x, int y))> right = games.GetRange(games.Count / 2, games.Count - games.Count / 2);
    //     
    //     Task<List<((int x, int y), (int x, int y))>> leftTask = Task.Run(() => MergeSort(game, left));
    //     Task<List<((int x, int y), (int x, int y))>> rightTask = Task.Run(() => MergeSort(game, right));
    //     Task.WaitAll(leftTask, rightTask);
    //     
    //     return Merge(game, leftTask.Result, rightTask.Result);
    // }

    // private static List<((int x, int y), (int x, int y))> Merge(Game game, List<((int x, int y), (int x, int y))> left, List<((int x, int y), (int x, int y))> right)
    // {
    //     List<((int x, int y), (int x, int y))> merged = [];
    //     int leftIndex = 0;
    //     int rightIndex = 0;
    //     
    //     while (leftIndex < left.Count && rightIndex < right.Count)
    //     {
    //         if (Evaluate(game, left[leftIndex]) > Evaluate(game, right[rightIndex]))
    //         {
    //             merged.Add(left[leftIndex]);
    //             leftIndex++;
    //         }
    //         else
    //         {
    //             merged.Add(right[rightIndex]);
    //             rightIndex++;
    //         }
    //     }
    //     
    //     while (leftIndex < left.Count)
    //     {
    //         merged.Add(left[leftIndex]);
    //         leftIndex++;
    //     }
    //     
    //     while (rightIndex < right.Count)
    //     {
    //         merged.Add(right[rightIndex]);
    //         rightIndex++;
    //     }
    //     
    //     return merged;
    // }
    
    /// <summary>
    /// Returns the move that the bot determines to be the best
    /// </summary>
    /// <param name="game">The game at the position where the bot is to make the move</param>
    /// <returns>The move that the bot determines to be the best</returns>
    public Task<((int oldX, int oldY), (int newX, int newY))> GetMove(Game game)
    {
        return FindBestMove(game, IsWhite ? 1 : -1, Environment.ProcessorCount);
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
    private static Task<((int oldX, int oldY), (int newX, int newY))> FindBestMove(Game game, int colour, int maxThreads)
    {
        DateTime startTime = DateTime.Now;
        List<Task> tasks = [];
        
        for (int threadId = 0; threadId < maxThreads; threadId++)
        {
            tasks.Add(Task.Run(() => SearchWorker(game, colour, startTime)));
        }

        Task.WaitAll(tasks.ToArray());

        // Sort by score in descending order and get the best move (first item).
        ((int oldX, int oldY), (int newX, int newY)) bestMove =
            MoveVoteCount.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key;
        MoveVoteCount.Clear();
        
        return Task.FromResult(bestMove);
    }
    
    /// <summary>
    /// Subroutine that worker threads use to find the best move 
    /// </summary>
    /// <param name="game"></param>
    /// <param name="colour"></param>
    /// <param name="startTime"></param>
    private static void SearchWorker(Game game, int colour, DateTime startTime)
    {
        // Deep copy
        Game localGame = game.Copy();
        double value = 0;

        // Iterative deepening search
        for (int depth = 1; depth < 100; depth++)
        {
            if ((DateTime.Now - startTime).TotalMilliseconds > MaxNegamaxTimeMs) // TODO: add future times if needed
            {
                break;
            }

            ((int oldX, int oldY), (int newX, int newY)) bestMove;
            if (depth == 1) (value, bestMove) = Negamax(localGame, depth, colour, Alpha, Beta, startTime);
            else (value, bestMove) = Negamax(localGame, depth, colour, Alpha, Beta, startTime, value);

            // Update the vote count for the move
            if (bestMove != ((-1, -1), (-1, -1)))
            {
                MoveVoteCount.AddOrUpdate(bestMove, 1, (_, count) => count + 1);
            }
        }
    }
    
    private static bool IsCapture(Game game, (int newX, int newY) newPos)
    {
        return game.Board[newPos.newX, newPos.newY] != "";
    }

    private static (double, ((int oldX, int oldY), (int newX, int newY))) QuiescenceSearch(Game game, int colour, 
        double alpha, double beta)
    {
        // TODO: Zugzwangs in pawn endgames
        double standPat = colour * Evaluate(game);
        double value = standPat;
        
        if (value >= beta)
        {
            return (beta, ((-1, -1), (-1, -1)));
        }
        if (alpha < value)
        {
            alpha = value;
        }

        LinkedList.LinkedList<((int x, int y), (int x, int y))> childGames = GetChildGames(game, colour);
        if (childGames.Head is null)
        {
            return (standPat, ((-1, -1), (-1, -1)));
        }

        ((int oldX, int oldY), (int newX, int newY)) move = ((-1, -1), (-1, -1));

        Node<((int x, int y), (int x, int y))>? currentNode = childGames.Head;
        while (currentNode != null)
        {
            if (!IsCapture(game, currentNode.Data.Item2))
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
            
            // Deep copy
            Game childGame = game.Copy();
            childGame.MovePiece(currentNode.Data.Item1, currentNode.Data.Item2);
            value = -QuiescenceSearch(childGame, -colour, -beta, -alpha).Item1;
            if (value > alpha)
            {
                alpha = value;
                move = currentNode.Data;
            }
            if (alpha >= beta) return (beta, move);
            currentNode = currentNode.NextNode;
        }

        return (alpha, move);
    }
    
    private static double GetPieceMaterialValue(Game game, (int x, int y) newPos)
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
    
    // TODO: Implement capture heuristics, killer move and history heuristics
    // https://www.duo.uio.no/bitstream/handle/10852/53769/master.pdf?sequence=1
    private static (double, ((int oldX, int oldY), (int newX, int newY))) Negamax(Game game, int depth, int colour,
        double alpha, double beta, DateTime startTime, double? windowCentre = null)
    {
        int[] hash = game.GetHash();
        if (TranspositionTable.TryGetValue(hash, out (double, int) entry) && entry.Item2 >= depth)
        {
            return (entry.Item1, ((-1, -1), (-1, -1)));
        }
        
        // Is terminal
        if (depth == 0 || (DateTime.Now - startTime).TotalMilliseconds > MaxNegamaxTimeMs)
        {
            return QuiescenceSearch(game, colour, alpha, beta);
        }
        if (game.IsFinished)
        {
            return (game.Score, ((-1, -1), (-1, -1)));
        }        
        // Null move pruning TODO: Zugzwangs in pawn endgames
        if (!game.IsKingInCheck())
        {
            Game nullMoveGame = game.Copy();
            nullMoveGame.MakeNullMove();
            double nullMoveValue = -Negamax(nullMoveGame, depth - 1, -colour, -beta, -beta + 1, startTime).Item1;
            if (nullMoveValue >= beta && depth >= 3)
            {
                return (nullMoveValue, ((-1, -1), (-1, -1)));
            }
        }

        double value = double.NegativeInfinity;
        LinkedList.LinkedList<((int x, int y), (int x, int y))> childGames = GetChildGames(game, colour);
        
        ((int oldX, int oldY), (int newX, int newY)) move = ((-1, -1), (-1, -1));

        Node<((int x, int y), (int x, int y))>? currentNode = childGames.Head;
        while (currentNode != null)
        {
            int extension = game.Board[currentNode.Data.Item2.x, currentNode.Data.Item2.y] != "" ? 1 : 0;
            
            // Deep copy
            Game childGame = game.Copy();
            childGame.MovePiece(currentNode.Data.Item1, currentNode.Data.Item2);
            
            const double aspirationWindow = 250;
            double windowAlpha = windowCentre.HasValue ? windowCentre.Value - aspirationWindow : alpha;
            double windowBeta = windowCentre.HasValue ? windowCentre.Value + aspirationWindow : beta;
            
            double value2 = -Negamax(childGame, depth - 1 + extension, -colour, -windowBeta, -windowAlpha, startTime)
                .Item1;
            
            if (value2 <= windowAlpha || value2 >= beta)
            {
                value2 = -Negamax(childGame, depth - 1 + extension, -colour, -beta, -alpha, startTime).Item1;
            }
            
            if (value2 > value)
            {
                value = value2;
                move = currentNode.Data;
            }
            alpha = Math.Max(alpha, value);
            if (alpha >= beta) break;
            currentNode = currentNode.NextNode;
        }

        TranspositionTable[hash] = (value, depth);
        return (value, move);
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