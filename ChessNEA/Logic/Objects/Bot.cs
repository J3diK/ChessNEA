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
    private const double SequentialDepthPercentage = 0.2; // Jamboree, https://courses.cs.washington.edu/courses/cse332/16au/handouts/games.pdf
    public bool IsWhite;
    private readonly ConcurrentDictionary<int[], (double, ((int oldX, int oldY), (int newX, int newY)))> _transpositionTable = new();

    public async Task<((int oldX, int oldY), (int newX, int newY))> GetMove(Game game)
    {
        return (await Negamax(game, Depth, IsWhite ? 1 : -1, double.NegativeInfinity, double.PositiveInfinity)).Item2;
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
        LinkedList.LinkedList<Game> childGames = GetChildGames(game, colour);
        ((int oldX, int oldY), (int newX, int newY)) move = ((-1, -1), (-1, -1));

        List<Game> childGamesList = [];
        Node<Game>? currentNode = childGames.Head;
        while (currentNode != null)
        {
            childGamesList.Add(currentNode.Data!);
            currentNode = currentNode.NextNode;
        }
        
        int sequentialDepth = (int)(childGamesList.Count * SequentialDepthPercentage);
        
        List<Game> childGamesListSequential = childGamesList.GetRange(0, Math.Min(
            sequentialDepth, childGamesList.Count));
        int length = childGamesList.Count - sequentialDepth;
        if (length < 0) length = 0;
        List<Game> childGamesListParallel = childGamesList.GetRange(Math.Min(sequentialDepth, childGamesList.Count), length);
        
        
        foreach (Game child in childGamesListSequential)
        {
            double value2 = -Negamax(child, depth - 1, -colour, -beta, -alpha).Result.Item1;
            if (value2 > value)
            {
                value = value2;
                move = child.LastMove;
            }
            alpha = Math.Max(alpha, value);
            if (alpha >= beta)
            {
                break;
            }
        }
        
        Parallel.ForEach(childGamesListParallel, (child, state) =>
        {
            double value2 = -Negamax(child, depth - 1, -colour, -beta, -alpha).Result.Item1;
            lock (this)
            {
                if (value2 > value)
                {
                    value = value2;
                    move = child.LastMove;
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

    private static LinkedList.LinkedList<Game> GetChildGames(Game game, int colour)
    {
        LinkedList.LinkedList<Game> childGames = new();
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
                    Game newGame = new(colour == 1, (string[,])game.Board.Clone());
                    newGame.MovePiece((i, j), currentMove.Data);
                    childGames.AddNode(newGame);
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
}