﻿using AutoMapper;
using Core.Entities;
using Core.Entities.Items;
using Core.Entities.Ventas;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs;

namespace WebApi.Controllers.ItemController
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemController : ControllerBase
    {
        IItemRepository _itemRepository;
        private readonly IMapper _mapper;
        protected ApiResponse _response;
        public ItemController(IItemRepository itemRepository, IMapper mapper)
        {
            _itemRepository = itemRepository;
            _mapper = mapper;
            _response = new ApiResponse();
        }

        [HttpGet]
        public async Task<IActionResult> Get(int top = 20, int skip = 0)
        {
            // int pageSize = 100;
            var sessionID = Request.Headers["SessionID"];
            var result = await _itemRepository.GetAll(sessionID, top);

            // Mapear la orden a OrderDto
            var itemsDto = _mapper.Map<List<ItemDto>>(result);
            _response.IsSuccessful = true;
            _response.Resultado = result;
            _response.StatusCode = System.Net.HttpStatusCode.OK;
            return Ok(itemsDto);
        }

        [HttpGet("GetItems")]
        public async Task<IActionResult> GetItems([FromQuery] string whsCode)
        {
            if (string.IsNullOrEmpty(whsCode))
            {
                return BadRequest("El código del almacén es obligatorio.");
            }
            try
            {
                List<ItemWhs> items = await _itemRepository.GetItemsByWarehouseAsync(whsCode);
                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener los items: {ex.Message}");
            }
        }
    }
}
