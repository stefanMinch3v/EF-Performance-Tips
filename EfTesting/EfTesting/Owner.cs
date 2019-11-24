namespace EfTesting
{
    using System.Collections.Generic;

    public class Owner
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }

        public ICollection<Cat> Cats { get; set; } = new List<Cat>();
    }
}
