namespace Compass.Models.Docs;

/// <summary>Functional specification for a Modern COMPASS view or process.</summary>
public sealed record DocsViewSpecification(
    string Id,
    string Title,
    string ProcessName,
    string Route,
    string WhatItDoes,
    string DataUsed,
    string Permissions,
    string DataUsage,
    string ApiAvailability);

/// <summary>Grouped functional specifications (Work, RAID, Reporting, etc.).</summary>
public sealed record DocsSpecificationArea(
    string Id,
    string Title,
    string Introduction,
    IReadOnlyList<DocsViewSpecification> Views);
