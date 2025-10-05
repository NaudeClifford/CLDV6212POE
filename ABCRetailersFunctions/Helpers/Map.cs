using System;
using ABCRetailersFunctions.Entities;
using ABCRetailersFunctions.Models;         // From your Functions project
using ABCRetails.Models;            // From your referenced Models project

namespace ABCRetailersFunctions.Helpers
{
    public static class Map
    {
        // Example: Convert Product model to ProductDto
        // CUSTOMER
        public static CustomerDto ToDto(Customer entity) => new()
        {
            CustomerId = entity.CustomerId,
            Name = entity.Name,
            Surname = entity.Surname,
            Username = entity.Username,
            Email = entity.Email,
            ShippingAddress = entity.ShippingAddress
        };

        public static Customer ToEntity(CustomerDto dto) => new()
        {
            RowKey = dto.CustomerId ?? Guid.NewGuid().ToString(),
            Name = dto.Name,
            Surname = dto.Surname,
            Username = dto.Username,
            Email = dto.Email,
            ShippingAddress = dto.ShippingAddress
        };

        // ORDER
        public static OrderDto ToDto(Order entity) => new()
        {
            OrderId = entity.OrderId,
            CustomerId = entity.CustomerId,
            Username = entity.Username,
            ProductId = entity.ProductId,
            ProductName = entity.ProductName,
            OrderDate = entity.OrderDate,
            Quantity = entity.Quantity,
            UnitPrice = entity.UnitPrice,
            TotalPrice = entity.TotalPrice,
            Status = entity.Status
        };

        public static Order ToEntity(OrderDto dto) => new()
        {
            RowKey = dto.OrderId ?? Guid.NewGuid().ToString(),
            CustomerId = dto.CustomerId,
            Username = dto.Username,
            ProductId = dto.ProductId,
            ProductName = dto.ProductName,
            OrderDate = dto.OrderDate,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice,
            TotalPrice = dto.TotalPrice,
            Status = dto.Status
        };

        // PRODUCT
        public static ProductDto ToDto(Product entity) => new()
        {
            ProductId = entity.ProductId,
            ProductName = entity.ProductName,
            Description = entity.Description,
            Price = entity.Price,
            StockAvailable = entity.StockAvailable,
            ImageUrl = entity.ImageUrl
        };

        public static Product ToEntity(ProductDto dto) => new()
        {
            RowKey = dto.ProductId ?? Guid.NewGuid().ToString(),
            ProductName = dto.ProductName,
            Description = dto.Description,
            Price = dto.Price,
            StockAvailable = dto.StockAvailable,
            ImageUrl = dto.ImageUrl
        };

        // ENTITY -> DTO (from Table Storage)
        public static CustomerDto ToDto(CustomerEntity e) => new()
        {
            CustomerId = e.RowKey,
            Name = e.Name,
            Surname = e.Surname,
            Username = e.Username,
            Email = e.Email,
            ShippingAddress = e.ShippingAddress
        };

        public static ProductDto ToDto(ProductEntity e) => new()
        {
            ProductId = e.RowKey,
            ProductName = e.ProductName,
            Description = e.Description,
            Price = e.Price,
            StockAvailable = e.StockAvailable,
            ImageUrl = e.ImageUrl
        };

        public static OrderDto ToDto(OrderEntity e) => new()
        {
            OrderId = e.RowKey,
            CustomerId = e.CustomerId,
            Username = e.Username,
            ProductId = e.ProductId,
            ProductName = e.ProductName,
            OrderDate = e.OrderDate,
            Quantity = e.Quantity,
            UnitPrice = e.UnitPrice,
            TotalPrice = e.TotalPrice,
            Status = e.Status
        };

    }
}
