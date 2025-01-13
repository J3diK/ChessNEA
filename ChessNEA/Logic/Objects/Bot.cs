using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

    private const int Depth = 6;
    private const double SequentialDepthPercentage = 0.5; // Jamboree, https://courses.cs.washington.edu/courses/cse332/16au/handouts/games.pdf
    public bool IsWhite;
    private readonly ConcurrentDictionary<int[], (double, ((int oldX, int oldY), (int newX, int newY)))> _transpositionTable = new();

    private static List<((int x, int y), (int x, int y))> MergeSort(Game game, List<((int x, int y), (int x, int y))> games)
    {
        if (games.Count <= 1)
        {
            return games;
        }
        
        List<((int x, int y), (int x, int y))> left = games.GetRange(0, games.Count / 2);
        List<((int x, int y), (int x, int y))> right = games.GetRange(games.Count / 2, games.Count - games.Count / 2);
        
        Task<List<((int x, int y), (int x, int y))>> leftTask = Task.Run(() => MergeSort(game, left));
        Task<List<((int x, int y), (int x, int y))>> rightTask = Task.Run(() => MergeSort(game, right));
        Task.WaitAll(leftTask, rightTask);
        
        return Merge(game, leftTask.Result, rightTask.Result);
    }

    private static List<((int x, int y), (int x, int y))> Merge(Game game, List<((int x, int y), (int x, int y))> left, List<((int x, int y), (int x, int y))> right)
    {
        List<((int x, int y), (int x, int y))> merged = [];
        int leftIndex = 0;
        int rightIndex = 0;
        
        while (leftIndex < left.Count && rightIndex < right.Count)
        {
            if (Evaluate(game, left[leftIndex]) > Evaluate(game, right[rightIndex]))
            {
                merged.Add(left[leftIndex]);
                leftIndex++;
            }
            else
            {
                merged.Add(right[rightIndex]);
                rightIndex++;
            }
        }
        
        while (leftIndex < left.Count)
        {
            merged.Add(left[leftIndex]);
            leftIndex++;
        }
        
        while (rightIndex < right.Count)
        {
            merged.Add(right[rightIndex]);
            rightIndex++;
        }
        
        return merged;
    }
    
    public async Task<((int oldX, int oldY), (int newX, int newY))> GetMove(Game game)
    {
        // return (await Negamax(game, Depth, IsWhite ? 1 : -1, double.NegativeInfinity, double.PositiveInfinity)).Item2;
        return (await PrincipalVariationSearch(game, Depth, IsWhite ? 1 : -1, double.NegativeInfinity, double.PositiveInfinity)).Item2;
    }
    
    // Alternative to Negamax, PVS, https://www.chessprogramming.org/Principal_Variation_Search
    private Task<(double, ((int oldX, int oldY), (int newX, int newY)))> PrincipalVariationSearch(Game game, int depth,
        int colour,
        double alpha, double beta)
    {
        int[] gameHash = game.GetHash();
        if (_transpositionTable.TryGetValue(gameHash, out (double, ((int oldX, int oldY), (int newX, int newY))) ttEntry) && ttEntry.Item1 >= depth)
        {
            return Task.FromResult(ttEntry);
        }

        if (depth == 0 || game.IsFinished)
        {
            return Task.FromResult((colour * Evaluate(game), ((-1, -1), (-1, -1))));
        }
        
        LinkedList.LinkedList<((int x, int y), (int x, int y))> childGames = GetChildGames(game, colour);
        ((int oldX, int oldY), (int newX, int newY)) move = ((-1, -1), (-1, -1));

        List<((int x, int y), (int x, int y))> childGamesList = [];
        Node<((int x, int y), (int x, int y))>? currentNode = childGames.Head;
        while (currentNode != null)
        {
            childGamesList.Add(currentNode.Data);
            currentNode = currentNode.NextNode;
        }

        childGamesList = MergeSort(game, childGamesList);

        bool firstChild = true;
        foreach (((int x, int y), (int x, int y)) child in childGamesList)
        {
            Game childGame = game.Copy();
            childGame.MovePiece(child.Item1, child.Item2);
            double score;
            if (firstChild)
            {
                score = -PrincipalVariationSearch(childGame, depth - 1, -colour, -beta, -alpha).Result.Item1;
                firstChild = false;
            }
            else
            {
                score = -PrincipalVariationSearch(childGame, depth - 1, -colour, -alpha - 1, -alpha).Result.Item1;
                if (alpha < score && score < beta)
                {
                    score = -PrincipalVariationSearch(childGame, depth - 1, -colour, -beta, -alpha).Result.Item1;
                }
            }

            if (score > alpha)
            {
                move = childGame.LastMove;
            }
            alpha = Math.Max(alpha, score);
            if (alpha >= beta)
            {
                break;
            }
        }

        _transpositionTable[gameHash] = (depth, move);
        return Task.FromResult((alpha, move));
    }
    
    private Task<(double, ((int oldX, int oldY), (int newX, int newY)))> Negamax(Game game, int depth, int colour,
        double alpha, double beta)
    {
        int[] gameHash = game.GetHash();
        if (_transpositionTable.TryGetValue(gameHash, out (double, ((int oldX, int oldY), (int newX, int newY))) ttEntry) && ttEntry.Item1 >= depth)
        {
            return Task.FromResult(ttEntry);
        }

        if (depth == 0 || game.IsFinished)
        {
            return Task.FromResult((colour * Evaluate(game), ((-1, -1), (-1, -1))));
        }

        double value = double.NegativeInfinity;
        LinkedList.LinkedList<((int x, int y), (int x, int y))> childGames = GetChildGames(game, colour);
        ((int oldX, int oldY), (int newX, int newY)) move = ((-1, -1), (-1, -1));

        List<((int x, int y), (int x, int y))> childGamesList = [];
        Node<((int x, int y), (int x, int y))>? currentNode = childGames.Head;
        while (currentNode != null)
        {
            childGamesList.Add(currentNode.Data);
            currentNode = currentNode.NextNode;
        }

        childGamesList = MergeSort(game, childGamesList);
        
        int sequentialDepth = (int)(childGamesList.Count * SequentialDepthPercentage);
        
        List<((int x, int y), (int x, int y))> childGamesListSequential = childGamesList.GetRange(0, Math.Min(
            sequentialDepth, childGamesList.Count));
        int length = childGamesList.Count - sequentialDepth;
        if (length < 0) length = 0;
        List<((int x, int y), (int x, int y))> childGamesListParallel = childGamesList.GetRange(Math.Min(sequentialDepth, childGamesList.Count), length);
        
        
        foreach (((int x, int y), (int x, int y)) child in childGamesListSequential)
        {
            Game childGame = game.Copy();
            childGame.MovePiece(child.Item1, child.Item2);
            double value2 = -Negamax(childGame, depth - 1, -colour, -beta, -alpha).Result.Item1;
            if (value2 > value)
            {
                value = value2;
                move = childGame.LastMove;
            }
            alpha = Math.Max(alpha, value);
            if (alpha >= beta)
            {
                break;
            }
        }
        
        Parallel.ForEach(childGamesListParallel, (child, state) =>
        {
            Game childGame = game.Copy();
            childGame.MovePiece(child.Item1, child.Item2);
            double value2 = -Negamax(childGame, depth - 1, -colour, -beta, -alpha).Result.Item1;
            lock (this)
            {
                if (value2 > value)
                {
                    value = value2;
                    move = childGame.LastMove;
                }
                alpha = Math.Max(alpha, value);
                if (alpha >= beta)
                {
                    state.Break();
                }
            }
        });
        

        _transpositionTable[gameHash] = (depth, move);
        return Task.FromResult((value, move));
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
    
    
    private static int GetPieceSquareValue(string piece, int x, int y)
    {
        if (piece[0] == 'b')
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

    private static double Evaluate(Game game, ((int x, int y), (int x, int y)) move)
    {
        Game gameToEvaluate = game.Copy();
        gameToEvaluate.MovePiece(move.Item1, move.Item2);
        return EvaluateWithoutMove(game);

    }

    private static double Evaluate(Game game)
    {
        return EvaluateWithoutMove(game);
    }

    private static double EvaluateWithoutMove(Game game)
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
}