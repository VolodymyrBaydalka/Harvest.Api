using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public abstract class BaseModel
    {
        public long Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
