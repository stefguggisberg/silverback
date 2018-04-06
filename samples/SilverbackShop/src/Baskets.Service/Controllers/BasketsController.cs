﻿using System.Threading.Tasks;
using Baskets.Domain.Model.BasketAggregate;
using Baskets.Domain.Repositories;
using Baskets.Domain.Services;
using Common.Data;
using Microsoft.AspNetCore.Mvc;

namespace Baskets.Service.Controllers
{
    [Route("basket")]
    public class BasketsController : Controller
    {
        private readonly BasketsService _basketService;
        private readonly CheckoutService _checkoutService;

        public BasketsController(CheckoutService checkoutService, BasketsService basketService)
        {
            _checkoutService = checkoutService;
            _basketService = basketService;
        }

        [HttpGet]
        public async Task<ActionResult> Get()
        {
            return Ok(await _basketService.GetBasket(UserData.DefaultUserId));
        }

        [HttpPost("checkout")]
        public async Task<ActionResult> Checkout()
        {
            var basket = await _basketService.GetBasket(UserData.DefaultUserId);

            try
            {
                await _checkoutService.Checkout(basket);

                return NoContent();
            }
            catch (BasketValidationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
