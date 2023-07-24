using Mango.Web.Models;
using Mango.Web.Models.Dto;
using Mango.Web.Service;
using Mango.Web.Service.IService;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Mango.Web.Controllers
{
	public class ProductController : Controller
	{
		private readonly IProductService _productService;
		public ProductController(IProductService productService)
		{
			_productService = productService;
		}
		public async Task<IActionResult> ProductIndex()
		{
			List<ProductDto>? listOfProducts = new();

			ResponseDto? response = await _productService.GetAllProductsAsync();

			if (response != null && response.IsSuccess)
			{
				listOfProducts = JsonConvert.DeserializeObject<List<ProductDto>>(Convert.ToString(response.Result));
			}
			else
			{
				TempData["error"] = response?.Message;
			}

			return View(listOfProducts);
		}
	}
}
