using System.ComponentModel.DataAnnotations;

namespace Demo.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        [Required(ErrorMessage = "ReviewerName is required.")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "ReviewerName must contain only letters.")]
        public string ReviewerName { get; set; }
        [Required(ErrorMessage = "Comment is required.")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Comment must contain only letters.")]
        public string Comment { get; set; }
        public int Rating { get; set; } // 1 to 5
        public DateTime DatePosted { get; set; }

        public Product? Product { get; set; }
    }

}
