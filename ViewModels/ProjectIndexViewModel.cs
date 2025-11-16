using System;
using System.Collections.Generic;
using Compass.Models;

namespace Compass.ViewModels
{
    public class ProjectIndexViewModel
    {
        public IReadOnlyList<Project> Projects { get; set; } = Array.Empty<Project>();
        public IReadOnlyList<Project> UserProjects { get; set; } = Array.Empty<Project>();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalCount { get; set; }

        public int TotalPages => PageSize > 0
            ? (int)Math.Ceiling(TotalCount / (double)PageSize)
            : 1;

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
