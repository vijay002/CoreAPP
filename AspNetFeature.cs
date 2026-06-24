using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Reva.Domain.Account
{
    [Table(name: "AspNetFeature")]
    public class AspNetFeature
    {
    
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public string Name { get; set; }

        [ForeignKey("ParentFeature")]
        public Guid? ParentFeatureId { get; set; }

        public virtual AspNetFeature ParentFeature { get; set; }

        public string Area { get; set; }

        public string ControllerName { get; set; }

        public string ActionName { get; set; }

        public bool IsExternal { get; set; }

        public string ExternalUrl { get; set; }

        public int DisplayOrder { get; set; }

        [NotMapped]
        public bool Permitted { get; set; }

        public bool Visible { get; set; }
        public bool Full { get; set; }
        public bool Add { get; set; }
        public bool Edit { get; set; }
        public bool Delete { get; set; }

    }
}
