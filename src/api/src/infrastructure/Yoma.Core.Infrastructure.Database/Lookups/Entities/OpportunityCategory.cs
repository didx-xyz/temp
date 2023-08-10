﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yoma.Core.Infrastructure.Database.Core.Entities;

namespace Yoma.Core.Infrastructure.Database.Lookups.Entities
{
    [Table("OpportunityCategory", Schema = "lookup")]
    [Index(nameof(Name), IsUnique = true)]
    public class OpportunityCategory : BaseEntity<Guid>
    {
        [Column(TypeName = "varchar(125)")]
        public string Name { get; set; }

        [Required]
        public DateTimeOffset DateCreated { get; set; }
    }
}
