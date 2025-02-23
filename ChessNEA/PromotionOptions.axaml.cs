using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ChessNEA;

public partial class PromotionOptions : Window
{
    public PromotionOptions()
    {
        InitializeComponent();
    }

    public event Action<string>? PromotionSelected;

    /// <summary>
    ///     Closes the window and sends the selected piece to the main window.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ClickButtonPromote(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton radioButton) return;
        string selectedPiece = radioButton.Content!.ToString()!;
        PromotionSelected?.Invoke(selectedPiece);
        Close();
    }
}