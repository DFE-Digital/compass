using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.EnvironmentSync;

public sealed partial class EnvironmentSyncService
{
  private async Task<IReadOnlyList<EnvironmentSyncCountLine>> PreviewServiceRegisterAsync(
    CompassDbContext source,
    CompassDbContext target,
    CancellationToken cancellationToken)
  {
    var sourceProducts = await source.CMDBProducts.AsNoTracking()
      .Where(p => !string.IsNullOrWhiteSpace(p.CMDBID))
      .CountAsync(cancellationToken);
    var targetProducts = await target.CMDBProducts.AsNoTracking()
      .Where(p => !string.IsNullOrWhiteSpace(p.CMDBID))
      .CountAsync(cancellationToken);
    var targetByCmdb = await target.CMDBProducts.AsNoTracking()
      .Where(p => !string.IsNullOrWhiteSpace(p.CMDBID))
      .Select(p => p.CMDBID!.Trim().ToLower())
      .ToListAsync(cancellationToken);
    var targetSet = targetByCmdb.ToHashSet(StringComparer.OrdinalIgnoreCase);
    var sourceCmdbIds = await source.CMDBProducts.AsNoTracking()
      .Where(p => !string.IsNullOrWhiteSpace(p.CMDBID))
      .Select(p => p.CMDBID!.Trim())
      .ToListAsync(cancellationToken);
    var wouldCreate = sourceCmdbIds.Count(id => !targetSet.Contains(id));
    var wouldUpdate = sourceCmdbIds.Count - wouldCreate;

    return
    [
      Line("FIPS channels", await source.FipsChannels.CountAsync(cancellationToken), await target.FipsChannels.CountAsync(cancellationToken)),
      Line("FIPS types", await source.FipsTypes.CountAsync(cancellationToken), await target.FipsTypes.CountAsync(cancellationToken)),
      Line("FIPS business areas", await source.FipsBusinessAreas.CountAsync(cancellationToken), await target.FipsBusinessAreas.CountAsync(cancellationToken)),
      Line("FIPS user groups", await source.FipsUserGroups.CountAsync(cancellationToken), await target.FipsUserGroups.CountAsync(cancellationToken)),
      Line("FIPS contact roles", await source.FipsContactRoles.CountAsync(cancellationToken), await target.FipsContactRoles.CountAsync(cancellationToken)),
      Line("FIPS categorisation groups", await source.FipsCategorisationGroups.CountAsync(cancellationToken), await target.FipsCategorisationGroups.CountAsync(cancellationToken)),
      Line("Service register products (CMDB ID)", sourceProducts, targetProducts, wouldCreate, wouldUpdate),
      Line("Service lines", await source.ServiceLines.CountAsync(cancellationToken), await target.ServiceLines.CountAsync(cancellationToken))
    ];
  }

