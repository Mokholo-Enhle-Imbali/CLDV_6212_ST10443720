using CLVD6212_POE.Models;
using CLVD6212_POE.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace CLVD6212_POE.Controllers
{
    
    public class CustomerController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public CustomerController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }


    
        public async Task<IActionResult> Index()
        {
            var customers= await _storageService.GetAllEntitiesAsync<Customer>();
            return View(customers);
        }


        //Create customer view and http post (funcrional part)
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _storageService.AddEntityAsync(customer);
                    TempData["Sucess Message"] = $"Customer {customer.Username} Created Sucessfully";
                    return RedirectToAction(nameof(Index));
                }

                catch (Exception ex) 
                {
                    ModelState.AddModelError("", "Error creating the customer");
                }
                
            }

            return View(customer);
        }


        //Edit each customer
        public async Task<IActionResult> Edit(string partitionKey, string rowKey) //view
        {

            if (string.IsNullOrEmpty(partitionKey)||string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }


            var customer= await _storageService.GetEntityAsync<Customer>(partitionKey,rowKey);

            if (customer==null)
            {
                return NotFound();
            }

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>Edit(Customer customer) //http post
        {
         

            if (ModelState.IsValid) 
            {
                try
                {
                   
                    await _storageService.UpdateEntityAsync(customer);
                    TempData["Success"] = "Customer Updated Successfuly";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {

                    ModelState.AddModelError("",$"Errror Updating the customer: {ex.Message}");
                    
                }

                
            }

            return View(customer);

        }

        //delete a customer
        public async Task<IActionResult> Delete(string partitionKey, string rowKey) //view
        {
            

            if (string.IsNullOrEmpty(partitionKey)||string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

         var entity= await _storageService.GetEntityAsync<Customer>(partitionKey, rowKey);

            if (entity == null)
            {
                return NotFound();
            }

            return View(entity); 
        }

        [HttpPost,ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAction(string partitionKey,string rowKey) //action
        {
          
            await _storageService.DeleteEntityAsync<Customer>(partitionKey, rowKey);

            TempData["SuccessMessage"] = "Customer deleted successfully";

            return RedirectToAction(nameof(Index));
        }



    }
}
