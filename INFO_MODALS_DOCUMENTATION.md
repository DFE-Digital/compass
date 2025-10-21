# Info modals documentation

## Overview

Contextual help modals have been added to Risk and Issue create/edit forms to provide guidance on classification fields. Users can click the info icon (<i class="fas fa-info-circle"></i>) next to field labels to view detailed explanations.

## Risk form modals

### Impact rating modal

**Field:** Impact rating (1-5)

**Guidance provided:**
- Definition: Measures the severity of consequences if the risk materialises
- 5-level rating scale with descriptions:
  - **1 (Very low):** Minimal impact, easily managed
  - **2 (Low):** Minor inconvenience, limited disruption
  - **3 (Medium):** Moderate disruption to service
  - **4 (High):** Significant service disruption or financial loss
  - **5 (Very high):** Critical failure, major financial loss, or reputational damage

### Likelihood rating modal

**Field:** Likelihood rating (1-5)

**Guidance provided:**
- Definition: Measures the probability of the risk occurring
- 5-level rating scale with probability ranges:
  - **1 (Very low):** <10% - Highly unlikely to occur
  - **2 (Low):** 10-30% - Unlikely but possible
  - **3 (Medium):** 30-60% - Reasonably likely to occur
  - **4 (High):** 60-85% - Likely to occur
  - **5 (Very high):** >85% - Almost certain to occur
- Risk score calculation: Impact × Likelihood (range 1-25)
  - 15-25: High risk (requires immediate attention)
  - 10-14: Medium risk (monitor and plan mitigation)
  - 1-9: Low risk (accept or monitor)

### Proximity date modal

**Field:** Proximity date

**Guidance provided:**
- Definition: Date when the risk is most likely to materialise or impact the project
- How to determine proximity date:
  - **Event-based risks:** Date of the triggering event (e.g., contract expiry, system cutover)
  - **Continuous risks:** Date when impact threshold is likely to be reached
  - **Uncertain timing:** Best estimate based on current trajectory
- Tip: Risks with proximity dates in the near future should be prioritised for treatment

### Response strategy modal

**Field:** Response strategy

**Guidance provided:**
- Definition: Choose the appropriate risk response strategy
- 4 response strategies with descriptions, when to use, and examples:

| Strategy | Description | When to use | Example |
|----------|-------------|-------------|---------|
| **Avoid** | Eliminate the risk entirely by changing the plan | High impact, unacceptable consequences | Cancel a risky feature, choose different technology |
| **Mitigate** | Reduce the impact or likelihood through actions | Most common; risk is manageable with effort | Add redundancy, increase testing, implement controls |
| **Transfer** | Shift the risk to a third party | Risk can be managed better by others | Insurance, outsource to specialist, warranties |
| **Accept** | Acknowledge the risk and monitor it | Low priority, cost of mitigation exceeds impact | Minor cosmetic issues, low probability events |

### Residual risk modal

**Fields:** Residual impact, Residual likelihood

**Guidance provided:**
- Definition: Expected level of risk after implementing mitigation actions
- How to determine residual risk:
  1. Identify your mitigation actions
  2. Estimate how much they will reduce impact and/or likelihood
  3. Rate the expected residual impact (1-5)
  4. Rate the expected residual likelihood (1-5)
- Example calculation:
  - Current: Impact 5, Likelihood 4 (Score: 20)
  - Mitigation: Implement backup system
  - Residual: Impact 3, Likelihood 2 (Score: 6)
- Target date: When you aim to achieve the residual risk level

## Issue form modals

### Severity modal

**Field:** Severity

**Guidance provided:**
- Definition: Measures the impact and urgency of the issue
- 4 severity levels with descriptions and examples:
  - **Critical:** System down, major functionality broken, severe security issue
    - Examples: Complete service outage, data breach, major bug preventing use
  - **High:** Significant functionality impaired, workaround difficult
    - Examples: Key feature not working, performance severely degraded
  - **Medium:** Moderate impact, workaround available
    - Examples: Minor feature broken, cosmetic issues affecting usability
  - **Low:** Minimal impact, easily worked around
    - Examples: Minor cosmetic issues, spelling errors, nice-to-have features
- Response times:
  - Critical: Immediate action required
  - High: Within 24-48 hours
  - Medium: Within 1 week
  - Low: Within 1 month or next release

### Priority modal

**Field:** Priority

**Guidance provided:**
- Definition: Determines the order in which issues should be addressed
- 3 priority levels:
  - **High:** Must be resolved urgently
    - When: Critical business impact, affecting multiple users, blocking other work
  - **Medium:** Should be resolved soon
    - When: Affecting some users, workaround available, important but not urgent
  - **Low:** Can be resolved when capacity allows
    - When: Minor issues, cosmetic problems, enhancement requests
