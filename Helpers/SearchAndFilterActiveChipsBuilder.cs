using System.Linq;
using Compass.Models;
using Compass.Models.Modern.Work;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;

namespace Compass.Helpers;

public static class SearchAndFilterActiveChipsBuilder
{
    /// <summary>Builds removable filter chips for <see cref="SearchAndFilterViewModel"/> (FIPS list, demand, etc.).</summary>
    public static IReadOnlyList<SearchAndFilterActiveChip> FromViewModel(
        SearchAndFilterViewModel vm,
        IUrlHelper url,
        string action,
        string controller,
        object? additionalRoute = null)
    {
        var list = new List<SearchAndFilterActiveChip>();
        var full = new RouteValueDictionary(additionalRoute);
        foreach (var h in vm.HiddenFields)
            full[h.Key] = h.Value;
        if (!string.IsNullOrWhiteSpace(vm.SearchValue))
            full["search"] = vm.SearchValue.Trim();
        foreach (var f in vm.Fields)
        {
            if (string.IsNullOrEmpty(f.SelectedValue) || IsNoOpFilterValue(f.SelectedValue))
                continue;
            full[f.Name] = f.SelectedValue;
        }

        if (!string.IsNullOrWhiteSpace(vm.SearchValue))
        {
            var d = new RouteValueDictionary(full);
            d.Remove("search");
            var u = url.Action(action, controller, d) ?? "#";
            list.Add(new SearchAndFilterActiveChip("Search", vm.SearchValue.Trim(), u));
        }

        foreach (var f in vm.Fields)
        {
            if (string.IsNullOrEmpty(f.SelectedValue) || IsNoOpFilterValue(f.SelectedValue))
                continue;
            var text = f.Options.FirstOrDefault(o => o.Value == f.SelectedValue)?.Text ?? f.SelectedValue;
            var d = new RouteValueDictionary(full);
            d.Remove(f.Name);
            var u = url.Action(action, controller, d) ?? "#";
            list.Add(new SearchAndFilterActiveChip(f.Label, text, u));
        }

        return list;
    }

    static bool IsNoOpFilterValue(string v) => v is "" or "all";

    /// <summary>Work register (all work / manage work).</summary>
    public static IReadOnlyList<SearchAndFilterActiveChip> ForWorkRegister(
        WorkRegisterViewModel m,
        IUrlHelper url,
        string listAction,
        string listController,
        string activeTab)
    {
        var list = new List<SearchAndFilterActiveChip>();
        var full = WorkRegisterFullRoute(m, activeTab);

        void AddChip(string label, string value, Action<RouteValueDictionary> clear)
        {
            var d = new RouteValueDictionary(full);
            clear(d);
            var u = url.Action(listAction, listController, d) ?? "#";
            list.Add(new SearchAndFilterActiveChip(label, value, u));
        }

        if (!string.IsNullOrWhiteSpace(m.FilterSearch))
            AddChip("Search", m.FilterSearch.Trim(), x => x.Remove("search"));

        if (m.FilterRagId is { } rId)
        {
            var name = m.RagOptions.FirstOrDefault(x => x.Id == rId)?.Name ?? rId.ToString();
            AddChip("RAG", name, x => x.Remove("ragId"));
        }

        if (m.FilterPhaseId is { } phId)
        {
            var o = m.DeliveryPhaseOptions.FirstOrDefault(x => x.Id == phId);
            var name = o?.Name ?? o?.Value ?? phId.ToString();
            AddChip("Phase", name, x => x.Remove("phaseId"));
        }

        if (m.FilterBusinessAreaId is { } baId)
        {
            var name = m.BusinessAreas.FirstOrDefault(x => x.Id == baId)?.Name ?? baId.ToString();
            AddChip("Business area", name, x => x.Remove("businessAreaId"));
        }

        if (m.FilterDirectorateId is { } dId)
        {
            var name = m.Directorates.FirstOrDefault(x => x.Id == dId)?.Name ?? dId.ToString();
            AddChip("Directorate", name, x => x.Remove("directorateId"));
        }

        if (m.FilterPriorityId is { } prId)
        {
            var o = m.PriorityOptions.FirstOrDefault(x => x.Id == prId);
            var name = o?.Name ?? o?.Value ?? prId.ToString();
            AddChip("Priority", name, x => x.Remove("priorityId"));
        }

        if (!string.IsNullOrWhiteSpace(m.FilterMonthlyUpdate))
        {
            var v = m.FilterMonthlyUpdate!;
            var display = v switch
            {
                "overdue" => "Overdue",
                "due-today" => "Due today",
                "submitted" => "Submitted",
                "not-required" => "Not required",
                _ => v
            };
            AddChip("Monthly update", display, x => x.Remove("monthlyUpdate"));
        }

        if (m.FilterPrimaryContactUserId is { } uId)
        {
            var name = m.PrimaryContactFilterOptions.FirstOrDefault(x => x.UserId == uId)?.DisplayName ?? uId.ToString();
            AddChip("Primary contact", name, x => x.Remove("primaryContactUserId"));
        }

        if (m.FilterTagIds is { Count: > 0 } tagIds)
        {
            foreach (var tid in tagIds)
            {
                var tName = m.TagFilterOptions.FirstOrDefault(x => x.Id == tid)?.Name
                            ?? m.TagFilterOptions.FirstOrDefault(x => x.Id == tid)?.Value
                            ?? tid.ToString();
                var remaining = tagIds.Where(id => id != tid).ToArray();
                AddChip("Tag", tName, d =>
                {
                    d.Remove("tagId");
                    d.Remove("tagIds");
                    if (remaining.Length > 0)
                        d["tagIds"] = remaining;
                });
            }
        }

        return list;
    }

    static RouteValueDictionary WorkRegisterFullRoute(WorkRegisterViewModel m, string activeTab)
    {
        var d = new RouteValueDictionary
        {
            ["tab"] = activeTab,
            ["page"] = 1,
        };
        if (m.IsMyWork)
            d["mine"] = true;
        if (!string.IsNullOrWhiteSpace(m.FilterSearch))
            d["search"] = m.FilterSearch;
        if (m.FilterBusinessAreaId is { } ba)
            d["businessAreaId"] = ba;
        if (m.FilterDirectorateId is { } dir)
            d["directorateId"] = dir;
        if (m.FilterPhaseId is { } ph)
            d["phaseId"] = ph;
        if (m.FilterRagId is { } rag)
            d["ragId"] = rag;
        if (m.FilterPriorityId is { } pr)
            d["priorityId"] = pr;
        if (!string.IsNullOrWhiteSpace(m.FilterMonthlyUpdate))
            d["monthlyUpdate"] = m.FilterMonthlyUpdate;
        if (m.FilterPrimaryContactUserId is { } pc)
            d["primaryContactUserId"] = pc;
        if (m.FilterTagIds is { Count: > 0 } tags)
            d["tagIds"] = tags.ToArray();
        d["sort"] = m.RegisterSortField ?? "title";
        d["sd"] = m.RegisterSortDescending;
        return d;
    }
}
