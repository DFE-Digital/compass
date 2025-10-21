# Reports feature

## Overview

The Reports section provides comprehensive analytical views of RAID (Risks, Actions, Issues, Decisions) data across all products. This enables leadership to identify problematic products, track portfolio health, and make data-driven decisions.

## Reports available

### 1. Risks and issues by product

**Route:** `/Reports/RisksAndIssues`

A league table ranking all products by health score, showing comprehensive RAID statistics for each product.

#### Features

**Summary cards:**
- Total risks across portfolio
- High risks (score 15+)
- Total issues
- Critical issues

**Product league table:**
Products ranked by health score (worst first) showing:
- **Rank** - Position in league table (top 3 highlighted)
- **Product** - Title and FIPS ID
- **Health score** - Visual progress bar (0-100, higher = better)
- **Risks** - Total count, open count, high risk count
- **Issues** - Total count, open count, critical count, blocked count
- **Actions** - Total count, overdue count
- **Milestones** - Total count, overdue count

**Visual indicators:**
- Red row highlighting for products with health score < 30 (critical)
- Amber row highlighting for products with health score < 60 (warning)
- Colour-coded badges for different metrics

#### Health score calculation

Starting from 100 (perfect health), points are deducted for:
- Open risks: -2 points each
- High risks (15+): -5 points each
- Open issues: -3 points each
- Critical issues: -10 points each
- Blocked issues: -8 points each
- Overdue actions: -2 points each
- Overdue milestones: -5 points each

**Interpretation:**
- 80-100: Good health
- 60-79: Fair health
- 30-59: Needs monitoring
- 0-29: Urgent attention required

### 2. Product analysis

**Route:** `/Reports/Analysis`

Advanced analytics dashboard identifying products that need attention based on multiple data sources.

#### Features

**Executive summary cards:**
- Total products in portfolio
- Products needing attention (problem score > 50)
- Total high risks across portfolio
- Total critical issues
- Total blocked issues

**Products requiring urgent attention:**
Up to 6 products with highest problem scores, showing:
- Problem score (0-100, higher = worse)
- User satisfaction percentage
- High risks count
- Critical issues count
- Blocked issues count
- Overdue actions count
- Delayed milestones count
- Age of oldest open issue
- Quick links to view risks/issues/actions

**Product analysis overview table:**
All products sorted by problem score showing:
- Product name and FIPS ID
- Problem score with visual indicator
- User satisfaction (from latest performance metrics)
- Risk indicators (total, high count, average score)
- Issue indicators (total, critical, blocked, oldest issue age)
- Delivery indicators (overdue actions, delayed milestones)
- Last report submission date
- Status badge (Needs attention / Monitor / Healthy)

**Key insights section:**
Automated analysis highlighting:
- Percentage of portfolio needing attention
- Count of high-severity risks
- Count of critical issues
- Count of blocked issues
- Products with low user satisfaction (<70%)
- Products with long-running issues (>90 days)

**Recommended actions section:**
Prioritised action recommendations:
- Priority review for most problematic products
- Critical issue escalation
- Blocked item review
- Risk mitigation strategies
- Data gap identification
- User experience investigations

#### Problem score calculation

Starting from 0, points are added for problems:
- High risks (15+): +10 points each
- Medium risks (10-14): +5 points each
- Open risks: +3 points each
- Critical issues: +15 points each
- High severity issues: +8 points each
- Blocked issues: +10 points each
- Open/in-progress issues: +2 points each
- Overdue actions: +3 points each
- Delayed milestones: +8 points each
- User satisfaction penalty: If <70%, add (70-satisfaction)/2 points

**Threshold:**
- Products with problem score > 50 are flagged as "Needs Attention"
- Products with score 20-50 should be monitored
- Products with score < 20 are considered healthy

#### Data sources

The analysis combines:
1. **RAID data** - Real-time from Compass database
2. **User satisfaction** - Latest submitted performance metrics
3. **Product information** - Live from FIPS CMS API
4. **Reporting status** - Product return submission dates

## Navigation

The Reports section appears in the main sidebar navigation with two menu items:
- **Risks and issues** - League table view
- **Analysis** - Advanced analytics dashboard

