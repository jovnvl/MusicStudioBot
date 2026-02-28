using System.Formats.Asn1;

namespace BookingService.Models.Entities
{
    public class CategoryRoom
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public  string Description { get; set; }
        public CategoryRoom(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
