using Microsoft.AspNetCore.Identity;

namespace DMS.Core.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public ICollection<Document> Documents { get; set; }
    }
}