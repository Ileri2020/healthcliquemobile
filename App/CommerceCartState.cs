using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using EEE.Components.Utility;

namespace EEE.App;

public sealed class CommerceCartState : INotifyPropertyChanged
{
    public static CommerceCartState Shared { get; } = new();

    private CommerceCartState()
    {
        Items.CollectionChanged += (_, args) =>
        {
            if (args.NewItems is not null)
            {
                foreach (CommerceCartItem item in args.NewItems)
                    item.PropertyChanged += OnItemPropertyChanged;
            }
            if (args.OldItems is not null)
            {
                foreach (CommerceCartItem item in args.OldItems)
                    item.PropertyChanged -= OnItemPropertyChanged;
            }
            OnPropertyChanged(nameof(ItemCount));
            OnPropertyChanged(nameof(TotalText));
        };
    }

    public ObservableCollection<CommerceCartItem> Items { get; } = [];
    public int ItemCount => Items.Sum(item => Math.Max(item.Quantity, 0));
    public string TotalText => $"₦{Items.Sum(item => (item.CustomPrice ?? item.Product?.Price ?? item.Price) * item.Quantity):N0}";

    public void Add(CommerceProduct product, int quantity = 1)
    {
        var existing = Items.FirstOrDefault(item => item.ProductId == product.Id && !item.IsSpecial);
        if (existing is not null)
        {
            existing.Quantity += quantity;
            return;
        }

        Items.Add(new CommerceCartItem
        {
            ProductId = product.Id,
            Product = product,
            Name = product.Name,
            ImageUrl = product.Images.FirstOrDefault() ?? string.Empty,
            Price = product.Price,
            Category = product.Category?.Name ?? "Medical",
            Quantity = quantity
        });
    }

    public void Add(DiscoveryProduct product) =>
        Add(new CommerceProduct
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description ?? string.Empty,
            Price = product.Price,
            Images = product.Images,
            Category = product.Category is null
                ? null
                : new CommerceCategory { Id = product.Category.Id, Name = product.Category.Name },
            BulkPrices = product.BulkPrices
        }, 1);

    public void Add(CommerceCartItem item)
    {
        var existing = !string.IsNullOrWhiteSpace(item.ProductId)
            ? Items.FirstOrDefault(cartItem => cartItem.ProductId == item.ProductId && !cartItem.IsSpecial)
            : null;
        if (existing is not null)
            existing.Quantity += item.Quantity;
        else
            Items.Add(item);
    }

    public void Remove(CommerceCartItem item) => Items.Remove(item);
    public void Clear() => Items.Clear();

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(CommerceCartItem.Quantity))
        {
            OnPropertyChanged(nameof(ItemCount));
            OnPropertyChanged(nameof(TotalText));
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