  private async Task<EnvironmentSyncResult> SyncServiceRegisterAsync(
    CompassDbContext source,
    CompassDbContext target,
    string sourceCatalog,
    string targetCatalog,
    bool dryRun,
    CancellationToken cancellationToken)
  {
    var messages = new List<string>();
    var errors = new List<string>();
    var created = 0;
    var updated = 0;
    var skipped = 0;

    var phaseMap = await SyncNamedLookupsAsync(
      source.PhaseLookups.AsNoTracking(),
      target,
      target.PhaseLookups,
      x => x.Name,
      (src, existing) =>
      {
        existing.Description = src.Description;
        existing.SortOrder = src.SortOrder;
        existing.IsActive = src.IsActive;
      },
      src => new PhaseLookup
      {
        Name = src.Name,
        Description = src.Description,
        SortOrder = src.SortOrder,
        IsActive = src.IsActive
      },
      dryRun,
      cancellationToken);

    var businessAreaLookupMap = await SyncNamedLookupsAsync(
      source.BusinessAreaLookups.AsNoTracking(),
      target,
      target.BusinessAreaLookups,
      x => x.Name,
      (src, existing) =>
      {
        existing.Description = src.Description;
        existing.SortOrder = src.SortOrder;
        existing.IsActive = src.IsActive;
      },
      src => new BusinessAreaLookup
      {
        Name = src.Name,
        Description = src.Description,
        SortOrder = src.SortOrder,
        IsActive = src.IsActive
      },
      dryRun,
      cancellationToken);

    var channelMap = await SyncNamedLookupsAsync(
      source.FipsChannels.AsNoTracking(),
      target,
      target.FipsChannels,
      x => x.Name,
      (src, existing) =>
      {
        existing.Description = src.Description;
        existing.DisplayOrder = src.DisplayOrder;
        existing.Active = src.Active;
      },
      src => new FipsChannel
      {
        Name = src.Name,
        Description = src.Description,
        DisplayOrder = src.DisplayOrder,
        Active = src.Active
      },
      dryRun,
      cancellationToken);

    var typeMap = await SyncNamedLookupsAsync(
      source.FipsTypes.AsNoTracking(),
      target,
      target.FipsTypes,
      x => x.Name,
      (src, existing) =>
      {
        existing.Description = src.Description;
        existing.DisplayOrder = src.DisplayOrder;
        existing.Active = src.Active;
      },
      src => new FipsType
      {
        Name = src.Name,
        Description = src.Description,
        DisplayOrder = src.DisplayOrder,
        Active = src.Active
      },
      dryRun,
      cancellationToken);

    var contactRoleMap = await SyncNamedLookupsAsync(
      source.FipsContactRoles.AsNoTracking(),
      target,
      target.FipsContactRoles,
      x => x.Name,
      (src, existing) =>
      {
        existing.Description = src.Description;
        existing.AllowMultiple = src.AllowMultiple;
        existing.DisplayOrder = src.DisplayOrder;
        existing.Active = src.Active;
      },
      src => new FipsContactRole
      {
        Name = src.Name,
        Description = src.Description,
        AllowMultiple = src.AllowMultiple,
        DisplayOrder = src.DisplayOrder,
        Active = src.Active
      },
      dryRun,
      cancellationToken);

    await SyncFipsBusinessAreasAsync(source, target, businessAreaLookupMap, dryRun, cancellationToken);
    var userGroupMap = await SyncFipsUserGroupsAsync(source, target, dryRun, cancellationToken);
    var categorisationItemMap = await SyncFipsCategorisationAsync(source, target, dryRun, cancellationToken);

    var sourceProducts = await source.CMDBProducts
      .Include(p => p.BusinessAreas)
      .Include(p => p.Channels)
      .Include(p => p.UserGroups)
      .Include(p => p.Types)
      .Include(p => p.CategorisationItems)
      .Include(p => p.Contacts)
      .AsNoTracking()
      .Where(p => !string.IsNullOrWhiteSpace(p.CMDBID))
      .ToListAsync(cancellationToken);

    var targetProducts = await target.CMDBProducts
      .Include(p => p.BusinessAreas)
      .Include(p => p.Channels)
      .Include(p => p.UserGroups)
      .Include(p => p.Types)
      .Include(p => p.CategorisationItems)
      .Include(p => p.Contacts)
      .Where(p => !string.IsNullOrWhiteSpace(p.CMDBID))
      .ToListAsync(cancellationToken);

    var targetByCmdb = targetProducts
      .GroupBy(p => p.CMDBID!.Trim(), StringComparer.OrdinalIgnoreCase)
      .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

  foreach (var src in sourceProducts)
    {
      var cmdbId = src.CMDBID!.Trim();
      if (!targetByCmdb.TryGetValue(cmdbId, out var existing))
      {
        if (dryRun)
        {
          created++;
          continue;
        }

        var product = new CMDBProduct
        {
          Id = src.Id,
          Title = src.Title,
          CMDBDescription = src.CMDBDescription,
          UserDescription = src.UserDescription,
          CMDBID = cmdbId,
          ProductURL = src.ProductURL,
          Status = src.Status,
          IsEnterpriseService = src.IsEnterpriseService,
          LastCmdbSnapshotJson = src.LastCmdbSnapshotJson,
          PhaseId = RemapNullable(src.PhaseId, phaseMap),
          CreatedAt = src.CreatedAt,
          CreatedBy = src.CreatedBy,
          UpdatedAt = DateTime.UtcNow,
          UpdatedBy = src.UpdatedBy
        };
        ApplyProductJunctions(product, src, channelMap, typeMap, userGroupMap, categorisationItemMap, contactRoleMap, errors);
        target.CMDBProducts.Add(product);
        targetByCmdb[cmdbId] = product;
        created++;
      }
      else
      {
        var changed =
          existing.Title != src.Title
          || existing.CMDBDescription != src.CMDBDescription
          || existing.UserDescription != src.UserDescription
          || existing.ProductURL != src.ProductURL
          || existing.Status != src.Status
          || existing.IsEnterpriseService != src.IsEnterpriseService
          || existing.LastCmdbSnapshotJson != src.LastCmdbSnapshotJson
          || existing.PhaseId != RemapNullable(src.PhaseId, phaseMap)
          || !JunctionSetEquals(existing.Channels, src.Channels, channelMap, x => x.FipsChannelId, x => x.FipsChannelId)
          || !JunctionSetEquals(existing.Types, src.Types, typeMap, x => x.FipsTypeId, x => x.FipsTypeId)
          || !JunctionSetEquals(existing.UserGroups, src.UserGroups, userGroupMap, x => x.FipsUserGroupId, x => x.FipsUserGroupId)
          || !JunctionSetEquals(existing.CategorisationItems, src.CategorisationItems, categorisationItemMap, x => x.FipsCategorisationItemId, x => x.FipsCategorisationItemId);

        if (!changed && ContactsEqual(existing.Contacts, src.Contacts, contactRoleMap))
        {
          skipped++;
          continue;
        }

        if (dryRun)
        {
          updated++;
          continue;
        }

        existing.Title = src.Title;
        existing.CMDBDescription = src.CMDBDescription;
        existing.UserDescription = src.UserDescription;
        existing.ProductURL = src.ProductURL;
        existing.Status = src.Status;
        existing.IsEnterpriseService = src.IsEnterpriseService;
        existing.LastCmdbSnapshotJson = src.LastCmdbSnapshotJson;
        existing.PhaseId = RemapNullable(src.PhaseId, phaseMap);
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = src.UpdatedBy;

        target.CMDBProductBusinessAreas.RemoveRange(existing.BusinessAreas);
        target.CMDBProductChannels.RemoveRange(existing.Channels);
        target.CMDBProductUserGroups.RemoveRange(existing.UserGroups);
        target.CMDBProductTypes.RemoveRange(existing.Types);
        target.CMDBProductFipsCategorisationItems.RemoveRange(existing.CategorisationItems);
        target.CMDBProductContacts.RemoveRange(existing.Contacts);
        existing.BusinessAreas.Clear();
        existing.Channels.Clear();
        existing.UserGroups.Clear();
        existing.Types.Clear();
        existing.CategorisationItems.Clear();
        existing.Contacts.Clear();
        ApplyProductJunctions(existing, src, channelMap, typeMap, userGroupMap, categorisationItemMap, contactRoleMap, errors);
        updated++;
      }
    }

    if (!dryRun)
      await target.SaveChangesAsync(cancellationToken);

    var serviceLineResult = await SyncServiceLinesAsync(source, target, dryRun, cancellationToken);
    created += serviceLineResult.Created;
    updated += serviceLineResult.Updated;
    skipped += serviceLineResult.Skipped;
    messages.AddRange(serviceLineResult.Messages);

    messages.Add($"Service register sync from {sourceCatalog} to {targetCatalog} completed.");
    messages.Add($"Products: {created} created, {updated} updated, {skipped} unchanged.");
    if (dryRun)
      messages.Insert(0, "Dry run — no changes were saved.");

    return new EnvironmentSyncResult
    {
      Success = errors.Count == 0,
      DryRun = dryRun,
      Direction = EnvironmentSyncDirection.DevToProdServiceRegister,
      SourceCatalog = sourceCatalog,
      TargetCatalog = targetCatalog,
      Messages = messages,
      Errors = errors,
      Created = created,
      Updated = updated,
      Skipped = skipped
    };
  }

