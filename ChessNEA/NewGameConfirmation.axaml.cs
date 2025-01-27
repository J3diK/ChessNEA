using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ChessNEA;

public partial class NewGameConfirmation : Window
{
    public NewGameConfirmation()
    {
        InitializeComponent();
        IsWhite.IsChecked = false;
    }

    public event Action<int, bool>? NewGameConfirmed;

    private void ClickButtonNewGame(object? sender, RoutedEventArgs e)
    {
        bool isWhite = (bool)(!IsWhite.IsChecked)!;
        int maxDepthPly = (int)MaxDepthPly.Value;
        NewGameConfirmed?.Invoke(maxDepthPly, isWhite);
        Close();
    }
}