using Compass.ViewModels.Modern;

namespace Compass.Services.Census;

/// <summary>
/// Builds analytical views of submitted census answers for Admin / Central Operations.
/// </summary>
public static class ServiceCensusAnalysisBuilder
{
    private static readonly string[] OverviewThemeKeys =
    [
        "phase",
        "group",
        "type",
        "channel",
        "user-group",
        "pattern",
        "ai",
        "stack",
        "source-code",
        "technology",
        "integration",
        "risk-appetite",
        "risk",
        "incidents",
        "accessibility"
    ];

    private static readonly string[] ThemeBoardKeys =
    [
        "group",
        "strategic-objective",
        "user-group",
        "pattern",
        "type",
        "channel",
        "phase",
        "ai",
        "stack",
        "technology",
        "integration",
        "source-code",
        "risk",
        "incidents",
        "accessibility"
    ];

    public static ServiceCensusAnalysisViewModel Build(
        ServiceCensusReportViewModel report,
        string? view,
        string? questionKey,
        Guid? productId,
        bool submittedOnly,
        bool? groupByAnswer = null)
    {
        var dimension = NormaliseView(view);
        var products = ScopeProducts(report.Products, submittedOnly);
        var productIds = products.Select(p => p.Id).ToHashSet();
        var answers = ScopeAnswers(report.SubmissionAnswers, productIds, submittedOnly);
        var contacts = ScopeContacts(report.SubmissionContacts, productIds, submittedOnly);

        var vm = new ServiceCensusAnalysisViewModel
        {
            View = dimension,
            BusinessAreaFilter = report.BusinessAreaFilter,
            BusinessAreaOptions = report.BusinessAreaOptions,
            QuestionKey = questionKey,
            ProductId = productId,
            SubmittedOnly = submittedOnly,
            GroupByAnswer = groupByAnswer == true,
            Questions = report.Questions,
            Products = products.OrderBy(p => p.Title, StringComparer.OrdinalIgnoreCase).ToList(),
            ProductCount = products.Count,
            SubmittedCount = products.Count(p => p.Submitted),
            AnswerRowCount = answers.Count(a =>
                a.CannotAnswer
                || !string.IsNullOrWhiteSpace(a.Item)
                || !string.IsNullOrWhiteSpace(a.Details))
        };

        switch (dimension)
        {
            case "area":
                BuildAreaView(vm, products, answers);
                break;
            case "question":
                BuildQuestionView(vm, products, answers, questionKey, groupByAnswer);
                break;
            case "product":
                BuildProductView(vm, products, answers, productId);
                break;
            case "themes":
                BuildThemeBoard(vm, products, answers);
                break;
            case "hierarchy":
                BuildServiceHierarchy(vm, products, answers);
                break;
            case "contacts":
                BuildContactsView(vm, contacts);
                break;
            default:
                BuildOverview(vm, products, answers);
                break;
        }

        return vm;
    }

    private static void BuildOverview(
        ServiceCensusAnalysisViewModel vm,
        List<ServiceCensusReportProductRow> products,
        List<ServiceCensusReportAnswerRow> answers)
    {
        var denominator = Math.Max(products.Count, 1);
        var cannotAnswerProducts = answers
            .Where(a => a.CannotAnswer)
            .Select(a => a.ProductId)
            .Distinct()
            .Count();

        vm.Kpis =
        [
            new ServiceCensusAnalysisKpi
            {
                Label = "Products in view",
                Value = products.Count.ToString("N0"),
                Hint = vm.SubmittedOnly ? "Submitted products only" : "All products in scope"
            },
            new ServiceCensusAnalysisKpi
            {
                Label = "Answer rows",
                Value = vm.AnswerRowCount.ToString("N0"),
                Hint = "Completed or cannot-answer responses"
            },
            new ServiceCensusAnalysisKpi
            {
                Label = "Cannot answer",
                Value = Pct(cannotAnswerProducts, denominator) + "%",
                Hint = $"{cannotAnswerProducts:N0} products marked at least one question"
            },
            KpiForMatch(products, answers, "ai", "AI functionality",
                item => ContainsLabel(item, "yes")),
            KpiForMatch(products, answers, "stack", "Hosted on Azure",
                item => ContainsLabel(item, "azure")),
            KpiForMatch(products, answers, "incidents", "Incident process",
                item => item.Contains("incident management process", StringComparison.OrdinalIgnoreCase)
                    && ContainsLabel(item, "yes"))
        ];

        vm.Themes = OverviewThemeKeys
            .Select(key => ThemeFor(key, products, answers))
            .Where(t => t != null)
            .Cast<ServiceCensusAnalysisTheme>()
            .ToList();
    }

