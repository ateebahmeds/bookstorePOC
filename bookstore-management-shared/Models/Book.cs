namespace bookstore_management_shared.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public int AuthorId { get; set; }
        public Author? Author { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }
}
