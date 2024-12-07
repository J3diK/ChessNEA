using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ChessNEA.Logic.Objects;

namespace ChessNEA;

public partial class MainWindow : Window
{
    private readonly Game _game = new();
    
    public MainWindow()
    {
        InitializeComponent();
        InitializeBoard();
    }

    private void InitializeBoard()
    {
        for (int i = 7; i >= 0; i--)
        {
            for (int j = 0; j < 8; j++)
            {
                Button button = new Button
                {
                    Content = GetSymbol(_game.Board[i, j]),
                    FontSize = 72,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    FontFamily = new FontFamily("Segoe UI Symbol"),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                };
                button.Click += (sender, e) => HelloWorld();
                
                BoardGrid.Children.Add(button);
            }
        }
    }

    private static void HelloWorld()
    {
        Console.WriteLine("Hello World");
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