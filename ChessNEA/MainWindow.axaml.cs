using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ChessNEA.Logic.Objects;
using ChessNEA.Logic.Objects.LinkedList;

namespace ChessNEA;

public partial class MainWindow : Window
{
    private readonly Game _game = new();
    private (int x, int y) _selectedPiece;
    
    public MainWindow()
    {
        InitializeComponent();
        InitializeBoard();
    }

    private void InitializeBoard(LinkedList<(int, int)>? moves = null)
    {
        BoardGrid.Children.Clear();
        for (int i = 7; i >= 0; i--)
        {
            for (int j = 0; j < 8; j++)
            {
                Button button = new()
                {
                    Content = GetSymbol(_game.Board[i, j]),
                    FontSize = 72,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    FontFamily = new FontFamily("Segoe UI Symbol"),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                    CommandParameter = (i, j)
                };
                if (moves is not null && moves.Contains((i, j)))
                {
                    button.Click += (_, _) => MovePiece(((int x, int y))button.CommandParameter);
                }
                else
                {
                    button.Click += (_, _) => GetMoves(((int x, int y))button.CommandParameter);
                }
                BoardGrid.Children.Add(button);
            }
        }
    }

    private void MovePiece((int x, int y) position)
    {
        _game.MovePiece(_selectedPiece, position);
        InitializeBoard();
    }
    

    private void GetMoves((int x, int y) position)
    {
        InitializeBoard();
        _selectedPiece = position;
        LinkedList<(int, int)>? moves = _game.GetMoves(position);

        if (moves is null)
        {
            return;
        }
        
        InitializeBoard(moves);
    }

    private static string GetSymbol(string piece)
    {
        if (piece == "")
        {
            return "";
        }
        
        return piece[..2] switch
        {
            "wP" => "\u2659",
            "wN" => "\u2658",
            "wB" => "\u2657",
            "wR" => "\u2656",
            "wQ" => "\u2655",
            "wK" => "\u2654",
            "bP" => "\u265f",
            "bN" => "\u265e",
            "bB" => "\u265d",
            "bR" => "\u265c",
            "bQ" => "\u265b",
            "bK" => "\u265a",
            _ => throw new ArgumentException("Unknown piece")
        };
    }
}