using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ChessNEA.Logic.Objects;
using ChessNEA.Logic.Objects.LinkedList;

namespace ChessNEA;

public partial class MainWindow : Window
{
    private static bool _isPlayerWhite = true;
    private Game _game = new(_isPlayerWhite, true);
    private (int x, int y) _selectedPiece;
    private bool _isWhiteOnBottom = true;

    public MainWindow()
    {
        InitializeComponent();
        InitializeBoard();
    }

    private void InitializeBoard(LinkedList<(int, int)>? moves = null)
    {
        BoardGrid.Children.Clear();
        DisplayMovesGrid.Children.Clear();
        for (int i = 7; i >= 0; i--)
        {
            for (int j = 0; j < 8; j++)
            {
                int x = _isWhiteOnBottom ? i : 7 - i;
                int y = _isWhiteOnBottom ? j : 7 - j;
                Button button = new()
                {
                    Content = GetSymbol(_game.Board[x, y]),
                    FontSize = 72,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    FontFamily = new FontFamily("Segoe UI Symbol"),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                    CommandParameter = (x, y)
                };
                if (moves is not null && moves.Contains((x, y)))
                {
                    button.Click += (_, _) => MovePiece(((int x, int y))button.CommandParameter);
                }
                else
                {
                    button.Click += (_, _) => GetMoves(((int x, int y))button.CommandParameter);
                }
                BoardGrid.Children.Add(button);
                DisplayMovesGrid.Children.Add(new TextBlock()
                {
                    Text = "",
                    FontSize = 60,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromArgb(50, 0, 0, 0)),
                });
            }
        }
    }

    private void MovePiece((int x, int y) position)
    {
        _game.MovePiece(_selectedPiece, position);
        InitializeBoard();

        if (!_game.IsFinished) return;
        string message = _game.Score switch
        {
            0.5 => "Draw!",
            1 => "White wins!",
            -1 => "Black wins!",
            _ => ""
        };
        WinnerText.Content = message;
        WinnerText.Background = new SolidColorBrush(Color.FromArgb(50, 0, 0, 0));
        WinnerText.IsVisible = true;
        ResignOrNewGameButton.Content = "New Game";
        ResignOrNewGameButton.Click -= ClickButtonResign;
        ResignOrNewGameButton.Click += ClickButtonNewGame;
    }

    private void ClickButtonNewGame(object? sender, RoutedEventArgs e)
    {
        NewGameConfirmation newGameConfirmation = new();

        // Subscribe to the NewGameConfirmed event
        newGameConfirmation.NewGameConfirmed += isWhite =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _isPlayerWhite = isWhite;
                SetupNewGame();
            });
        };

        newGameConfirmation.Show();
    }
    
    private void SetupNewGame()
    {
        _game = new Game(_isPlayerWhite, true);
        _selectedPiece = (0, 0);
        _isWhiteOnBottom = _isPlayerWhite;
        InitializeBoard();
        WinnerText.IsVisible = false;
        ResignOrNewGameButton.Content = "Resign";
        ResignOrNewGameButton.Click -= ClickButtonNewGame;
        ResignOrNewGameButton.Click += ClickButtonResign;
    }
    
    private void ClickButtonResign(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
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
        
        Node<(int x, int y)>? node = moves.Head;
        while (node is not null)
        {
            ((TextBlock)DisplayMovesGrid.Children[8 * (7 - node.Data.x) + node.Data.y]).Text = "\u2b24";
            node = node.NextNode;
        }
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

    private void ClickButtonRotateBoard(object? sender, RoutedEventArgs routedEventArgs)
    {
        _isWhiteOnBottom = !_isWhiteOnBottom;
        InitializeBoard();
    }
}