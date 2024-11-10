using AutoMapper;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using WebApi.DTOs;

namespace WebApi.Controllers.VentaController
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        IOrderRepository _respository;
        private readonly IMapper _mapper;
        protected ApiResponse _response;

        public OrderController(IOrderRepository repository, IMapper mapper)
        {
            _respository = repository;
            _mapper = mapper;
            _response = new ApiResponse();
        }

        [HttpGet]
        public async Task<IActionResult> Get(int top = 10, int skip = 0, string search = "")
        {
            var sessionID = Request.Headers["SessionID"];
            var result = search.Length > 0
                ? await _respository.GetForText(sessionID, search)
                : await _respository.GetAll(sessionID, top, skip);

            List<OrderDto> orderDtos = _mapper.Map<List<OrderDto>>(result);
            _response.IsSuccessful = true;
            _response.Resultado = orderDtos;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }
    }
}
