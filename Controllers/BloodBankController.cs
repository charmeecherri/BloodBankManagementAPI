using BloodBankManagement.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BloodBankManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BloodBankController : ControllerBase
    {
        private static List<BloodBankEntry> bloodBankEntries=new List<BloodBankEntry>();
        private static int currentId = 1;

        //Create (POST/api/bloodbank)
        [HttpPost]
        public IActionResult Create([FromBody] BloodBankEntry entry)
        {
            if (entry == null) return BadRequest("Entry cannot be null");
            if (string.IsNullOrWhiteSpace(entry.DonorName)) return BadRequest("Donor name is required");
            if (string.IsNullOrWhiteSpace(entry.BloodType) || !IsValidBloodType(entry.BloodType))
                return BadRequest("Invalid blood type. Accepted types: A+, A-, B+, B-, AB+, AB-, O+, O-.");
            if (entry.Quantity <= 0)
                return BadRequest("Quantity must be greater than zero.");
            if (entry.CollectionDate == default || entry.ExpirationDate == default)
                return BadRequest("Collection and expiration dates are required.");

            if (entry.ExpirationDate <= entry.CollectionDate)
                return BadRequest("Expiration date must be after the collection date.");
            if (!Enum.TryParse(typeof(DonationStatus), entry.Status, true, out _))
                return BadRequest("Invalid status. Valid statuses are: Available, Expired, Reserved, Used.");



            entry.Id = currentId++;
            bloodBankEntries.Add(entry);
            return CreatedAtAction(nameof(GetById), new { id = entry.Id }, entry);
        }
        private bool IsValidBloodType(string bloodType)
        {
            var validBloodTypes = new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
            return validBloodTypes.Contains(bloodType);
        }

        // READ ALL (GET /api/bloodbank)
        [HttpGet("all")]
       
        public IActionResult GetAll([FromQuery] string? sortBy=null)
        {
            var results = bloodBankEntries.AsQueryable();

            // Sorting logic based on 'sortBy' query parameter
            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "bloodtype":
                        results = results.OrderBy(e => e.BloodType);
                        break;
                    case "collectiondate":
                        results = results.OrderBy(e => e.CollectionDate);
                        break;
                    default:
                        return BadRequest("Invalid sort parameter.");
                }
            }

            return Ok(results.ToList());
        }


        // // READ BY ID (GET /api/bloodbank/{id})

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var entry = bloodBankEntries.FirstOrDefault(e => e.Id == id);
            if (entry == null)
                return NotFound();
            return Ok(entry);
        }
        // UPDATE(PUT /api/bloodbank/{ id})
        [HttpPut("updateby{id}")]
        public IActionResult Update(int id, [FromBody] BloodBankEntry updatedEntry)
        {
            // Find the existing entry by ID
            var entry = bloodBankEntries.FirstOrDefault(e => e.Id == id);
            if (entry == null)
                return NotFound();

            // Validate the updated entry
            if (string.IsNullOrWhiteSpace(updatedEntry.DonorName))
                return BadRequest("Donor name is required.");
            if (string.IsNullOrWhiteSpace(updatedEntry.BloodType) || !IsValidBloodType(updatedEntry.BloodType))
                return BadRequest("Invalid blood type.");
            if (updatedEntry.ExpirationDate <= updatedEntry.CollectionDate)
                return BadRequest("Expiration date must be later than collection date.");
            if (!Enum.TryParse(typeof(DonationStatus), updatedEntry.Status, true, out _))
                return BadRequest("Invalid status.");

            // Update the fields of the entry
            entry.DonorName = updatedEntry.DonorName;
            entry.Age = updatedEntry.Age;
            entry.BloodType = updatedEntry.BloodType;
            entry.ContactInfo = updatedEntry.ContactInfo;
            entry.Quantity = updatedEntry.Quantity;
            entry.CollectionDate = updatedEntry.CollectionDate;
            entry.ExpirationDate = updatedEntry.ExpirationDate;
            entry.Status = updatedEntry.Status;

            
            return NoContent();// Return 204 No Content
        }

        // DELETE (DELETE /api/bloodbank/{id})
        [HttpDelete("deleteby{id}")]
        public IActionResult Delete(int id)
        {
            var entry = bloodBankEntries.FirstOrDefault(e => e.Id == id);
            if (entry == null)
                return NotFound();

            bloodBankEntries.Remove(entry);
            Console.WriteLine($"Entry with ID {id} was deleted.");
            return NoContent();
        }
        //Pagination
        [HttpGet("paged")]
        public IActionResult GetPaged(int page = 1, int size = 10)
        {
            if (page <= 0 || size <= 0)
                return BadRequest("Page and size parameters must be greater than zero.");

            var totalEntries = bloodBankEntries.Count;
            var totalPages = (int)Math.Ceiling(totalEntries / (double)size);

            if ((page - 1) * size >= totalEntries)
            {
                return Ok(new
                {
                    totalEntries,
                    totalPages,
                    currentPage = page,
                    pageSize = size,
                    entries = new List<BloodBankEntry>()
                });
            }

            var pagedEntries = bloodBankEntries
                .Skip((page - 1) * size)
                .Take(size)
                .ToList();

            var response = new
            {
                totalEntries,
                totalPages,
                currentPage = page,
                pageSize = size,
                entries = pagedEntries
            };

            return Ok(response);

        }


        //search Functionality

        [HttpGet("search")]
        public IActionResult Search([FromQuery] string? bloodType, [FromQuery] string? status, [FromQuery] string? donorName)
        {
            var results = bloodBankEntries.AsQueryable();

            // Filter by blood type
            if (!string.IsNullOrEmpty(bloodType))
                results = results.Where(e => e.BloodType.Equals(bloodType, StringComparison.OrdinalIgnoreCase));

            // Filter by status (with validation)
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<DonationStatus>(status, true, out var validStatus))
                results = results.Where(e => e.Status.Equals(validStatus.ToString(), StringComparison.OrdinalIgnoreCase));
            else if (!string.IsNullOrEmpty(status))
                return BadRequest(new { Error = "Invalid status value provided." });

            // Filter by donor name
            if (!string.IsNullOrEmpty(donorName))
                results = results.Where(e => e.DonorName.Contains(donorName, StringComparison.OrdinalIgnoreCase));

            // Return results
            var filteredResults = results.ToList();
            return Ok(new
            {
                TotalResults = filteredResults.Count,
                Entries = filteredResults
            });
        }










    }
}



