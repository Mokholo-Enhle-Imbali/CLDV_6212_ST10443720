using ABCRetailersFunction.Entities;
using ABCRetailersFunction.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABCRetailersFunction.Helpers
{
    public static class Map
    {
        // Table Entity  ->  DTOs returned to MVC

        public static CustomerDto ToDto(CustomerEntity e)
            => new(
                Id: e.RowKey,
                Name: e.Name,
                Surname: e.Surname,
                Username: e.Username,
                Email: e.Email,
                ShippingAddress: e.ShippingAddress
            );

        public static ProductDto ToDto(ProductEntity e)
            => new(
                Id: e.RowKey,
                ProductName: e.ProductName,
                Description: e.Description,
                Price: (decimal)e.Price,                 // stored as double in Table, cast to decimal for MVC
                StockAvailable: e.StockAvailable,
                ImageUrl: e.ImageUrl
            );

        public static OrderDto ToDto(OrderEntity e)
        {
            var unitPrice = (decimal)e.UnitPrice;       // double -> decimal for money in MVC
            var total = unitPrice * e.Quantitiy;

            return new OrderDto(
                Id: e.RowKey,
                CustomerId: e.CustomerId,
                ProductId: e.ProductId,
                ProductName: e.ProductName,
                Quantity: e.Quantitiy,
                UnitPrice: unitPrice,
                TotalAmount: total,
                OrderDateUtc: e.OrderDate,
                Status: e.Status
            );
        }
    }
}