## Technical implementation

### Controller

`ReportsController.cs` provides:
- `RisksAndIssues()` action - Generates league table
- `Analysis()` action - Generates analytical dashboard
- `CalculateHealthScore()` - Health score algorithm
- `CalculateProblemScore()` - Problem score algorithm

### View models

**ProductRaidReport:**
- Used by Risks and Issues report
- Contains RAID counts and health score
- Properties: FipsId, ProductTitle, various counts, HealthScore

**ProductAnalysisReport:**
- Used by Analysis report
- Contains comprehensive analytics
- Properties: FipsId, ProductTitle, UserSatisfaction, RAID indicators, ProblemScore, NeedsAttention, etc.

### Views

Located in `/Views/Reports/`:
- `RisksAndIssues.cshtml` - League table with legend
- `Analysis.cshtml` - Dashboard with insights and recommendations

## Use cases

### Portfolio management

Leadership can:
- Quickly identify which products need intervention
- Understand portfolio-wide RAID exposure
- Compare products objectively using health/problem scores
- Track trends over time (future enhancement)

### Product oversight

Product owners can:
- See their product's ranking in the portfolio
- Understand what's impacting their health score
- Compare their product against others
- Get specific recommendations for improvement

### Risk management

Risk managers can:
- Identify products with highest risk exposure
- Prioritise risk treatment efforts
- Track risk mitigation effectiveness
- Report on portfolio risk profile

### Issue management

Service desk teams can:
- Identify products with most critical issues
- Find long-running issues needing escalation
- Track blocked items requiring intervention
- Monitor issue resolution performance

## Example scenarios

### Scenario 1: Monthly portfolio review

1. Navigate to **Reports > Analysis**
2. Review executive summary for overall health
3. Focus on products flagged as "Needs Attention"
4. Review key insights for portfolio-wide trends
5. Follow recommended actions
6. Drill into specific products for details

### Scenario 2: Emergency escalation

1. Navigate to **Reports > Risks and Issues**
2. Look for products with health score < 30 (red highlighting)
3. Check critical issues and high risks columns
4. Identify products with multiple blocked issues
5. Take immediate action on worst performers

### Scenario 3: Proactive monitoring

1. Navigate to **Reports > Analysis**
2. Review products in "Monitor" status (score 20-50)
3. Check user satisfaction trends
4. Identify early warning signs
5. Prevent products from deteriorating to "Needs Attention"

## Best practices

### Regular review cadence

- **Weekly:** Review Analysis dashboard for new problems
- **Monthly:** Full portfolio review using both reports
- **Quarterly:** Trend analysis and strategic planning

### Action thresholds

- **Problem score > 70:** Immediate intervention required
- **Problem score 50-70:** Review within 1 week
- **Problem score 20-50:** Monitor, review monthly
- **Problem score < 20:** Business as usual

### Data quality

Ensure accuracy by:
- Keeping RAID items up to date
- Closing resolved risks and issues promptly
- Linking items to correct products
- Submitting metrics on time
- Recording actual completion dates

## Future enhancements

Potential additions:
1. **Trend analysis** - Historical tracking of scores
2. **Filters and drill-down** - Filter by severity, status, date ranges
3. **Export functionality** - Download reports to Excel/PDF
4. **Automated alerts** - Email notifications when scores deteriorate
5. **Benchmarking** - Compare against portfolio averages
6. **Predictive analytics** - Forecast future problems
7. **Custom scoring** - Configurable weightings for calculations
8. **API endpoints** - Programmatic access to report data

## Performance considerations

The reports query multiple database tables and external APIs. For large portfolios:
- Data is loaded asynchronously
- Products without RAID items show zeroes (not excluded)
- Error handling ensures graceful degradation
- Consider caching for very large datasets (future enhancement)

## Accessibility

The reports follow GOV.UK design system standards:
- Semantic HTML structure
- ARIA labels for screen readers
- Keyboard navigation support
- Colour-blind friendly indicators (not relying solely on colour)
- High contrast mode compatibility

---

**Created:** 17 October 2025  
**Controller:** ReportsController.cs  
**Views:** RisksAndIssues.cshtml, Analysis.cshtml  
**Version:** 1.0

