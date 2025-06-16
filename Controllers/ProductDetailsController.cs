using Demo.Data;
using Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Demo.Controllers
{
    public class ProductDetailsController : Controller
    {
        protected readonly ApplicationDbContext _context;
        public ProductDetailsController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(int? pageNumber)
        {

            var products = from p in _context.Products.Include(p => p.Images)
                           select p;


            int pageSize = 3;
            return View(await PaginatedList<Product>.CreateAsync(products.AsNoTracking(), pageNumber ?? 1, pageSize));

        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fullProduct = await GetFullProductModelAsync(id.Value);
            if (fullProduct == null)
                return NotFound();

            return View(fullProduct);
        }

        [HttpPost]
        public async Task<IActionResult> AddReview(Product model)
        {

            // Clear validation errors for the outer model
            ModelState.Clear();

            // Only validate the Review submodel
            TryValidateModel(model.Review);

            if (!ModelState.IsValid)
            {
                var fullProduct = await GetFullProductModelAsync(model.ProductId);
                if (fullProduct == null)
                    return NotFound();

                fullProduct.Review = model.Review;

                // Return the view with validation messages for Review only
                return View("Details", fullProduct);
            }

            // Save logic
            var review = new Review
            {
                ProductId = model.ProductId,
                ReviewerName = model.Review.ReviewerName,
                Rating = model.Review.Rating,
                Comment = model.Review.Comment,
                DatePosted = DateTime.Now
            };

            _context.Reviews.Add(review);
            _context.SaveChanges();


            return RedirectToAction("Details", "ProductDetails", new { id = model.ProductId });
        }


        private async Task<Product> GetFullProductModelAsync(int productId)
        {
             var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
                return null;

            var ratings = new List<SelectListItem>();
            ratings.Add(new SelectListItem { Text = "1 - Terrible", Value = "1" });
            ratings.Add(new SelectListItem { Text = "2 - Poor", Value = "2" });
            ratings.Add(new SelectListItem { Text = "3 - Average", Value = "3" });
            ratings.Add(new SelectListItem { Text = "4 - Good", Value = "4" });
            ratings.Add(new SelectListItem { Text = "5 - Excellent", Value = "5" });

            product.Ratings = ratings;
            product.Review = new Review();
            return product;
        }

    }
}
