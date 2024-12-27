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
    }

    private void ClickButtonNewGame(object? sender, RoutedEventArgs e)
    {
        bool isWhite = !IsWhite.IsEnabled;
        NewGameConfirmed?.Invoke(isWhite);
        Close();
    }
}