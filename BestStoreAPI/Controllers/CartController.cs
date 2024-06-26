﻿using BestStoreAPI.Models;
using BestStoreAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BestStoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public CartController(ApplicationDbContext context)
        {
            this.context = context;
        }

        [HttpGet("PaymentMethods")]
        public IActionResult GetPaymentMethods()
        {
            return Ok(OrderHelper.PaymentMethods);
        }

        [HttpGet]
        public IActionResult GetCart(string productIdentifiers)
        {
            CartDto cartDto = new CartDto()
            {
                CartItems = new List<CartItemDto>(),
                SubTotal = 0,
                ShippingFee = OrderHelper.ShippingFee,
                TotalPrice = 0,
            };

            var productDictionary = OrderHelper.GetProductDictionary(productIdentifiers);

            foreach (var pair in productDictionary)
            {
                int productId = pair.Key;
                var product = context.Products.Find(productId);
                if (product == null)
                {
                    continue;
                }

                var cartItemDto = new CartItemDto();
                cartItemDto.Product = product;
                cartItemDto.Quantity = pair.Value;

                cartDto.CartItems.Add(cartItemDto);
                cartDto.SubTotal += product.Price * pair.Value;
                cartDto.TotalPrice = cartDto.SubTotal + cartDto.ShippingFee;

            }

            return Ok(cartDto);
        }
    }
}
