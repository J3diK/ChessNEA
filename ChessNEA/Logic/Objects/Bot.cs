using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
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

    // Graph representing openings with at least 100k games played by masters, according to Lichess as of 2025-01-19.
    private static readonly Dictionary<string, List<string>> OpeningsBook = new()
    {
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
    private static string _openingMoves = "";
    private static bool _inOpeningBook = true;
    
    private static readonly ConcurrentDictionary<((int oldX, int oldY), (int newX, int newY)), int> MoveVoteCount =
        new();
    // Avoid redundant calculations
    private static readonly ConcurrentDictionary<int[], (double, int)> TranspositionTable = new();

    private const double Alpha = double.MinValue;
    private const double Beta = double.MaxValue;
    
    public bool IsWhite;

    private const int MaxNegamaxTimeMs = 30_000;

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
    public Task<((int oldX, int oldY), (int newX, int newY))> GetMove(Game game)
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
        
        return Task.FromResult(DecodeMove(_openingMoves.Substring(_openingMoves.Length - 4)));
    }
    
    private static string EncodeMove(((int oldX, int oldY), (int newX, int newY)) move)
    {
        // +97 converts 1 to A, 2 to B, etc.
        return
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
    private Task<((int oldX, int oldY), (int newX, int newY))> FindBestMove(Game game, int colour, int maxThreads)
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
        childGames = MergeSort(game, childGames);
        
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
    
    private static double Evaluate(Game game, ((int oldX, int oldY), (int newX, int newY)) move)
    {
        // Deep copy
        Game childGame = game.Copy();
        childGame.MovePiece(move.Item1, move.Item2);
        return Evaluate(childGame);
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