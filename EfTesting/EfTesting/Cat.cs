namespace EfTesting
{
    public class Cat
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Color { get; set; }

        public int Age { get; set; }

        public int OwnerId { get; set; }

        public Owner Owner { get; set; }
    }
}