    private static void BuildAreaView(
        ServiceCensusAnalysisViewModel vm,
        List<ServiceCensusReportProductRow> products,
        List<ServiceCensusReportAnswerRow> answers)
    {
        var areaNames = products
            .SelectMany(p => SplitAreas(p.Areas))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(a => a, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (areaNames.Count == 0 && !string.IsNullOrWhiteSpace(vm.BusinessAreaFilter))
            areaNames = [vm.BusinessAreaFilter];

        foreach (var name in areaNames)
        {
            var areaProducts = products
                .Where(p => SplitAreas(p.Areas).Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            var areaIds = areaProducts.Select(p => p.Id).ToHashSet();
            var areaAnswers = answers.Where(a => areaIds.Contains(a.ProductId)).ToList();
            var submitted = areaProducts.Count(p => p.Submitted);

            vm.Areas.Add(new ServiceCensusAnalysisAreaRow
            {
                Name = name,
                ProductCount = areaProducts.Count,
                SubmittedCount = submitted,
                SubmittedPercent = Pct(submitted, Math.Max(areaProducts.Count, 1)),
                TopPhase = TopValue(areaAnswers, "phase") ?? "—",
                AiYesPercent = MatchPercentLabel(areaProducts, areaAnswers, "ai",
                    item => ContainsLabel(item, "yes")),
                AzurePercent = MatchPercentLabel(areaProducts, areaAnswers, "stack",
                    item => ContainsLabel(item, "azure")),
                IncidentProcessYesPercent = MatchPercentLabel(areaProducts, areaAnswers, "incidents",
                    item => item.Contains("incident management process", StringComparison.OrdinalIgnoreCase)
                        && ContainsLabel(item, "yes")),
                TopTechnology = TopValue(areaAnswers, "technology") ?? "—"
            });
        }

        // Keep BusinessAreaFilter as the report filter only — do not overwrite it with
        // the drill-down area (that broke "All business areas" and Clear filter).
        var selectedName = vm.BusinessAreaFilter;
        if (string.IsNullOrWhiteSpace(selectedName))
            selectedName = vm.Areas.FirstOrDefault()?.Name;
        if (string.IsNullOrWhiteSpace(selectedName))
            return;

        var selectedProducts = products
            .Where(p => SplitAreas(p.Areas).Any(a => string.Equals(a, selectedName, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(p => p.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var selectedIds = selectedProducts.Select(p => p.Id).ToHashSet();
        var selectedAnswers = answers.Where(a => selectedIds.Contains(a.ProductId)).ToList();

        vm.SelectedArea = new ServiceCensusAnalysisAreaDetail
        {
            Name = selectedName,
            ProductCount = selectedProducts.Count,
            SubmittedCount = selectedProducts.Count(p => p.Submitted),
            Products = selectedProducts,
            Themes = OverviewThemeKeys
                .Select(key => ThemeFor(key, selectedProducts, selectedAnswers))
                .Where(t => t != null)
                .Cast<ServiceCensusAnalysisTheme>()
                .ToList()
        };
    }

    private static void BuildThemeBoard(
        ServiceCensusAnalysisViewModel vm,
        List<ServiceCensusReportProductRow> products,
        List<ServiceCensusReportAnswerRow> answers)
    {
        vm.ThemeBoard = ThemeBoardKeys
            .Select(key => ThemeFor(key, products, answers))
            .Where(t => t != null)
            .Cast<ServiceCensusAnalysisTheme>()
            .OrderByDescending(t => t.ProductsAnswered)
            .ThenBy(t => t.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        vm.Kpis =
        [
            new ServiceCensusAnalysisKpi
            {
                Label = "Theme questions",
                Value = vm.ThemeBoard.Count.ToString("N0"),
                Hint = "Categorical questions with answers in view"
            },
            new ServiceCensusAnalysisKpi
            {
                Label = "Products in view",
                Value = products.Count.ToString("N0"),
                Hint = vm.SubmittedOnly ? "Submitted products only" : "All products in scope"
            },
            new ServiceCensusAnalysisKpi
            {
                Label = "Distinct answers",
                Value = vm.ThemeBoard.Sum(t => t.Values.Count).ToString("N0"),
                Hint = "Across theme distributions"
            }
        ];
    }

    private static void BuildServiceHierarchy(
        ServiceCensusAnalysisViewModel vm,
        List<ServiceCensusReportProductRow> products,
        List<ServiceCensusReportAnswerRow> answers)
    {
        static string? BestValue(IEnumerable<ServiceCensusReportAnswerRow> rows) =>
            rows.Select(r => !string.IsNullOrWhiteSpace(r.Item) ? r.Item!.Trim() : r.Details?.Trim())
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        var serviceByProduct = answers
            .Where(a => string.Equals(a.QuestionKey, "service", StringComparison.OrdinalIgnoreCase) && !a.CannotAnswer)
            .GroupBy(a => a.ProductId)
            .ToDictionary(g => g.Key, g => BestValue(g) ?? "");

        var serviceLineByProduct = answers
            .Where(a => string.Equals(a.QuestionKey, "service-line", StringComparison.OrdinalIgnoreCase) && !a.CannotAnswer)
            .GroupBy(a => a.ProductId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var row = g.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.Item) || !string.IsNullOrWhiteSpace(r.Details));
                    return (
                        Name: row?.Item?.Trim() ?? "",
                        Description: row?.Details?.Trim()
                    );
                });

        // Hierarchy is Service line → Service → Products.
        var lines = new Dictionary<string, ServiceCensusAnalysisServiceLineNode>(StringComparer.OrdinalIgnoreCase);
        var noServiceLine = new Dictionary<string, ServiceCensusAnalysisServiceNode>(StringComparer.OrdinalIgnoreCase);
        var unassigned = new List<ServiceCensusAnalysisHierarchyProduct>();

        foreach (var product in products.OrderBy(p => p.Title, StringComparer.OrdinalIgnoreCase))
        {
            serviceByProduct.TryGetValue(product.Id, out var serviceName);
            serviceLineByProduct.TryGetValue(product.Id, out var line);
            var hierarchyProduct = new ServiceCensusAnalysisHierarchyProduct
            {
                ProductId = product.Id,
                Product = product.Title,
                Areas = product.Areas,
                CensusStatus = product.CensusStatus
            };

            if (string.IsNullOrWhiteSpace(line.Name) && string.IsNullOrWhiteSpace(serviceName))
            {
                unassigned.Add(hierarchyProduct);
                continue;
            }

            if (string.IsNullOrWhiteSpace(line.Name))
            {
                if (string.IsNullOrWhiteSpace(serviceName))
                {
                    unassigned.Add(hierarchyProduct);
                    continue;
                }

                if (!noServiceLine.TryGetValue(serviceName, out var orphanService))
                {
                    orphanService = new ServiceCensusAnalysisServiceNode { ServiceName = serviceName };
                    noServiceLine[serviceName] = orphanService;
                }

                orphanService.Products.Add(hierarchyProduct);
                continue;
            }

            if (!lines.TryGetValue(line.Name, out var lineNode))
            {
                lineNode = new ServiceCensusAnalysisServiceLineNode
                {
                    ServiceLineName = line.Name,
                    Description = line.Description
                };
                lines[line.Name] = lineNode;
            }
            else if (string.IsNullOrWhiteSpace(lineNode.Description) && !string.IsNullOrWhiteSpace(line.Description))
            {
                lineNode.Description = line.Description;
            }

            if (string.IsNullOrWhiteSpace(serviceName))
            {
                lineNode.UnassignedProducts.Add(hierarchyProduct);
                continue;
            }

            var serviceNode = lineNode.Services.FirstOrDefault(s =>
                string.Equals(s.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase));
            if (serviceNode == null)
            {
                serviceNode = new ServiceCensusAnalysisServiceNode { ServiceName = serviceName };
                lineNode.Services.Add(serviceNode);
            }

            serviceNode.Products.Add(hierarchyProduct);
        }

        static void FinaliseService(ServiceCensusAnalysisServiceNode service)
        {
            service.Products = service.Products
                .OrderBy(p => p.Product, StringComparer.OrdinalIgnoreCase)
                .ToList();
            service.ProductCount = service.Products.Count;
        }

        foreach (var lineNode in lines.Values)
        {
            foreach (var service in lineNode.Services)
                FinaliseService(service);

            lineNode.Services = lineNode.Services
                .OrderByDescending(s => s.ProductCount)
                .ThenBy(s => s.ServiceName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            lineNode.UnassignedProducts = lineNode.UnassignedProducts
                .OrderBy(p => p.Product, StringComparer.OrdinalIgnoreCase)
                .ToList();
            lineNode.ProductCount = lineNode.Services.Sum(s => s.ProductCount) + lineNode.UnassignedProducts.Count;
        }

        vm.ServiceHierarchy = lines.Values
            .OrderByDescending(n => n.ProductCount)
            .ThenBy(n => n.ServiceLineName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (noServiceLine.Count > 0)
        {
            var orphanLine = new ServiceCensusAnalysisServiceLineNode
            {
                ServiceLineName = "No service line entered",
                Services = noServiceLine.Values
                    .Select(s =>
                    {
                        FinaliseService(s);
                        return s;
                    })
                    .OrderByDescending(s => s.ProductCount)
                    .ThenBy(s => s.ServiceName, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };
            orphanLine.ProductCount = orphanLine.Services.Sum(s => s.ProductCount);
            vm.ServiceHierarchy.Add(orphanLine);
        }

        if (unassigned.Count > 0)
        {
            vm.ServiceHierarchy.Add(new ServiceCensusAnalysisServiceLineNode
            {
                ServiceLineName = "No service line or service entered",
                ProductCount = unassigned.Count,
                UnassignedProducts = unassigned
            });
        }

        vm.Kpis =
        [
            new ServiceCensusAnalysisKpi
            {
                Label = "Service lines",
                Value = lines.Count.ToString("N0"),
                Hint = "Distinct service lines entered"
            },
            new ServiceCensusAnalysisKpi
            {
                Label = "Services",
                Value = (lines.Values.Sum(l => l.Services.Count) + noServiceLine.Count).ToString("N0"),
                Hint = "Distinct services under service lines"
            },
            new ServiceCensusAnalysisKpi
            {
                Label = "Unassigned",
                Value = unassigned.Count.ToString("N0"),
                Hint = "Products with neither service line nor service"
            }
        ];
    }

    private static void BuildContactsView(
        ServiceCensusAnalysisViewModel vm,
        List<ServiceCensusReportContactRow> contacts)
    {
        static ServiceCensusAnalysisContactChange Map(ServiceCensusReportContactRow c) => new()
        {
            ProductId = c.ProductId,
            Product = c.Product,
            Areas = c.Areas,
            Role = c.Role,
            Change = c.Change,
            OriginalName = c.OriginalName,
            OriginalEmail = c.OriginalEmail,
            NewName = c.Name,
            NewEmail = c.Email
        };

        var changed = contacts
            .Where(c => string.Equals(c.Change, "Changed", StringComparison.OrdinalIgnoreCase))
            .Select(Map)
            .OrderBy(c => c.Product, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Role, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var added = contacts
            .Where(c => c.IsAdded || string.Equals(c.Change, "Added", StringComparison.OrdinalIgnoreCase))
            .Select(Map)
            .OrderBy(c => c.Product, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Role, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var removed = contacts
            .Where(c => string.Equals(c.Change, "Removed", StringComparison.OrdinalIgnoreCase))
            .Select(Map)
            .OrderBy(c => c.Product, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Role, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var confirmed = contacts.Count(c =>
            string.Equals(c.Change, "Confirmed", StringComparison.OrdinalIgnoreCase));

        vm.Contacts = new ServiceCensusAnalysisContactsView
        {
            ChangedCount = changed.Count,
            AddedCount = added.Count,
            RemovedCount = removed.Count,
            ConfirmedCount = confirmed,
            Changes = changed,
            AddedRoles = added,
            RemovedRoles = removed
        };

        vm.Kpis =
        [
            new ServiceCensusAnalysisKpi
            {
                Label = "Changed",
                Value = changed.Count.ToString("N0"),
                Hint = "Different person named for an existing role"
            },
            new ServiceCensusAnalysisKpi
            {
                Label = "Added",
                Value = added.Count.ToString("N0"),
                Hint = "Extra roles created for product offerings"
            },
            new ServiceCensusAnalysisKpi
            {
                Label = "Removed",
                Value = removed.Count.ToString("N0"),
                Hint = "Existing contacts marked incorrect with no replacement"
            },
            new ServiceCensusAnalysisKpi
            {
                Label = "Confirmed",
                Value = confirmed.ToString("N0"),
                Hint = "Existing contacts left unchanged"
            }
        ];
    }

    private static void BuildQuestionView(
        ServiceCensusAnalysisViewModel vm,
        List<ServiceCensusReportProductRow> products,
        List<ServiceCensusReportAnswerRow> answers,
        string? questionKey,
        bool? groupByAnswer)
    {
        var key = questionKey;
        if (string.IsNullOrWhiteSpace(key) || CensusDefinition.Find(key) == null)
            key = CensusDefinition.Tasks.FirstOrDefault()?.Key;
        vm.QuestionKey = key;
        if (string.IsNullOrWhiteSpace(key))
            return;

        var task = CensusDefinition.Find(key)!;
        var canGroup = SupportsGroupByAnswer(task);
        var isMultiSelect = IsMultiSelect(task);
        var doGroup = canGroup && (groupByAnswer ?? true);
        vm.GroupByAnswer = doGroup;

        var questionAnswers = answers
            .Where(a => string.Equals(a.QuestionKey, key, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var byProduct = questionAnswers
            .GroupBy(a => a.ProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var productAnswers = new List<ServiceCensusAnalysisProductAnswer>();
        var answered = 0;
        var cannot = 0;
        var missing = 0;
        var itemTotals = 0;
        var itemProducts = 0;

        foreach (var product in products)
        {
            if (!byProduct.TryGetValue(product.Id, out var rows) || rows.Count == 0)
            {
                missing++;
                productAnswers.Add(new ServiceCensusAnalysisProductAnswer
                {
                    ProductId = product.Id,
                    Product = product.Title,
                    Areas = product.Areas,
                    Status = "Not started",
                    Summary = "—"
                });
                continue;
            }

            var status = rows[0].QuestionStatus;
            if (rows.Any(r => r.CannotAnswer))
            {
                cannot++;
                productAnswers.Add(new ServiceCensusAnalysisProductAnswer
                {
                    ProductId = product.Id,
                    Product = product.Title,
                    Areas = product.Areas,
                    Status = "Cannot answer",
                    Summary = "Cannot answer",
                    Details = rows.Select(r => r.CannotAnswerReason).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))
                });
                continue;
            }

            var meaningful = rows
                .Where(r => !string.IsNullOrWhiteSpace(r.Item) || !string.IsNullOrWhiteSpace(r.Details))
                .ToList();
            if (meaningful.Count == 0)
            {
                missing++;
                productAnswers.Add(new ServiceCensusAnalysisProductAnswer
                {
                    ProductId = product.Id,
                    Product = product.Title,
                    Areas = product.Areas,
                    Status = string.IsNullOrWhiteSpace(status) ? "Not started" : status,
                    Summary = "—"
                });
                continue;
            }

            answered++;
            itemTotals += meaningful.Count;
            itemProducts++;
            var answerValues = meaningful
                .Select(r => r.Item?.Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var summary = string.Join("; ", meaningful
                .Select(FormatAnswerBrief)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8));
            var details = string.Join(" · ", meaningful
                .Select(r => r.Details)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(4));

            productAnswers.Add(new ServiceCensusAnalysisProductAnswer
            {
                ProductId = product.Id,
                Product = product.Title,
                Areas = product.Areas,
                Status = string.IsNullOrWhiteSpace(status) ? "Complete" : status,
                Summary = string.IsNullOrWhiteSpace(summary) ? "—" : summary,
                Details = string.IsNullOrWhiteSpace(details) ? null : details,
                AnswerValues = answerValues
            });
        }

        var valueCounts = CountValues(
            questionAnswers.Where(a => !a.CannotAnswer),
            Math.Max(products.Count, 1));

        var answerGroups = new List<ServiceCensusAnalysisAnswerGroup>();
        if (doGroup && valueCounts.Count > 0)
        {
            var byId = productAnswers.ToDictionary(p => p.ProductId);
            foreach (var value in valueCounts)
            {
                var memberIds = questionAnswers
                    .Where(a => !a.CannotAnswer
                        && !string.IsNullOrWhiteSpace(a.Item)
                        && string.Equals(a.Item.Trim(), value.Value, StringComparison.OrdinalIgnoreCase))
                    .Select(a => a.ProductId)
                    .Distinct()
                    .ToList();
                var members = memberIds
                    .Where(id => byId.ContainsKey(id))
                    .Select(id =>
                    {
                        var source = byId[id];
                        var others = source.AnswerValues
                            .Where(v => !string.Equals(v, value.Value, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        return new ServiceCensusAnalysisProductAnswer
                        {
                            ProductId = source.ProductId,
                            Product = source.Product,
                            Areas = source.Areas,
                            Status = source.Status,
                            Summary = value.Value,
                            Details = others.Count == 0
                                ? source.Details
                                : "Also: " + string.Join("; ", others)
                                    + (string.IsNullOrWhiteSpace(source.Details) ? "" : " · " + source.Details),
                            AnswerValues = source.AnswerValues
                        };
                    })
                    .OrderBy(p => p.Product, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                answerGroups.Add(new ServiceCensusAnalysisAnswerGroup
                {
                    Answer = value.Value,
                    ProductCount = members.Count,
                    Percent = value.Percent,
                    Products = members
                });
            }
        }

        vm.SelectedQuestion = new ServiceCensusAnalysisQuestionDetail
        {
            Key = task.Key,
            Section = task.GroupName,
            Question = task.NavLabel,
            KindLabel = KindLabel(task),
            CanGroupByAnswer = canGroup,
            IsMultiSelect = isMultiSelect,
            ProductsInScope = products.Count,
            ProductsAnswered = answered,
            ProductsCannotAnswer = cannot,
            ProductsMissing = missing,
            AverageItemsPerProduct = itemProducts == 0 ? 0 : Math.Round((double)itemTotals / itemProducts, 1),
            ValueCounts = valueCounts,
            ProductAnswers = productAnswers
                .OrderBy(p => p.Product, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            AnswerGroups = answerGroups
        };
    }

    private static bool SupportsGroupByAnswer(CensusTask task) =>
        task.Kind is CensusTaskKind.Lookup or CensusTaskKind.YesChoice or CensusTaskKind.Accessibility
        || task.ChoiceOptions.Count > 0
        || (task.Kind == CensusTaskKind.Form
            && task.FormFields.Any(f =>
                f.Kind == CensusFormFieldKind.Radio
                || f.ConditionalChoices.Count > 0));

    private static bool IsMultiSelect(CensusTask task) =>
        task.Kind == CensusTaskKind.Lookup && task.LookupControl == CensusLookupControl.Checkbox
        || task.FormFields.Any(f => f.ConditionalChoices.Any(c => c.Minimum != 1 || c.Choices.Count > 2));

    private static void BuildProductView(
        ServiceCensusAnalysisViewModel vm,
        List<ServiceCensusReportProductRow> products,
        List<ServiceCensusReportAnswerRow> answers,
        Guid? productId)
    {
        var id = productId;
        if (id == null || products.All(p => p.Id != id))
            id = products.FirstOrDefault()?.Id;
        vm.ProductId = id;
        if (id == null)
            return;

        var product = products.First(p => p.Id == id);
        var productAnswers = answers
            .Where(a => a.ProductId == id)
            .ToList();

        var sections = new List<ServiceCensusAnalysisSectionAnswers>();
        foreach (var group in CensusDefinition.Groups())
        {
            var questions = new List<ServiceCensusAnalysisQuestionAnswers>();
            foreach (var task in group)
            {
                var rows = productAnswers
                    .Where(a => string.Equals(a.QuestionKey, task.Key, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (rows.Count == 0)
                {
                    questions.Add(new ServiceCensusAnalysisQuestionAnswers
                    {
                        Key = task.Key,
                        Question = task.NavLabel,
                        Status = "Not started"
                    });
                    continue;
                }

                var first = rows[0];
                if (first.CannotAnswer || rows.Any(r => r.CannotAnswer))
                {
                    questions.Add(new ServiceCensusAnalysisQuestionAnswers
                    {
                        Key = task.Key,
                        Question = task.NavLabel,
                        Status = "Cannot answer",
                        CannotAnswer = true,
                        CannotAnswerReason = rows
                            .Select(r => r.CannotAnswerReason)
                            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))
                    });
                    continue;
                }

                var items = rows
                    .Where(r => !string.IsNullOrWhiteSpace(r.Item)
                        || !string.IsNullOrWhiteSpace(r.Details)
                        || !string.IsNullOrWhiteSpace(r.Reference))
                    .Select(r =>
                    {
                        var item = r.Item?.Trim();
                        var details = r.Details?.Trim();
                        // Drop details that only repeat the selected option / value.
                        if (!string.IsNullOrWhiteSpace(item)
                            && !string.IsNullOrWhiteSpace(details)
                            && string.Equals(item, details, StringComparison.OrdinalIgnoreCase))
                            details = null;
                        return new ServiceCensusAnalysisAnswerItem
                        {
                            Item = item,
                            Details = details,
                            Reference = r.Reference
                        };
                    })
                    .ToList();

                questions.Add(new ServiceCensusAnalysisQuestionAnswers
                {
                    Key = task.Key,
                    Question = task.NavLabel,
                    Status = items.Count == 0
                        ? (string.IsNullOrWhiteSpace(first.QuestionStatus) ? "Not started" : first.QuestionStatus)
                        : (string.IsNullOrWhiteSpace(first.QuestionStatus) ? "Complete" : first.QuestionStatus),
                    Items = items
                });
            }

            sections.Add(new ServiceCensusAnalysisSectionAnswers
            {
                Name = group.First().GroupName,
                Questions = questions
            });
        }

        var submittedAt = productAnswers.Select(a => a.SubmittedAt).FirstOrDefault(d => d != null);
        var submittedBy = productAnswers.Select(a => a.SubmittedBy).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));

        vm.SelectedProduct = new ServiceCensusAnalysisProductDetail
        {
            Id = product.Id,
            Title = product.Title,
            Areas = product.Areas,
            CensusStatus = product.CensusStatus,
            SubmittedAt = submittedAt,
            SubmittedBy = submittedBy,
            Sections = sections
        };
    }

    private static ServiceCensusAnalysisTheme? ThemeFor(
        string questionKey,
        List<ServiceCensusReportProductRow> products,
        List<ServiceCensusReportAnswerRow> answers)
    {
        var task = CensusDefinition.Find(questionKey);
        if (task == null)
            return null;

        var rows = answers
            .Where(a => string.Equals(a.QuestionKey, questionKey, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (rows.Count == 0)
            return null;

        var answeredProducts = rows
            .Where(a => !a.CannotAnswer
                && (!string.IsNullOrWhiteSpace(a.Item) || !string.IsNullOrWhiteSpace(a.Details)))
            .Select(a => a.ProductId)
            .Distinct()
            .Count();
        var cannot = rows.Where(a => a.CannotAnswer).Select(a => a.ProductId).Distinct().Count();
        var values = CountValues(rows.Where(a => !a.CannotAnswer), Math.Max(products.Count, 1));
        if (values.Count == 0 && answeredProducts == 0 && cannot == 0)
            return null;

        return new ServiceCensusAnalysisTheme
        {
            Title = task.NavLabel,
            Section = task.GroupName,
            QuestionKey = task.Key,
            Description = answeredProducts == 0 && cannot > 0
                ? $"{cannot:N0} products cannot answer"
                : $"{answeredProducts:N0} of {products.Count:N0} products answered",
            ProductsAnswered = answeredProducts,
            ProductsCannotAnswer = cannot,
            Values = values.Take(8).ToList()
        };
    }

    private static List<ServiceCensusAnalysisValueCount> CountValues(
        IEnumerable<ServiceCensusReportAnswerRow> answers,
        int denominator)
    {
        return answers
            .Where(a => !string.IsNullOrWhiteSpace(a.Item))
            .GroupBy(a => a.Item!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var count = g.Select(x => x.ProductId).Distinct().Count();
                return new ServiceCensusAnalysisValueCount
                {
                    Value = g.First().Item!.Trim(),
                    ProductCount = count,
                    Percent = Pct(count, denominator)
                };
            })
            .OrderByDescending(v => v.ProductCount)
            .ThenBy(v => v.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ServiceCensusAnalysisKpi KpiForMatch(
        List<ServiceCensusReportProductRow> products,
        List<ServiceCensusReportAnswerRow> answers,
        string questionKey,
        string label,
        Func<string, bool> match)
    {
        var matched = products.Count(p =>
            answers.Any(a =>
                a.ProductId == p.Id
                && string.Equals(a.QuestionKey, questionKey, StringComparison.OrdinalIgnoreCase)
                && !a.CannotAnswer
                && !string.IsNullOrWhiteSpace(a.Item)
                && match(a.Item!)));
        return new ServiceCensusAnalysisKpi
        {
            Label = label,
            Value = Pct(matched, Math.Max(products.Count, 1)) + "%",
            Hint = $"{matched:N0} of {products.Count:N0} products",
            QuestionKey = questionKey
        };
    }

    private static string MatchPercentLabel(
        List<ServiceCensusReportProductRow> products,
        List<ServiceCensusReportAnswerRow> answers,
        string questionKey,
        Func<string, bool> match)
    {
        if (products.Count == 0)
            return "—";
        var matched = products.Count(p =>
            answers.Any(a =>
                a.ProductId == p.Id
                && string.Equals(a.QuestionKey, questionKey, StringComparison.OrdinalIgnoreCase)
                && !a.CannotAnswer
                && !string.IsNullOrWhiteSpace(a.Item)
                && match(a.Item!)));
        return Pct(matched, products.Count) + "%";
    }

    private static string? TopValue(List<ServiceCensusReportAnswerRow> answers, string questionKey)
    {
        return CountValues(
                answers.Where(a =>
                    string.Equals(a.QuestionKey, questionKey, StringComparison.OrdinalIgnoreCase)
                    && !a.CannotAnswer),
                1)
            .FirstOrDefault()?.Value;
    }

    private static string FormatAnswerBrief(ServiceCensusReportAnswerRow row)
    {
        if (!string.IsNullOrWhiteSpace(row.Item) && !string.IsNullOrWhiteSpace(row.Details)
            && !string.Equals(row.Item, row.Details, StringComparison.OrdinalIgnoreCase))
            return row.Item + " — " + Truncate(row.Details, 80);
        if (!string.IsNullOrWhiteSpace(row.Item))
            return row.Item!;
        return Truncate(row.Details, 100) ?? "";
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..(max - 1)].TrimEnd() + "…";
    }

    private static string KindLabel(CensusTask task) => task.Kind switch
    {
        CensusTaskKind.LongText => "Free text",
        CensusTaskKind.ItemList => "List of items",
        CensusTaskKind.YesChoice => "Yes / no",
        CensusTaskKind.Contacts => "Contacts",
        CensusTaskKind.Accessibility => "Accessibility",
        CensusTaskKind.Lookup => task.LookupControl == CensusLookupControl.Checkbox ? "Multi-select" : "Single select",
        CensusTaskKind.Form => "Structured form",
        _ => task.Kind.ToString()
    };

    private static bool ContainsLabel(string item, string needle)
    {
        var colon = item.LastIndexOf(':');
        var focus = colon >= 0 ? item[(colon + 1)..] : item;
        return focus.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> SplitAreas(string? areas)
    {
        if (string.IsNullOrWhiteSpace(areas))
            yield break;
        foreach (var part in areas.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrWhiteSpace(part))
                yield return part;
        }
    }

    private static List<ServiceCensusReportProductRow> ScopeProducts(
        List<ServiceCensusReportProductRow> products, bool submittedOnly) =>
        submittedOnly ? products.Where(p => p.Submitted).ToList() : products.ToList();

    private static List<ServiceCensusReportAnswerRow> ScopeAnswers(
        List<ServiceCensusReportAnswerRow> answers,
        HashSet<Guid> productIds,
        bool submittedOnly)
    {
        var scoped = answers.Where(a => productIds.Contains(a.ProductId));
        if (submittedOnly)
            scoped = scoped.Where(a =>
                string.Equals(a.CensusStatus, "Submitted", StringComparison.OrdinalIgnoreCase));
        return scoped.ToList();
    }

    private static List<ServiceCensusReportContactRow> ScopeContacts(
        List<ServiceCensusReportContactRow> contacts,
        HashSet<Guid> productIds,
        bool submittedOnly)
    {
        var scoped = contacts.Where(c => productIds.Contains(c.ProductId));
        if (submittedOnly)
            scoped = scoped.Where(c =>
                string.Equals(c.CensusStatus, "Submitted", StringComparison.OrdinalIgnoreCase));
        return scoped.ToList();
    }

    private static string NormaliseView(string? view) =>
        view?.Trim().ToLowerInvariant() switch
        {
            "area" or "areas" or "business-area" or "business-areas" => "area",
            "question" or "questions" => "question",
            "product" or "products" => "product",
            "themes" or "theme" or "theming" => "themes",
            "hierarchy" or "service" or "services" or "service-line" or "service-lines" => "hierarchy",
            "contacts" or "contact" or "contact-changes" => "contacts",
            _ => "overview"
        };

    private static int Pct(int count, int denominator) =>
        denominator <= 0 ? 0 : (int)Math.Round(100.0 * count / denominator);
}
