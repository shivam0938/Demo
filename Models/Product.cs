using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Demo.Models
{
    public class Product
    {
       
        public int ProductId { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Stock Quantity must be greater than 0.")]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Category must contain only letters.")]
        public string Category { get; set; }

        public virtual ICollection<ProductImage>? Images { get; set; }

        public ICollection<Review>? Reviews { get; set; }
        [NotMapped]
        public IList<SelectListItem>? Ratings { get; set; }
        [NotMapped]
        public Review? Review { get; set; }
        public Guid CustomerId { get; set; }
    }
}