- Note: Priority is often determined by combining severity with business factors:
  - Number of users affected
  - Business criticality of affected functionality
  - Availability of workarounds
  - Upcoming deadlines or dependencies

### Blocked flag modal

**Field:** Blocked flag (checkbox)

**Guidance provided:**
- Definition: Mark an issue as blocked when progress cannot continue due to external dependencies
- Common blocking scenarios:
  - **External dependencies:** Waiting for third-party vendor, supplier, or partner
  - **Resource constraints:** Required specialist unavailable or budget not approved
  - **Technical blockers:** Dependent on other work being completed first
  - **Decision pending:** Waiting for stakeholder decision or approval
  - **Access issues:** Lack of access to systems, data, or environments
- Important: Blocked issues should be escalated immediately to remove the blocker. Document the blocking reason in the workaround or notes field.
- Tip: When unblocking an issue, update the status to "in_progress" and remove the blocked flag.

## Implementation details

### Location of modals

**Risk forms:**
- `/Views/Risk/Create.cshtml` - 5 modals (Impact, Likelihood, Proximity, Response, Residual)
- `/Views/Risk/Edit.cshtml` - 5 modals (same as Create)

**Issue forms:**
- `/Views/Issue/Create.cshtml` - 3 modals (Severity, Priority, Blocked)
- `/Views/Issue/Edit.cshtml` - 3 modals (same as Create)

### Modal structure

Each modal includes:
- **Header** with descriptive title
- **Body** with:
  - Clear definition of the field
  - Tables or lists with detailed guidance
  - Examples where appropriate
  - Tips and warnings using alert boxes
- **Footer** with close button

### User experience

**How to access:**
1. Click the blue info icon (<i class="fas fa-info-circle"></i>) next to any field label
2. Modal opens with detailed guidance
3. Read the information
4. Click "Close" or press ESC to dismiss

**Visual design:**
- Info icons use text-info colour (blue) for consistency
- Modal dialogs use Bootstrap modal component
- Tables use colour-coded badges matching the field values
- Alert boxes highlight important notes and tips
- Large modals for complex content (e.g., Response strategy)

### Accessibility

**ARIA attributes:**
- `role="dialog"` on modal containers
- `aria-labelledby` linking to modal titles
- `aria-label="Close"` on close buttons
- Keyboard navigation support (ESC to close, TAB navigation)
- Focus management when opening/closing modals

### Bootstrap integration

Modals use Bootstrap 4 modal component:
- `data-toggle="modal"` on trigger links
- `data-target` specifying modal ID
- `.modal-dialog` for container
- `.modal-content` for content wrapper
- `.modal-header`, `.modal-body`, `.modal-footer` for structure

## Benefits

### Improved data quality

- Users understand what each field means before entering data
- Consistent interpretation of severity, priority, and risk ratings
- Reduced errors and misclassification
- Better decision-making with clear examples

### Reduced training time

- Self-service help available at point of need
- No need to reference external documentation
- New users can be productive immediately
- Consistent understanding across teams

### Professional user experience

- Clean, uncluttered forms with help available on demand
- GOV.UK design system compliance
- Modern, intuitive interface
- Context-sensitive help

## Future enhancements

Potential improvements:
1. Add modals to Action and Milestone forms
2. Video tutorials embedded in modals
3. Recent examples from the database
4. Link to related help documentation
5. Tooltips for quick hints (hover without click)
6. Guided wizards for complex classifications
7. Field dependencies and smart defaults

## Maintenance

### Updating guidance

To update modal content:
1. Locate the relevant view file (`Risk/Create.cshtml`, `Issue/Create.cshtml`, etc.)
2. Find the modal section at the bottom (before `@section Scripts`)
3. Update the modal body content
4. Test the changes
5. Update both Create and Edit views for consistency

### Adding new modals

To add a modal to a new field:
1. Add info icon to field label:
   ```html
   <label>
       Field name
       <a href="#" data-toggle="modal" data-target="#myModal" class="text-info">
           <i class="fas fa-info-circle"></i>
       </a>
   </label>
   ```
2. Add modal HTML before `@section Scripts`:
   ```html
   <div class="modal fade" id="myModal" tabindex="-1" role="dialog">
       <!-- Modal content -->
   </div>
   ```

---

**Created:** 17 October 2025  
**Views updated:** 4 (Risk/Create, Risk/Edit, Issue/Create, Issue/Edit)  
**Total modals:** 8  
**Version:** 1.0