  private async Task<(int Created, int Updated, int Skipped, List<string> Messages)> SyncServiceLinesAsync(
    CompassDbContext source,
    CompassDbContext target,
    bool dryRun,
    CancellationToken cancellationToken)
  {
    var messages = new List<string>();
    var created = 0;
    var updated = 0;
    var skipped = 0;

    var sourceLines = await source.ServiceLines.AsNoTracking()
      .Include(s => s.ServiceLineDivisions)
      .Include(s => s.ServiceLineBusinessAreas)
      .Include(s => s.ServiceLineProducts)
      .Include(s => s.ServiceLineProjects)
      .ToListAsync(cancellationToken);

    var targetLines = await target.ServiceLines
      .Include(s => s.ServiceLineDivisions)
      .Include(s => s.ServiceLineBusinessAreas)
      .Include(s => s.ServiceLineProducts)
      .Include(s => s.ServiceLineProjects)
      .ToListAsync(cancellationToken);

    var targetBySlug = targetLines.ToDictionary(s => s.Slug.Trim(), StringComparer.OrdinalIgnoreCase);
    var businessAreaByName = await target.BusinessAreaLookups.AsNoTracking()
      .ToDictionaryAsync(x => x.Name.Trim(), x => x.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
    var divisionByName = await target.Divisions.AsNoTracking()
      .ToDictionaryAsync(x => x.Name.Trim(), x => x.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
    var productByCmdb = await target.CMDBProducts.AsNoTracking()
      .Where(p => !string.IsNullOrWhiteSpace(p.CMDBID))
      .ToDictionaryAsync(p => p.CMDBID!.Trim(), p => p.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
    var projectByCode = await target.Projects.AsNoTracking()
      .Where(p => !p.IsDeleted && !string.IsNullOrWhiteSpace(p.ProjectCode))
      .ToDictionaryAsync(p => p.ProjectCode.Trim(), p => p.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

    var sourceDivisionNames = await source.Divisions.AsNoTracking()
      .ToDictionaryAsync(x => x.Id, x => x.Name.Trim(), cancellationToken);
    var sourceBusinessAreaNames = await source.BusinessAreaLookups.AsNoTracking()
      .ToDictionaryAsync(x => x.Id, x => x.Name.Trim(), cancellationToken);
    var sourceProductCmdb = await source.CMDBProducts.AsNoTracking()
      .Where(p => !string.IsNullOrWhiteSpace(p.CMDBID))
      .ToDictionaryAsync(p => p.Id, p => p.CMDBID!.Trim(), cancellationToken);
    var sourceProjectCodes = await source.Projects.AsNoTracking()
      .Where(p => !p.IsDeleted && !string.IsNullOrWhiteSpace(p.ProjectCode))
      .ToDictionaryAsync(p => p.Id, p => p.ProjectCode.Trim(), cancellationToken);

    foreach (var src in sourceLines)
    {
      if (!targetBySlug.TryGetValue(src.Slug.Trim(), out var existing))
      {
        if (dryRun) { created++; continue; }

        var line = new ServiceLine
        {
          Id = src.Id,
          Name = src.Name,
          Slug = src.Slug,
          Description = src.Description,
          CreatedAt = src.CreatedAt,
          UpdatedAt = DateTime.UtcNow
        };
        AddServiceLineLinks(line, src, sourceDivisionNames, divisionByName, sourceBusinessAreaNames, businessAreaByName,
          sourceProductCmdb, productByCmdb, sourceProjectCodes, projectByCode);
        target.ServiceLines.Add(line);
        created++;
      }
      else
      {
        if (dryRun) { updated++; continue; }

        existing.Name = src.Name;
        existing.Description = src.Description;
        existing.UpdatedAt = DateTime.UtcNow;
        target.ServiceLineDivisions.RemoveRange(existing.ServiceLineDivisions);
        target.ServiceLineBusinessAreas.RemoveRange(existing.ServiceLineBusinessAreas);
        target.ServiceLineProducts.RemoveRange(existing.ServiceLineProducts);
        target.ServiceLineProjects.RemoveRange(existing.ServiceLineProjects);
        existing.ServiceLineDivisions.Clear();
        existing.ServiceLineBusinessAreas.Clear();
        existing.ServiceLineProducts.Clear();
        existing.ServiceLineProjects.Clear();
        AddServiceLineLinks(existing, src, sourceDivisionNames, divisionByName, sourceBusinessAreaNames, businessAreaByName,
          sourceProductCmdb, productByCmdb, sourceProjectCodes, projectByCode);
        updated++;
      }
    }

    if (!dryRun)
      await target.SaveChangesAsync(cancellationToken);

    messages.Add($"Service lines: {created} created, {updated} updated, {skipped} skipped.");
    return (created, updated, skipped, messages);
  }

  private static void AddServiceLineLinks(
    ServiceLine targetLine,
    ServiceLine sourceLine,
    Dictionary<int, string> sourceDivisionNames,
    Dictionary<string, int> targetDivisionByName,
    Dictionary<int, string> sourceBusinessAreaNames,
    Dictionary<string, int> targetBusinessAreaByName,
    Dictionary<Guid, string> sourceProductCmdb,
    Dictionary<string, Guid> targetProductByCmdb,
    Dictionary<int, string> sourceProjectCodes,
    Dictionary<string, int> targetProjectByCode)
  {
    foreach (var div in sourceLine.ServiceLineDivisions)
    {
      if (!sourceDivisionNames.TryGetValue(div.DivisionId, out var name))
        continue;
      if (!targetDivisionByName.TryGetValue(name, out var targetId))
        continue;
      targetLine.ServiceLineDivisions.Add(new ServiceLineDivision
      {
        ServiceLineId = targetLine.Id,
        DivisionId = targetId
      });
    }

    foreach (var ba in sourceLine.ServiceLineBusinessAreas)
    {
      if (!sourceBusinessAreaNames.TryGetValue(ba.BusinessAreaLookupId, out var name))
        continue;
      if (!targetBusinessAreaByName.TryGetValue(name, out var targetId))
        continue;
      targetLine.ServiceLineBusinessAreas.Add(new ServiceLineBusinessArea
      {
        ServiceLineId = targetLine.Id,
        BusinessAreaLookupId = targetId
      });
    }

    foreach (var prod in sourceLine.ServiceLineProducts)
    {
      if (!sourceProductCmdb.TryGetValue(prod.CMDBProductId, out var cmdb))
        continue;
      if (!targetProductByCmdb.TryGetValue(cmdb, out var targetProductId))
        continue;
      targetLine.ServiceLineProducts.Add(new ServiceLineProduct
      {
        ServiceLineId = targetLine.Id,
        CMDBProductId = targetProductId
      });
    }

    foreach (var proj in sourceLine.ServiceLineProjects)
    {
      if (!sourceProjectCodes.TryGetValue(proj.ProjectId, out var code))
        continue;
      if (!targetProjectByCode.TryGetValue(code, out var targetProjectId))
        continue;
      targetLine.ServiceLineProjects.Add(new ServiceLineProject
      {
        ServiceLineId = targetLine.Id,
        ProjectId = targetProjectId
      });
    }
  }

  private async Task SyncFipsBusinessAreasAsync(
    CompassDbContext source,
    CompassDbContext target,
    Dictionary<int, int> businessAreaLookupMap,
    bool dryRun,
    CancellationToken cancellationToken)
  {
    var sourceRows = await source.FipsBusinessAreas.AsNoTracking().ToListAsync(cancellationToken);
    var targetRows = await target.FipsBusinessAreas.ToListAsync(cancellationToken);
    var targetByName = targetRows.ToDictionary(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase);

    foreach (var src in sourceRows)
    {
      int? lookupId = src.BusinessAreaLookupId.HasValue && businessAreaLookupMap.TryGetValue(src.BusinessAreaLookupId.Value, out var mapped)
        ? mapped
        : src.BusinessAreaLookupId;

      if (targetByName.TryGetValue(src.Name.Trim(), out var existing))
      {
        if (dryRun) continue;
        existing.BusinessAreaLookupId = lookupId;
        existing.Description = src.Description;
        existing.DisplayOrder = src.DisplayOrder;
        existing.Active = src.Active;
      }
      else if (!dryRun)
      {
        target.FipsBusinessAreas.Add(new FipsBusinessArea
        {
          Name = src.Name,
          BusinessAreaLookupId = lookupId,
          Description = src.Description,
          DisplayOrder = src.DisplayOrder,
          Active = src.Active
        });
      }
    }

    if (!dryRun)
      await target.SaveChangesAsync(cancellationToken);
  }

  private async Task<Dictionary<int, int>> SyncFipsUserGroupsAsync(
    CompassDbContext source,
    CompassDbContext target,
    bool dryRun,
    CancellationToken cancellationToken)
  {
    var map = new Dictionary<int, int>();
    var sourceRows = await source.FipsUserGroups.AsNoTracking()
      .OrderBy(x => x.ParentId == null ? 0 : 1)
      .ThenBy(x => x.DisplayOrder)
      .ToListAsync(cancellationToken);
    var targetRows = await target.FipsUserGroups.ToListAsync(cancellationToken);
    var targetByName = targetRows.ToDictionary(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase);

    foreach (var src in sourceRows)
    {
      int? parentId = null;
      if (src.ParentId.HasValue && map.TryGetValue(src.ParentId.Value, out var mappedParent))
        parentId = mappedParent;

      if (targetByName.TryGetValue(src.Name.Trim(), out var existing))
      {
        map[src.Id] = existing.Id;
        if (dryRun) continue;
        existing.Description = src.Description;
        existing.DisplayOrder = src.DisplayOrder;
        existing.Active = src.Active;
        existing.ParentId = parentId;
      }
      else if (!dryRun)
      {
        var row = new FipsUserGroup
        {
          Name = src.Name,
          Description = src.Description,
          DisplayOrder = src.DisplayOrder,
          Active = src.Active,
          ParentId = parentId
        };
        target.FipsUserGroups.Add(row);
        await target.SaveChangesAsync(cancellationToken);
        map[src.Id] = row.Id;
        targetByName[src.Name.Trim()] = row;
      }
    }

    if (!dryRun)
      await target.SaveChangesAsync(cancellationToken);

    return map;
  }

  private async Task<Dictionary<int, int>> SyncFipsCategorisationAsync(
    CompassDbContext source,
    CompassDbContext target,
    bool dryRun,
    CancellationToken cancellationToken)
  {
    var itemMap = new Dictionary<int, int>();
    var sourceGroups = await source.FipsCategorisationGroups.AsNoTracking()
      .Include(g => g.Items)
      .OrderBy(g => g.DisplayOrder)
      .ToListAsync(cancellationToken);
    var targetGroups = await target.FipsCategorisationGroups
      .Include(g => g.Items)
      .ToListAsync(cancellationToken);
    var targetGroupByName = targetGroups.ToDictionary(g => g.Name.Trim(), StringComparer.OrdinalIgnoreCase);

    foreach (var srcGroup in sourceGroups)
    {
      if (!targetGroupByName.TryGetValue(srcGroup.Name.Trim(), out var targetGroup))
      {
        if (dryRun) continue;
        targetGroup = new FipsCategorisationGroup
        {
          Name = srcGroup.Name,
          Description = srcGroup.Description,
          DisplayOrder = srcGroup.DisplayOrder,
          Active = srcGroup.Active
        };
        target.FipsCategorisationGroups.Add(targetGroup);
        await target.SaveChangesAsync(cancellationToken);
        targetGroupByName[srcGroup.Name.Trim()] = targetGroup;
      }
      else if (!dryRun)
      {
        targetGroup.Description = srcGroup.Description;
        targetGroup.DisplayOrder = srcGroup.DisplayOrder;
        targetGroup.Active = srcGroup.Active;
      }

      var targetItemsByName = targetGroup.Items.ToDictionary(i => i.Name.Trim(), StringComparer.OrdinalIgnoreCase);
      foreach (var srcItem in srcGroup.Items.OrderBy(i => i.DisplayOrder))
      {
        if (targetItemsByName.TryGetValue(srcItem.Name.Trim(), out var existingItem))
        {
          itemMap[srcItem.Id] = existingItem.Id;
          if (dryRun) continue;
          existingItem.Description = srcItem.Description;
          existingItem.DisplayOrder = srcItem.DisplayOrder;
          existingItem.Active = srcItem.Active;
        }
        else if (!dryRun)
        {
          var item = new FipsCategorisationItem
          {
            FipsCategorisationGroupId = targetGroup.Id,
            Name = srcItem.Name,
            Description = srcItem.Description,
            DisplayOrder = srcItem.DisplayOrder,
            Active = srcItem.Active
          };
          target.FipsCategorisationItems.Add(item);
          await target.SaveChangesAsync(cancellationToken);
          itemMap[srcItem.Id] = item.Id;
        }
      }
    }

    if (!dryRun)
      await target.SaveChangesAsync(cancellationToken);

    return itemMap;
  }

  private static void ApplyProductJunctions(
    CMDBProduct target,
    CMDBProduct source,
    Dictionary<int, int> channelMap,
    Dictionary<int, int> typeMap,
    Dictionary<int, int> userGroupMap,
    Dictionary<int, int> categorisationItemMap,
    Dictionary<int, int> contactRoleMap,
    List<string> errors)
  {
    foreach (var channelId in source.Channels.Select(x => x.FipsChannelId).Distinct())
    {
      if (!channelMap.TryGetValue(channelId, out var mapped))
        continue;
      target.Channels.Add(new CMDBProductChannel { CMDBProductId = target.Id, FipsChannelId = mapped });
    }

    foreach (var typeId in source.Types.Select(x => x.FipsTypeId).Distinct())
    {
      if (!typeMap.TryGetValue(typeId, out var mapped))
        continue;
      target.Types.Add(new CMDBProductType { CMDBProductId = target.Id, FipsTypeId = mapped });
    }

    foreach (var groupId in source.UserGroups.Select(x => x.FipsUserGroupId).Distinct())
    {
      if (!userGroupMap.TryGetValue(groupId, out var mapped))
        continue;
      target.UserGroups.Add(new CMDBProductUserGroup { CMDBProductId = target.Id, FipsUserGroupId = mapped });
    }

    foreach (var itemId in source.CategorisationItems.Select(x => x.FipsCategorisationItemId).Distinct())
    {
      if (!categorisationItemMap.TryGetValue(itemId, out var mapped))
        continue;
      target.CategorisationItems.Add(new CMDBProductFipsCategorisationItem
      {
        CMDBProductId = target.Id,
        FipsCategorisationItemId = mapped
      });
    }

    foreach (var contact in source.Contacts)
    {
      if (!contactRoleMap.TryGetValue(contact.FipsContactRoleId, out var roleId))
        continue;
      target.Contacts.Add(new CMDBProductContact
      {
        CMDBProductId = target.Id,
        FipsContactRoleId = roleId,
        UserEmail = contact.UserEmail,
        UserName = contact.UserName,
        CanManage = contact.CanManage
      });
    }
  }

  private static bool ContactsEqual(
    ICollection<CMDBProductContact> left,
    ICollection<CMDBProductContact> right,
    Dictionary<int, int> contactRoleMap)
  {
    var normalize = left
      .Where(c => contactRoleMap.ContainsKey(c.FipsContactRoleId))
      .Select(c => $"{contactRoleMap[c.FipsContactRoleId]}|{c.UserEmail?.Trim().ToLowerInvariant()}|{c.UserName}|{c.CanManage}")
      .OrderBy(x => x)
      .ToList();
    var normalizeRight = right
      .Select(c => $"{c.FipsContactRoleId}|{c.UserEmail?.Trim().ToLowerInvariant()}|{c.UserName}|{c.CanManage}")
      .OrderBy(x => x)
      .ToList();
    return normalize.SequenceEqual(normalizeRight);
  }

  private static bool JunctionSetEquals<TLeft, TRight>(
    ICollection<TLeft> left,
    ICollection<TRight> right,
    Dictionary<int, int> map,
    Func<TLeft, int> leftId,
    Func<TRight, int> rightId)
  {
    var mappedLeft = left.Select(x => map.GetValueOrDefault(leftId(x), -1)).Where(x => x >= 0).OrderBy(x => x).ToList();
    var rightIds = right.Select(rightId).OrderBy(x => x).ToList();
    return mappedLeft.SequenceEqual(rightIds);
  }

  private static int? RemapNullable(int? sourceId, Dictionary<int, int> map) =>
    sourceId.HasValue && map.TryGetValue(sourceId.Value, out var mapped) ? mapped : sourceId;

  private static EnvironmentSyncCountLine Line(
    string label,
    int sourceCount,
    int targetCount,
    int wouldCreate = 0,
    int wouldUpdate = 0) =>
    new()
    {
      Label = label,
      SourceCount = sourceCount,
      TargetCount = targetCount,
      WouldCreate = wouldCreate,
      WouldUpdate = wouldUpdate
    };
}
