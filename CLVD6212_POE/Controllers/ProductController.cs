using CLVD6212_POE.Models;
using CLVD6212_POE.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace CLVD6212_POE.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IAzureStorageService storageService, ILogger<ProductController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }



        public async Task<IActionResult> Index()
        {
            var products = await _storageService.GetAllEntitiesAsync<Product>();
            return View(products);
        }


        //create a product
        public IActionResult Create() //view
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? formFile) //action
        {

            if (ModelState.IsValid)
            {
                try
                {

                    if (formFile!=null && formFile.Length>0)
                    {
                        string filename = Guid.NewGuid().ToString() + Path.GetExtension(formFile.FileName);
                        string imageUrl = await _storageService.UploadFileAsync(formFile, "product-images");
                        product.ImageUrl = imageUrl;
                    }



                    await _storageService.AddEntityAsync(product);
                    TempData["Sucess Message"] = $"Product {product.ProductName} Created Sucessfully";
                    return RedirectToAction(nameof(Index));
                }

                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product");
                    ModelState.AddModelError("", $"Error creating the product {product.ProductName}");
                }

            }

            return View(product);
        }



        //delete a product
        public async Task<IActionResult> Delete(string partitionKey, string rowKey) //get the view
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            var entity = await _storageService.GetEntityAsync<Product>("Product", rowKey);

            if (entity == null)
            {
                return NotFound();
            }

            return View(entity);
        }

        [HttpPost,ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAction(string partitionKey, string rowKey) //action
        {
            try
            {
                await _storageService.DeleteEntityAsync<Product>(partitionKey, rowKey);
                TempData["Success"] = "Product Deleted Successfully";
            }
            catch (Exception ex)
            {

                TempData["Error"] = $"Error Deleting the product {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }



        //edit a product
        public async Task<IActionResult> Edit(string partitionKey,string rowKey)  //view
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            var product = await _storageService.GetEntityAsync<Product>(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }


            return View(product);

        }

        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAction(Product product, IFormFile? formFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                   
                    var originalProduct = await _storageService.GetEntityAsync<Product>(product.PartitionKey, product.RowKey);

                    if (originalProduct == null)
                    {
                        ModelState.AddModelError("", "Product not found in database");
                        return View(product);
                    }

                    if (formFile != null && formFile.Length > 0)
                    {
                        
                        string imageUrl = await _storageService.UploadFileAsync(formFile, "product-images");
                        product.ImageUrl = imageUrl;
                    }
                    else
                    {
                        
                        product.ImageUrl = originalProduct.ImageUrl;
                    }

                   
                    product.PartitionKey = originalProduct.PartitionKey;
                    product.RowKey = originalProduct.RowKey;
                    product.ETag = originalProduct.ETag;

                    await _storageService.UpdateEntityAsync(product);
                    TempData["Success"] = "Product Updated Successfully"; 
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error Updating the product: {ex.Message}");
                }
            }

            return View(product);
        }
    }
}
