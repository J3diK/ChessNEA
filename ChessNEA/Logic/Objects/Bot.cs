using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChessNEA.Logic.Objects.LinkedList;

namespace ChessNEA.Logic.Objects;

// TODO: https://www.chessprogramming.org/Simplified_Evaluation_Function
public class Bot
{
    private const int Depth = 5;
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

        ConcurrentBag<(double, ((int oldX, int oldY), (int newX, int newY)))> resultsBag = [];

        double alpha1 = alpha;
        Parallel.ForEach(childGamesList, child =>
        {
            double value2 = -(Negamax(child, depth - 1, -colour, -beta, -alpha1).Result.Item1);
            resultsBag.Add((value2, child.LastMove));
        });

        foreach ((double, ((int oldX, int oldY), (int newX, int newY))) result in resultsBag)
        {
            if (result.Item1 > value)
            {
                value = result.Item1;
                move = result.Item2;
            }
            alpha = Math.Max(alpha, value);
            if (alpha >= beta)
            {
                break;
            }
        }

        _transpositionTable[gameHash] = (value, move);

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
                    'P' => 1,
                    'N' => 3,
                    'B' => 3,
                    'R' => 5,
                    'Q' => 9,
                    'K' => 0,
                    _ => throw new ArgumentException("Invalid piece")
                };
                score += pieceValue * (game.Board[i, j][0] == 'w' ? 1 : -1);
            }
        }

        return score;
    }
}