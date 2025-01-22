using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ChessNEA;

public partial class PromotionOptions : Window
{
    public event Action<string>? PromotionSelected;
    
    public PromotionOptions()
    {
        InitializeComponent();
    }
    
    private void ClickButtonPromote(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton radioButton) return;
        string selectedPiece = radioButton.Content!.ToString()!;
        PromotionSelected?.Invoke(selectedPiece);
        Close();
    }
}