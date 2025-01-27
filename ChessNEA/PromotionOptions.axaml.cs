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

    private void ClickButtonPromote(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton radioButton) return;
        string selectedPiece = radioButton.Content!.ToString()!;
        PromotionSelected?.Invoke(selectedPiece);
        Close();
    }
}