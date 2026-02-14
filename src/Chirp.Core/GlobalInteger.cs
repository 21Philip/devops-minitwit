using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Chirp.Core
{

    /// <summary>
    /// A global integer identified by a key
    /// </summary>
    public class GlobalInteger
    {
        [Key]
        public required string Key { get; set; }
        public required int Value { get; set; }
    }
}