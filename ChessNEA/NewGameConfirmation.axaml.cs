using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ChessNEA;

public partial class NewGameConfirmation : Window
{
    public event Action<bool>? NewGameConfirmed;
    
    public NewGameConfirmation()
    {
        InitializeComponent();
        IsWhite.IsChecked = false;
    }

    private void ClickButtonNewGame(object? sender, RoutedEventArgs e)
    {
        bool isWhite = (bool)(!IsWhite.IsChecked)!;
        NewGameConfirmed?.Invoke(isWhite);
        Close();
    }
}