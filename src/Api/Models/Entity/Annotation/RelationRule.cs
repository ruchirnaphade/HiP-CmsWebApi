﻿using System.ComponentModel.DataAnnotations;

namespace PaderbornUniversity.SILab.Hip.CmsApi.Models.Entity.Annotation
{
    public class RelationRule
    {
        [Key]
        public int Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string ArrowStyle { get; set; }

        public string Color { get; set; }
    }
}
