namespace bookstore_management_shared.Models
{
    public class Author
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<Book>? Books { get; set; }
    }
}
