using AutoMapper;
using Mango.Services.ShoppingCartAPI.Data;
using Mango.Services.ShoppingCartAPI.Models;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection.PortableExecutable;

namespace Mango.Services.ShoppingCartAPI.Controllers
{
    [Route("api/cart")]
    [ApiController]
    public class CartAPIController : ControllerBase
    {
        private ResponseDto _response;
        private IMapper _mapper;
        private readonly AppDbContext _appDbContext;

        public CartAPIController(AppDbContext appDbContext, IMapper mapper)
        {
            _appDbContext = appDbContext;
            _mapper = mapper;
            _response = new ResponseDto();
        }

        [HttpGet("GetCart/{userId")]
        public async Task<ResponseDto> GetCart(string userId)
        {
            try
            {
                CartDto cart = new()
                {
                    CartHeader = _mapper.Map<CartHeaderDto>(_appDbContext.CartHeaders.First(u => u.UserId == userId)),
                };
                cart.CartDetails = _mapper.Map<IEnumerable<CartDetailsDto>>(_appDbContext.CartDetails
                                                .Where(u => u.CartHeaderId == cart.CartHeader.CartHeaderId));

                foreach(var item in cart.CartDetails)
                {
                    cart.CartHeader.CartTotal += (item.Count * item.Product.Price);
                }

                _response.Result = cart;
            }
            catch(Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [HttpPost("CartUpsert")]
        public async Task<ResponseDto> CartUpsert(CartDto cartDto)
        {
            try
            {
                var cartHeaderFromDb = await _appDbContext.CartHeaders.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == cartDto.CartHeader.UserId);

                if (cartHeaderFromDb == null)
                {
                    //create cart header and details
                    CartHeader cartHeader = _mapper.Map<CartHeader>(cartDto.CartHeader);
                    _appDbContext.CartHeaders.Add(cartHeader);
                    await _appDbContext.SaveChangesAsync();

                    cartDto.CartDetails.First().CartHeaderId = cartHeader.CartHeaderId;

                    CartDetails cartDetails = _mapper.Map<CartDetails>(cartDto.CartDetails.First());

                    _appDbContext.CartDetails.Add(cartDetails);
                    await _appDbContext.SaveChangesAsync();
                }
                else
                {
                    //if header is not null, check if details has same product

                    var cartDetailsFromDb = await _appDbContext.CartDetails.AsNoTracking().FirstOrDefaultAsync(
                                                u => u.ProductId == cartDto.CartDetails.First().ProductId && 
                                                u.CartHeaderId == cartHeaderFromDb.CartHeaderId);

                    if(cartDetailsFromDb == null)
                    {
                        //create cartDetails
                        cartDto.CartDetails.First().CartHeaderId = cartHeaderFromDb.CartHeaderId;

                        CartDetails cartDetails = _mapper.Map<CartDetails>(cartDto.CartDetails.First());

                        _appDbContext.CartDetails.Add(cartDetails);
                        await _appDbContext.SaveChangesAsync();
                    }
                    else
                    {
                        //update count in cart details
                        cartDto.CartDetails.First().Count += cartDetailsFromDb.Count;
                        cartDto.CartDetails.First().CartHeaderId = cartDetailsFromDb.CartHeaderId;
                        cartDto.CartDetails.First().CartDetailsId = cartDetailsFromDb.CartDetailsId;

                        CartDetails cartDetails = _mapper.Map<CartDetails>(cartDto.CartDetails.First());
                        _appDbContext.CartDetails.Update(cartDetails);
                        await _appDbContext.SaveChangesAsync();
                    }
                }

                _response.Result = cartDto;
            }
            catch (Exception ex)
            {
                _response.Message = ex.Message.ToString();
                _response.IsSuccess = false;
            }

            return _response;
        }

        [HttpPost("RemoveCart")]
        public async Task<ResponseDto> RemoveCart([FromBody]int cartDetailsId)
        {
            try
            {
                CartDetails cartDetails = _appDbContext.CartDetails.First(u=>u.CartDetailsId == cartDetailsId);

                int totalCountOfCartItem = _appDbContext.CartDetails.Where(u=> u.CartHeaderId == cartDetails.CartHeaderId).Count();
                _appDbContext.CartDetails.Remove(cartDetails);

                if(totalCountOfCartItem == 1)
                {
                    var cartHeaderToRemove = await _appDbContext.CartHeaders
                        .FirstOrDefaultAsync(u=>u.CartHeaderId == cartDetails.CartHeaderId);   
                    
                    _appDbContext.CartHeaders.Remove(cartHeaderToRemove);
                }

                await _appDbContext.SaveChangesAsync();

                _response.Result = true;
            }
            catch (Exception ex)
            {
                _response.Message = ex.Message.ToString();
                _response.IsSuccess = false;
            }

            return _response;
        }
    }
}
