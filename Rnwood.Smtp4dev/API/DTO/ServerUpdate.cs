using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.API.DTO
{
    public class ServerUpdate
    {
        [Required]
        public int id { get; set; }

        [Required]
        public bool isEnabled { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int port { get; set; }
    }
}