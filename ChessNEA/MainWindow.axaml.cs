using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ChessNEA.Logic;
using ChessNEA.Logic.Objects;
using ChessNEA.Logic.Objects.LinkedList;

namespace ChessNEA;

public partial class MainWindow : Window
{
    private static bool _isPlayerWhite = true;
    private static int _maxDepthPly = 4;
    private readonly SolidColorBrush _background = new(Color.FromArgb(50, 0, 0, 0));
    private Bot _bot = new(_maxDepthPly);
    private Game _game = new(_isPlayerWhite);
    private bool _isWhiteOnBottom = true;
    private (int x, int y) _selectedPiece;

    public MainWindow()
    {
        InitializeComponent();
        SetupNewGame();
        FirstOpen();
    }

    private void FirstOpen()
    {
        WinnerText.Content = "";
        WinnerText.Background = new SolidColorBrush(_background.Color);
        WinnerText.IsVisible = true;
        ResignOrNewGameButton.Content = "New Game";
        ResignOrNewGameButton.Click -= ClickButtonResign;
        ResignOrNewGameButton.Click += ClickButtonNewGame;
        BottomPlayer.FontSize = 28;
        BottomPlayer.FontFamily = new FontFamily("Segoe UI Symbol");
        TopPlayer.FontSize = 28;
        TopPlayer.FontFamily = new FontFamily("Segoe UI Symbol");
    }

    private void InitializeBoard(LinkedList<(int, int)>? moves = null)
    {
        BoardGrid.Children.Clear();
        DisplayMovesGrid.Children.Clear();
        for (int i = 7; i >= 0; i--)
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
                button.Click += (_, _) => MovePiecePlayer(((int x, int y))button.CommandParameter);
            else
                button.Click += (_, _) => GetMoves(((int x, int y))button.CommandParameter);
            BoardGrid.Children.Add(button);
            DisplayMovesGrid.Children.Add(new TextBlock
            {
                Text = "",
                FontSize = 60,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromArgb(50, 0, 0, 0))
            });
        }
    }

    private async void MovePiecePlayer((int x, int y) position)
    {
        if (_game.IsPromotingMove(_selectedPiece, position))
        {
            TaskCompletionSource<char> tcs = new();
            PromotionOptions promotionOptions = new();
            promotionOptions.PromotionSelected += selectedPiece =>
            {
                char piece = selectedPiece[0] switch
                {
                    'Q' => 'Q',
                    'R' => 'R',
                    'B' => 'B',
                    'K' => 'N',
                    _ => throw new ArgumentException("Unknown piece")
                };
                tcs.SetResult(piece);
                promotionOptions.Close();
            };
            promotionOptions.Show();

            char promotionPiece = await tcs.Task;
            MovePiece(position, promotionPiece);
        }
        else
        {
            MovePiece(position);
        }

        if (_game.IsFinished) return;

        await Dispatcher.UIThread.InvokeAsync(() => { BlockInput.IsVisible = true; });

        MovePieceBot();
    }

    private async void MovePieceBot()
    {
        ((int oldX, int oldY) moveOld, (int newX, int newY) moveNew, char? promotionPiece) = await _bot.GetMove(_game);
        _selectedPiece = moveOld;
        MovePiece(moveNew, promotionPiece);

        await Dispatcher.UIThread.InvokeAsync(() => { BlockInput.IsVisible = false; });
    }

    private void MovePiece((int x, int y) position, char? promotionPiece = null)
    {
        if (_game.Board[position.x, position.y] != "") AddCapture(_game.Board[position.x, position.y]);

        if (promotionPiece is not null)
            _game.MovePiece(_selectedPiece, position, promotionPiece);
        else
            _game.MovePiece(_selectedPiece, position);
        InitializeBoard();

        if (!_game.IsFinished) return;
        DisplayGameEnd();
    }

    private void AddCapture(string piece)
    {
        if ((piece[0] == 'w' && !_isWhiteOnBottom) || (piece[0] == 'b' && _isWhiteOnBottom))
            BottomPlayer.Text += GetSymbol(piece);
        else
            TopPlayer.Text += GetSymbol(piece);
    }


    private void DisplayGameEnd()
    {
        string message = _game.Score switch
        {
            0 => "Draw!",
            (int)Constants.Infinity => "White wins!",
            -(int)Constants.Infinity => "Black wins!",
            _ => ""
        };
        WinnerText.Content = message;
        WinnerText.Background = new SolidColorBrush(_background.Color);
        WinnerText.IsVisible = true;
        ResignOrNewGameButton.Content = "New Game";
        ResignOrNewGameButton.Click -= ClickButtonResign;
        ResignOrNewGameButton.Click += ClickButtonNewGame;
    }

    private void ClickButtonNewGame(object? sender, RoutedEventArgs e)
    {
        NewGameConfirmation newGameConfirmation = new();

        // Subscribe to the NewGameConfirmed event
        newGameConfirmation.NewGameConfirmed += (maxDepthPly, isWhite) =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _isPlayerWhite = isWhite;
                _maxDepthPly = maxDepthPly;
                SetupNewGame();
            });
        };

        newGameConfirmation.Show();
    }

    private void SetupNewGame()
    {
        _bot = new Bot(_maxDepthPly, !_isPlayerWhite);
        _game = new Game();
        _selectedPiece = (0, 0);
        BottomPlayer.Text = "";
        TopPlayer.Text = "";
        _isWhiteOnBottom = _isPlayerWhite;
        InitializeBoard();
        WinnerText.IsVisible = false;
        ResignOrNewGameButton.Content = "Resign";
        ResignOrNewGameButton.Click -= ClickButtonNewGame;
        ResignOrNewGameButton.Click += ClickButtonResign;

        if (!_isPlayerWhite) MovePieceBot();
    }

    private void ClickButtonResign(object? sender, RoutedEventArgs e)
    {
        _game.Score = _isPlayerWhite ? -(int)Constants.Infinity : (int)Constants.Infinity;
        DisplayGameEnd();
    }


    private void GetMoves((int x, int y) position)
    {
        InitializeBoard();
        _selectedPiece = position;
        LinkedList<(int, int)>? moves = _game.GetMoves(position);

        if (moves is null) return;

        InitializeBoard(moves);

        Node<(int x, int y)>? node = moves.Head;
        while (node is not null)
        {
            int x = _isWhiteOnBottom ? 7 - node.Data.x : node.Data.x;
            int y = _isWhiteOnBottom ? node.Data.y : 7 - node.Data.y;

            ((TextBlock)DisplayMovesGrid.Children[8 * x + y]).Text = "\u2b24";
            node = node.NextNode;
        }
    }

    private static string GetSymbol(string piece)
    {
        if (piece == "") return "";

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

    private void ClickButtonRotateBoard(object? sender, RoutedEventArgs e)
    {
        _isWhiteOnBottom = !_isWhiteOnBottom;
        InitializeBoard();
    }
}