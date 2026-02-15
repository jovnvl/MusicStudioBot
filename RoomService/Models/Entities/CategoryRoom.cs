using System.Formats.Asn1;

namespace RoomService.Models.Entities
{
    public class CategoryRoom
    {
        public int Id { get; }
        public string Name { get; }
        public  string Description { get; }
        public CategoryRoom(int id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }
    }
}
