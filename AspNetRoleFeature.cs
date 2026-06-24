using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Reva.Domain.Account
{
    [Table(name: "AspNetRoleFeature")]
    public class AspNetRoleFeature
    {
        public string RoleId { get; set; }

        public Guid AspNetFeatureId { get; set; }



        public bool Visible { get; set; }
        public bool Full { get; set; }
        public bool Add { get; set; }
        public bool Edit { get; set; }
        public bool Delete { get; set; }

        public AspNetFeature AspNetFeature { get; set; }
    }
}
