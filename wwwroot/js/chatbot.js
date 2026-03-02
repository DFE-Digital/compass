// Help Chatbot JavaScript

(function() {
    'use strict';

    // Storage keys
    const STORAGE_KEY_OPEN = 'help-chatbot-open';
    const STORAGE_KEY_MESSAGES = 'help-chatbot-messages';
    const STORAGE_KEY_SESSION_START = 'help-chatbot-session-start';

    // Expanded knowledge base with actual COMPASS features
    const knowledgeBase = {
        // Milestones
        'update milestone': {
            title: 'Update a milestone',
            content: `<p>To update a milestone:</p>
                <ol>
                    <li>Navigate to the project containing the milestone</li>
                    <li>Click on the "Milestones" tab</li>
                    <li>Find the milestone you want to update and click on it</li>
                    <li>Click the "Edit" button or update individual fields directly</li>
                    <li>You can update:
                        <ul>
                            <li>Name</li>
                            <li>Description</li>
                            <li>Due date</li>
                            <li>Status (Not Started, In Progress, Completed, etc.)</li>
                            <li>Progress percentage</li>
                            <li>Notes</li>
                            <li>Business area</li>
                        </ul>
                    </li>
                    <li>Click "Save" to save your changes</li>
                </ol>
                <p>You can also add milestone updates to track progress over time.</p>`
        },
        'remove milestone': {
            title: 'Remove a milestone',
            content: `<p>To remove a milestone:</p>
                <ol>
                    <li>Navigate to the project containing the milestone</li>
                    <li>Go to the "Milestones" tab</li>
                    <li>Click on the milestone to view its details</li>
                    <li>Click the "Delete" button</li>
                    <li>Confirm the deletion when prompted</li>
                </ol>
                <p><strong>Warning:</strong> Deleting a milestone cannot be undone. The milestone will be marked as deleted (soft delete).</p>`
        },
        'create milestone': {
            title: 'Create a new milestone',
            content: `<p>To create a new milestone:</p>
                <ol>
                    <li>Go to the project where you want to add a milestone</li>
                    <li>Click on the "Milestones" tab</li>
                    <li>Click the "Add Milestone" or "New Milestone" button</li>
                    <li>Fill in the required information:
                        <ul>
                            <li>Name (required)</li>
                            <li>Description</li>
                            <li>Due date (required)</li>
                            <li>Status</li>
                            <li>Progress percentage</li>
                            <li>Link to objectives (optional)</li>
                        </ul>
                    </li>
                    <li>Click "Save" to create the milestone</li>
                </ol>`
        },
        // Delivery Reporting
        'submit metrics': {
            title: 'Submit performance metrics',
            content: `<p>To submit performance metrics for your product:</p>
                <ol>
                    <li>Navigate to <strong>Delivery reporting</strong> in the left sidebar</li>
                    <li>Click <strong>Operational reports</strong></li>
                    <li>Find your product in the list and click on it</li>
                    <li>Click <strong>Submit metrics</strong></li>
                    <li>Enter values for each metric:
                        <ul>
                            <li>User satisfaction (0-100%)</li>
                            <li>Service availability (0-100%)</li>
                            <li>Transaction volumes</li>
                            <li>Other product-specific metrics</li>
                        </ul>
                    </li>
                    <li>Add any notes or commentary to provide context</li>
                    <li>Click <strong>Submit</strong></li>
                </ol>
                <p>Metrics are submitted monthly and tracked over time to show trends.</p>`
        },
        'operational reporting': {
            title: 'Operational reporting',
            content: `<p>Operational reporting helps you track product performance:</p>
                <ul>
                    <li><strong>Submit monthly metrics:</strong> Report on user satisfaction, availability, and transaction volumes</li>
                    <li><strong>View trends:</strong> See how your product's performance changes over time</li>
                    <li><strong>Track completion:</strong> Monitor which products have submitted their returns</li>
                    <li><strong>RAG status:</strong> See Red, Amber, Green indicators for service health</li>
                </ul>
                <p>Access operational reports from <strong>Delivery reporting > Operational reports</strong> in the sidebar.</p>`
        },
        'delivery reporting': {
            title: 'Delivery reporting overview',
            content: `<p>Delivery reporting in COMPASS enables you to:</p>
                <ul>
                    <li><strong>Track project delivery:</strong> Monitor project lifecycle, milestones, deliverables, and outcomes</li>
                    <li><strong>Monitor operational performance:</strong> Track product performance metrics and visualise trends</li>
                    <li><strong>Compliance tracking:</strong> Conduct functional standards assessments and track compliance</li>
                    <li><strong>Strategic alignment:</strong> Link projects to objectives and track progress</li>
                </ul>
                <p>Access delivery reporting from the left sidebar.</p>`
        },
        // Standards
        'functional standards': {
            title: 'Functional standards assessments',
            content: `<p>To conduct a functional standards assessment:</p>
                <ol>
                    <li>Navigate to <strong>Standards</strong> in the left sidebar</li>
                    <li>Click <strong>Functional standards assessments</strong></li>
                    <li>Select a standard theme and practice area</li>
                    <li>Review the criteria for each standard</li>
                    <li>Provide evidence and rate compliance levels</li>
                    <li>Submit your assessment</li>
                </ol>
                <p>Functional standards help ensure your products meet DDaT (Digital, Data and Technology) standards.</p>`
        },
        'ddt standards': {
            title: 'DDT Standards management',
            content: `<p>DDT (Digital, Data and Technology) Standards in COMPASS:</p>
                <ul>
                    <li><strong>Published standards:</strong> Browse fully assured standards ready to reference</li>
                    <li><strong>Draft standards:</strong> Review standards in progress through peer review and approvals</li>
                    <li><strong>Create standards:</strong> Draft new DDT standards with categories, stages, and owners</li>
                    <li><strong>Service standards:</strong> Access service-specific standards and guidance</li>
                </ul>
                <p>Access standards from <strong>Standards</strong> in the left sidebar.</p>`
        },
        // Apps
        'accessibility': {
            title: 'Accessibility management',
            content: `<p>To manage accessibility for your products:</p>
                <ol>
                    <li>Navigate to <strong>Apps</strong> in the left sidebar</li>
                    <li>Click <strong>Accessibility Issues and Statements</strong></li>
                    <li>You can:
                        <ul>
                            <li>Track and manage accessibility issues</li>
                            <li>Maintain accessibility statements</li>
                            <li>Monitor compliance status</li>
                            <li>Record audit history</li>
                            <li>Manage product enrollment</li>
                        </ul>
                    </li>
                </ol>
                <p>Accessibility issues are tracked against WCAG criteria and can be linked to products.</p>`
        },
        'accessibility issues': {
            title: 'Manage accessibility issues',
            content: `<p>To manage accessibility issues:</p>
                <ol>
                    <li>Go to <strong>Apps > Accessibility Issues and Statements</strong></li>
                    <li>View all issues or filter by product, status, or WCAG level</li>
                    <li>Click on an issue to view details</li>
                    <li>Update the issue status, add resolution notes, or link to WCAG criteria</li>
                    <li>Add comments and track issue history</li>
                </ol>
                <p>Issues can be tracked from identification through to resolution and verification.</p>`
        },
        // Projects
        'create project': {
            title: 'Create a new project',
            content: `<p>To create a new project:</p>
                <ol>
                    <li>Navigate to <strong>Projects</strong> in the left sidebar</li>
                    <li>Click <strong>New Project</strong> or <strong>Create Project</strong></li>
                    <li>Fill in the project details:
                        <ul>
                            <li>Project name and description</li>
                            <li>Business area</li>
                            <li>Project status</li>
                            <li>Delivery priority</li>
                            <li>Funding sources</li>
                            <li>Team members and contacts</li>
                        </ul>
                    </li>
                    <li>Click <strong>Save</strong> to create the project</li>
                </ol>`
        },
        'project dashboard': {
            title: 'Project dashboard',
            content: `<p>The project dashboard shows:</p>
                <ul>
                    <li><strong>Project overview:</strong> Key information, status, and RAG indicators</li>
                    <li><strong>Milestones:</strong> Track project milestones and deadlines</li>
                    <li><strong>RAID items:</strong> Risks, Actions, Issues, and Decisions</li>
                    <li><strong>Objectives:</strong> Strategic objectives linked to the project</li>
                    <li><strong>Team:</strong> Project contacts and team members</li>
                    <li><strong>Funding:</strong> Funding sources and allocations</li>
                </ul>
                <p>Access your project dashboard by clicking on any project from the Projects list.</p>`
        },
        // RAID
        'add risk': {
            title: 'Add a risk to a project',
            content: `<p>To add a risk:</p>
                <ol>
                    <li>Navigate to your project</li>
                    <li>Go to the <strong>Risks</strong> tab</li>
                    <li>Click <strong>Add Risk</strong> or <strong>New Risk</strong></li>
                    <li>Fill in the risk details:
                        <ul>
                            <li>Risk title and description</li>
                            <li>Risk type and tier</li>
                            <li>Likelihood and impact</li>
                            <li>Risk score (calculated automatically)</li>
                            <li>Owner and mitigation actions</li>
                        </ul>
                    </li>
                    <li>Click <strong>Save</strong></li>
                </ol>`
        },
        'add issue': {
            title: 'Add an issue to a project',
            content: `<p>To add an issue:</p>
                <ol>
                    <li>Navigate to your project</li>
                    <li>Go to the <strong>Issues</strong> tab</li>
                    <li>Click <strong>Add Issue</strong> or <strong>New Issue</strong></li>
                    <li>Fill in the issue details:
                        <ul>
                            <li>Issue title and description</li>
                            <li>Status (Open, In Progress, Resolved, Closed)</li>
                            <li>Priority and severity</li>
                            <li>Owner and resolution plan</li>
                        </ul>
                    </li>
                    <li>Click <strong>Save</strong></li>
                </ol>`
        },
        'add action': {
            title: 'Add an action to a project',
            content: `<p>To add an action:</p>
                <ol>
                    <li>Navigate to your project</li>
                    <li>Go to the <strong>Actions</strong> tab</li>
                    <li>Click <strong>Add Action</strong> or <strong>New Action</strong></li>
                    <li>Fill in the action details:
                        <ul>
                            <li>Action title and description</li>
                            <li>Owner and due date</li>
                            <li>Status (Not Started, In Progress, Completed)</li>
                            <li>Source (e.g., linked to a risk or issue)</li>
                        </ul>
                    </li>
                    <li>Click <strong>Save</strong></li>
                </ol>`
        },
        // Project Features - Overview
        'project overview': {
            title: 'Project overview',
            content: `<p>The project overview shows key information about your project:</p>
                <ul>
                    <li><strong>Priority:</strong> Delivery priority level (e.g., P1, P2, P3)</li>
                    <li><strong>RAG status:</strong> Red, Amber, or Green status indicator</li>
                    <li><strong>Aim:</strong> Project aim and objectives</li>
                    <li><strong>Active milestones:</strong> Upcoming milestones and deadlines</li>
                    <li><strong>Team size:</strong> Permanent and MSP FTE counts</li>
                    <li><strong>Delivery code:</strong> Unique project identifier (DFE-DDT-XXX)</li>
                </ul>
                <p>Access the overview from the project details page.</p>`
        },
        'rag status': {
            title: 'RAG status tracking',
            content: `<p>RAG (Red, Amber, Green) status helps track project health:</p>
                <ul>
                    <li><strong>Green:</strong> Project on track</li>
                    <li><strong>Amber:</strong> Some concerns, monitoring required</li>
                    <li><strong>Red:</strong> Significant issues requiring attention</li>
                </ul>
                <p>To update RAG status:</p>
                <ol>
                    <li>Go to your project</li>
                    <li>Click on the "RAG Tracking" tab</li>
                    <li>Click "Add RAG update"</li>
                    <li>Select the new status and add notes</li>
                    <li>Save the update</li>
                </ol>
                <p>RAG history is tracked over time to show trends.</p>`
        },
        'status update': {
            title: 'Project status updates',
            content: `<p>Status updates provide regular progress reports on your project:</p>
                <ol>
                    <li>Navigate to your project</li>
                    <li>Go to the "Status Updates" tab</li>
                    <li>Click "Add status update"</li>
                    <li>Fill in:
                        <ul>
                            <li>Update narrative (what's happened)</li>
                            <li>Progress summary</li>
                            <li>Key achievements</li>
                            <li>Challenges or blockers</li>
                            <li>Next steps</li>
                        </ul>
                    </li>
                    <li>Save the update</li>
                </ol>
                <p>Status updates help stakeholders stay informed about project progress.</p>`
        },
        'outcomes': {
            title: 'Project outcomes',
            content: `<p>Outcomes track the expected results and benefits of your project:</p>
                <ol>
                    <li>Go to your project</li>
                    <li>Click on the "Outcomes" tab</li>
                    <li>Click "Add outcome" to create a new outcome</li>
                    <li>Fill in:
                        <ul>
                            <li>Outcome title and description</li>
                            <li>Target date</li>
                            <li>Achievement status</li>
                            <li>Measurement criteria</li>
                        </ul>
                    </li>
                    <li>Save the outcome</li>
                </ol>
                <p>Outcomes can be linked to strategic objectives to show alignment.</p>`
        },
        'successes': {
            title: 'Project successes',
            content: `<p>Track key successes and achievements for your project:</p>
                <ol>
                    <li>Navigate to your project</li>
                    <li>Go to the "Successes" tab</li>
                    <li>Click "Add success"</li>
                    <li>Enter the success title and description</li>
                    <li>Optionally mark as reported to SLT</li>
                    <li>Save</li>
                </ol>
                <p>Successes help celebrate achievements and demonstrate value delivery.</p>`
        },
        'kpi': {
            title: 'Project KPIs',
            content: `<p>Key Performance Indicators (KPIs) help measure project success:</p>
                <ol>
                    <li>Go to your project</li>
                    <li>Click on the "KPIs" tab</li>
                    <li>Click "Add KPI"</li>
                    <li>Fill in:
                        <ul>
                            <li>KPI name and description</li>
                            <li>Category</li>
                            <li>Target value</li>
                            <li>Current value</li>
                            <li>Status</li>
                        </ul>
                    </li>
                    <li>Save the KPI</li>
                </ol>
                <p>KPIs can be tracked over time to show progress towards targets.</p>`
        },
        'dependencies': {
            title: 'Project dependencies',
            content: `<p>Dependencies track relationships between your project and others:</p>
                <ol>
                    <li>Navigate to your project</li>
                    <li>Go to the "Dependencies" tab</li>
                    <li>Click "Add dependency"</li>
                    <li>Specify:
                        <ul>
                            <li>Dependency type (blocks, is blocked by, relates to)</li>
                            <li>Related project</li>
                            <li>Description</li>
                            <li>Criticality</li>
                        </ul>
                    </li>
                    <li>Save</li>
                </ol>
                <p>Dependencies help identify potential blockers and coordination needs.</p>`
        },
        'deliverables': {
            title: 'Project deliverables',
            content: `<p>Deliverables track what your project will produce:</p>
                <ol>
                    <li>Go to your project</li>
                    <li>Click on the "Deliverables" tab</li>
                    <li>Add deliverables with:
                        <ul>
                            <li>Deliverable name</li>
                            <li>Description</li>
                            <li>Due date</li>
                            <li>Status</li>
                        </ul>
                    </li>
                </ol>
                <p>Deliverables help track what needs to be completed for project success.</p>`
        },
        'team': {
            title: 'Project team management',
            content: `<p>Manage your project team members:</p>
                <ol>
                    <li>Navigate to your project</li>
                    <li>Go to the "Team" tab</li>
                    <li>Click "Add team member"</li>
                    <li>Enter:
                        <ul>
                            <li>Team member name and email</li>
                            <li>Role on the project</li>
                            <li>Start and end dates (if applicable)</li>
                        </ul>
                    </li>
                    <li>Save</li>
                </ol>
                <p>Team members can be assigned to actions, milestones, and other project items.</p>`
        },
        'strategic alignment': {
            title: 'Strategic alignment',
            content: `<p>Link your project to strategic objectives:</p>
                <ol>
                    <li>Go to your project</li>
                    <li>Click on the "Strategic Alignment" tab</li>
                    <li>Link to objectives:
                        <ul>
                            <li>Select relevant strategic objectives</li>
                            <li>Add alignment notes</li>
                            <li>Link to mission pillars</li>
                        </ul>
                    </li>
                    <li>Save</li>
                </ol>
                <p>Strategic alignment shows how your project contributes to organisational goals.</p>`
        },
        'contacts governance': {
            title: 'Contacts and governance',
            content: `<p>Manage project contacts and governance structure:</p>
                <ol>
                    <li>Navigate to your project</li>
                    <li>Go to the "Contacts & Governance" tab</li>
                    <li>Add contacts:
                        <ul>
                            <li>Project owner</li>
                            <li>Senior responsible owner (SRO)</li>
                            <li>Project manager</li>
                            <li>Stakeholders</li>
                        </ul>
                    </li>
                    <li>Set up governance meetings and reporting</li>
                </ol>
                <p>Contacts help ensure the right people are involved and informed.</p>`
        },
        'artefacts': {
            title: 'Project artefacts',
            content: `<p>Store and manage project documents and artefacts:</p>
                <ol>
                    <li>Go to your project</li>
                    <li>Click on the "Artefacts" tab</li>
                    <li>Click "Add artefact"</li>
                    <li>Upload or link to:
                        <ul>
                            <li>Project documents</li>
                            <li>Designs and specifications</li>
                            <li>Meeting notes</li>
                            <li>Reports</li>
                        </ul>
                    </li>
                    <li>Add metadata (title, description, type)</li>
                    <li>Save</li>
                </ol>
                <p>Artefacts provide a central repository for project documentation.</p>`
        },
        'project settings': {
            title: 'Project settings',
            content: `<p>Configure project settings and details:</p>
                <ol>
                    <li>Navigate to your project</li>
                    <li>Go to the "Settings" tab</li>
                    <li>Update:
                        <ul>
                            <li>Project title and description</li>
                            <li>Business area</li>
                            <li>Status</li>
                            <li>Delivery priority</li>
                            <li>Funding sources</li>
                            <li>Project dates</li>
                        </ul>
                    </li>
                    <li>Save changes</li>
                </ol>
                <p>Settings control how your project appears and behaves in COMPASS.</p>`
        },
        'needs problem statements': {
            title: 'Needs and problem statements',
            content: `<p>Document what your project is trying to solve:</p>
                <ol>
                    <li>Go to your project</li>
                    <li>Find the "Needs" or "Problem Statements" section</li>
                    <li>Click "Add need" or "Add problem statement"</li>
                    <li>Describe:
                        <ul>
                            <li>The need or problem</li>
                            <li>Source of the need</li>
                            <li>Validation status</li>
                        </ul>
                    </li>
                    <li>Save</li>
                </ol>
                <p>Needs and problem statements help clarify why the project exists.</p>`
        },
        'mission pillars': {
            title: 'Mission pillars',
            content: `<p>Link your project to mission pillars:</p>
                <ol>
                    <li>Navigate to your project</li>
                    <li>Go to strategic alignment or settings</li>
                    <li>Select relevant mission pillars</li>
                    <li>Add alignment notes</li>
                    <li>Save</li>
                </ol>
                <p>Mission pillars show how your project supports organisational missions.</p>`
        },
        'funding': {
            title: 'Project funding',
            content: `<p>Track funding sources and allocations:</p>
                <ol>
                    <li>Go to your project</li>
                    <li>Navigate to funding or financial sections</li>
                    <li>Add funding sources:
                        <ul>
                            <li>Funding source type</li>
                            <li>Amount allocated</li>
                            <li>Financial year</li>
                            <li>Notes</li>
                        </ul>
                    </li>
                    <li>Save</li>
                </ol>
                <p>Funding tracking helps manage project budgets and financial reporting.</p>`
        },
        // Reports
        'reports': {
            title: 'Reports and analytics',
            content: `<p>COMPASS provides several reports:</p>
                <ul>
                    <li><strong>Risks and issues by product:</strong> League table ranking products by health score</li>
                    <li><strong>Product analysis:</strong> Advanced analytics identifying products needing attention</li>
                    <li><strong>Flagship projects:</strong> Overview of key strategic projects</li>
                    <li><strong>Accessibility report:</strong> Summary of accessibility compliance across products</li>
                </ul>
                <p>Access reports from <strong>Reports</strong> in the left sidebar.</p>`
        },
        // Data queries (these will be handled dynamically)
        'my milestones': {
            title: 'Your upcoming milestones',
            content: '<p>Loading your milestones...</p>'
        },
        'my issues': {
            title: 'Your high priority issues',
            content: '<p>Loading your issues...</p>'
        },
        'my risks': {
            title: 'Your high priority risks',
            content: '<p>Loading your risks...</p>'
        },
        // Default
        'default': {
            title: 'How can I help?',
            content: `<p>I can help you with tasks across COMPASS. Try asking me about:</p>
                <ul>
                    <li><strong>Your data:</strong> "What milestones are coming up?", "Any high priority issues?", "What risks do I have?"</li>
                    <li><strong>Milestones:</strong> "Update my milestones", "Remove a milestone", "Create a milestone"</li>
                    <li><strong>Delivery reporting:</strong> "Submit metrics", "Operational reporting"</li>
                    <li><strong>Standards:</strong> "Functional standards", "DDT standards"</li>
                    <li><strong>Apps:</strong> "Accessibility", "Accessibility issues"</li>
                    <li><strong>Projects:</strong> "Create project", "Project dashboard"</li>
                    <li><strong>Reports:</strong> "Reports", "Product analysis"</li>
                </ul>
                <p>Or type your question in the box below!</p>`
        }
    };

    // Question patterns with expanded coverage
    const questionPatterns = [
        // Data queries (check these first as they need API calls)
        { pattern: /what.*milestone|milestone.*coming|upcoming.*milestone|my.*milestone/i, key: 'my milestones', isDataQuery: true },
        { pattern: /what.*issue|high.*priority.*issue|my.*issue|any.*issue/i, key: 'my issues', isDataQuery: true },
        { pattern: /what.*risk|high.*priority.*risk|my.*risk|any.*risk/i, key: 'my risks', isDataQuery: true },
        { pattern: /what.*project.*is.*|projects.*is.*|which.*project.*is/i, key: 'user projects query', isDataQuery: true },
        // Milestones
        { pattern: /update.*milestone|milestone.*update|edit.*milestone|milestone.*edit/i, key: 'update milestone' },
        { pattern: /remove.*milestone|delete.*milestone|milestone.*remove|milestone.*delete/i, key: 'remove milestone' },
        { pattern: /create.*milestone|add.*milestone|new.*milestone|milestone.*create|milestone.*add/i, key: 'create milestone' },
        // Reporting
        { pattern: /submit.*metric|metric.*submit|performance.*metric|operational.*report/i, key: 'submit metrics' },
        { pattern: /operational.*report|report.*operational/i, key: 'operational reporting' },
        { pattern: /delivery.*report|report.*delivery/i, key: 'delivery reporting' },
        // Standards
        { pattern: /functional.*standard|standard.*functional|ddat.*standard/i, key: 'functional standards' },
        { pattern: /ddt.*standard|standard.*ddt/i, key: 'ddt standards' },
        // Accessibility
        { pattern: /accessibility|wcag|a11y/i, key: 'accessibility' },
        { pattern: /accessibility.*issue|issue.*accessibility/i, key: 'accessibility issues' },
        // Projects
        { pattern: /create.*project|new.*project|add.*project/i, key: 'create project' },
        { pattern: /project.*dashboard|dashboard.*project|project.*overview/i, key: 'project overview' },
        { pattern: /rag.*status|status.*rag|rag.*tracking/i, key: 'rag status' },
        { pattern: /status.*update|update.*status|project.*status/i, key: 'status update' },
        { pattern: /outcome|outcomes/i, key: 'outcomes' },
        { pattern: /success|successes/i, key: 'successes' },
        { pattern: /\bkpi\b|kpis|key.*performance/i, key: 'kpi' },
        { pattern: /dependency|dependencies/i, key: 'dependencies' },
        { pattern: /deliverable|deliverables/i, key: 'deliverables' },
        { pattern: /team.*member|project.*team|add.*team/i, key: 'team' },
        { pattern: /strategic.*alignment|alignment|objective/i, key: 'strategic alignment' },
        { pattern: /contact|governance|sro|owner/i, key: 'contacts governance' },
        { pattern: /artefact|artefacts|document/i, key: 'artefacts' },
        { pattern: /project.*setting|configure.*project/i, key: 'project settings' },
        { pattern: /need|problem.*statement/i, key: 'needs problem statements' },
        { pattern: /mission.*pillar|pillar/i, key: 'mission pillars' },
        { pattern: /funding|budget|financial/i, key: 'funding' },
        // Reports
        { pattern: /report|analytics|analysis/i, key: 'reports' }
    ];

    class HelpChatbot {
        constructor() {
            this.isOpen = false;
            this.messages = [];
            this.sessionStart = null;
            this.userAvatarUrl = null;
            this.selectedProject = null;
            this.userProjects = [];
            this.pendingProjectQuestion = null;
            this.init();
        }

        init() {
            this.loadState();
            this.loadUserAvatar();
            this.createChatbotHTML();
            this.attachEventListeners();
            this.restoreMessages();
        }

        loadState() {
            // Load open/closed state
            const savedOpen = localStorage.getItem(STORAGE_KEY_OPEN);
            this.isOpen = savedOpen === 'true';

            // Load session start
            const savedStart = localStorage.getItem(STORAGE_KEY_SESSION_START);
            if (savedStart) {
                this.sessionStart = new Date(savedStart);
            }
        }

        async loadUserAvatar() {
            try {
                const response = await fetch('/api/v1/chatbot/user-photo');
                if (response.ok) {
                    const blob = await response.blob();
                    this.userAvatarUrl = URL.createObjectURL(blob);
                }
            } catch (error) {
                console.log('Could not load user avatar:', error);
            }
        }

        createChatbotHTML() {
            const chatbot = document.createElement('div');
            chatbot.className = 'help-chatbot';
            chatbot.innerHTML = `
                <button class="help-chatbot__button" 
                        aria-label="Open help chatbot" 
                        aria-expanded="${this.isOpen}"
                        id="chatbot-toggle">
                    <i class="fas fa-compass" aria-hidden="true"></i>
                </button>
                <div class="help-chatbot__window" 
                     aria-hidden="${!this.isOpen}" 
                     role="dialog" 
                     aria-labelledby="chatbot-title"
                     aria-modal="true"
                     id="chatbot-window">
                    <div class="help-chatbot__header">
                        <h3 id="chatbot-title">Help & Support</h3>
                        <div class="help-chatbot__header-actions">
                            <button class="help-chatbot__end-chat" 
                                    aria-label="End chat session"
                                    id="chatbot-end-chat">
                                End chat
                            </button>
                            <button class="help-chatbot__close" 
                                    aria-label="Close chatbot"
                                    id="chatbot-close">
                                <i class="fas fa-times" aria-hidden="true"></i>
                            </button>
                        </div>
                    </div>
                    <div class="help-chatbot__messages" 
                         id="chatbot-messages"
                         role="log"
                         aria-live="polite"
                         aria-atomic="false"
                         tabindex="0">
                    </div>
                    <div class="help-chatbot__suggestions" id="chatbot-suggestions">
                        <button class="help-chatbot__suggestion" data-suggestion="What milestones are coming up?" type="button">What milestones are coming up?</button>
                        <button class="help-chatbot__suggestion" data-suggestion="Any high priority issues?" type="button">Any high priority issues?</button>
                        <button class="help-chatbot__suggestion" data-suggestion="Update my RAG status" type="button">Update my RAG status</button>
                    </div>
                    <div class="help-chatbot__input-area">
                        <input type="text" 
                               class="help-chatbot__input" 
                               id="chatbot-input" 
                               placeholder="Ask me how to do something..."
                               aria-label="Type your question"
                               aria-describedby="chatbot-input-hint">
                        <span id="chatbot-input-hint" class="govuk-visually-hidden">Press Enter to send your message</span>
                        <button class="help-chatbot__send" 
                                id="chatbot-send"
                                aria-label="Send message"
                                type="button">
                            Send
                        </button>
                    </div>
                </div>
            `;
            document.body.appendChild(chatbot);

            // Add end chat confirmation modal
            const modal = document.createElement('div');
            modal.className = 'modal fade';
            modal.id = 'endChatModal';
            modal.setAttribute('tabindex', '-1');
            modal.setAttribute('role', 'dialog');
            modal.setAttribute('aria-labelledby', 'endChatModalLabel');
            modal.setAttribute('aria-hidden', 'true');
            modal.innerHTML = `
                <div class="modal-dialog" role="document">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="endChatModalLabel">End chat session</h5>
                            <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                                <span aria-hidden="true">&times;</span>
                            </button>
                        </div>
                        <div class="modal-body">
                            <p>Are you sure you want to end this chat session?</p>
                            <p class="text-muted">The conversation will be saved and the chat window will be closed and reset.</p>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancel</button>
                            <button type="button" class="btn btn-primary" id="confirmEndChat">End chat</button>
                        </div>
                    </div>
                </div>
            `;
            document.body.appendChild(modal);

            // Restore open state
            if (this.isOpen) {
                this.open();
            }
        }

        attachEventListeners() {
            const toggle = document.getElementById('chatbot-toggle');
            const close = document.getElementById('chatbot-close');
            const endChat = document.getElementById('chatbot-end-chat');
            const send = document.getElementById('chatbot-send');
            const input = document.getElementById('chatbot-input');
            const suggestions = document.querySelectorAll('.help-chatbot__suggestion');
            const confirmEndChat = document.getElementById('confirmEndChat');

            toggle.addEventListener('click', () => this.toggle());
            close.addEventListener('click', () => this.close());
            endChat.addEventListener('click', () => this.showEndChatModal());
            if (confirmEndChat) {
                confirmEndChat.addEventListener('click', () => this.confirmEndChat());
            }
            send.addEventListener('click', () => this.handleSend());
            
            input.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    this.handleSend();
                }
            });

            input.addEventListener('keydown', (e) => {
                // Allow Escape to close, but don't prevent default for other keys
                if (e.key === 'Escape') {
                    this.close();
                }
            });

            suggestions.forEach(suggestion => {
                suggestion.addEventListener('click', (e) => {
                    const text = e.target.getAttribute('data-suggestion');
                    input.value = text;
                    this.handleSend();
                });
            });

            // Trap focus within dialog when open
            const window = document.getElementById('chatbot-window');
            window.addEventListener('keydown', (e) => {
                if (e.key === 'Tab' && this.isOpen) {
                    this.trapFocus(e);
                }
            });
        }

        trapFocus(e) {
            const window = document.getElementById('chatbot-window');
            const focusableElements = window.querySelectorAll(
                'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
            );
            const firstElement = focusableElements[0];
            const lastElement = focusableElements[focusableElements.length - 1];

            if (e.shiftKey && document.activeElement === firstElement) {
                e.preventDefault();
                lastElement.focus();
            } else if (!e.shiftKey && document.activeElement === lastElement) {
                e.preventDefault();
                firstElement.focus();
            }
        }

        toggle() {
            if (this.isOpen) {
                this.close();
            } else {
                this.open();
            }
        }

        open() {
            this.isOpen = true;
            const toggle = document.getElementById('chatbot-toggle');
            const window = document.getElementById('chatbot-window');

            toggle.setAttribute('aria-expanded', 'true');
            window.setAttribute('aria-hidden', 'false');

            // Save state
            localStorage.setItem(STORAGE_KEY_OPEN, 'true');

            // Start session if not already started
            if (!this.sessionStart) {
                this.sessionStart = new Date();
                localStorage.setItem(STORAGE_KEY_SESSION_START, this.sessionStart.toISOString());
            }

            // Focus on input
            setTimeout(() => {
                document.getElementById('chatbot-input').focus();
            }, 100);
        }

        close() {
            this.isOpen = false;
            const toggle = document.getElementById('chatbot-toggle');
            const window = document.getElementById('chatbot-window');

            toggle.setAttribute('aria-expanded', 'false');
            window.setAttribute('aria-hidden', 'true');

            // Save state
            localStorage.setItem(STORAGE_KEY_OPEN, 'false');
        }

        showEndChatModal() {
            if (typeof $ !== 'undefined' && $.fn.modal) {
                $('#endChatModal').modal('show');
            } else {
                // Fallback if Bootstrap/jQuery not available
                const modal = document.getElementById('endChatModal');
                if (modal) {
                    modal.style.display = 'block';
                    modal.setAttribute('aria-hidden', 'false');
                    modal.classList.add('show');
                }
            }
        }

        async confirmEndChat() {
            // Close modal
            if (typeof $ !== 'undefined' && $.fn.modal) {
                $('#endChatModal').modal('hide');
            } else {
                const modal = document.getElementById('endChatModal');
                if (modal) {
                    modal.style.display = 'none';
                    modal.setAttribute('aria-hidden', 'true');
                    modal.classList.remove('show');
                }
            }

            // Save and clear
            await this.saveConversation(true);
            this.clearChat();
            this.close();
        }

        clearChat() {
            this.messages = [];
            this.sessionStart = null;
            localStorage.removeItem(STORAGE_KEY_MESSAGES);
            localStorage.removeItem(STORAGE_KEY_SESSION_START);
            
            const messagesContainer = document.getElementById('chatbot-messages');
            messagesContainer.innerHTML = '';
            
            this.addWelcomeMessage();
        }

        restoreMessages() {
            const saved = localStorage.getItem(STORAGE_KEY_MESSAGES);
            if (saved) {
                try {
                    this.messages = JSON.parse(saved);
                    const messagesContainer = document.getElementById('chatbot-messages');
                    messagesContainer.innerHTML = '';
                    
                    this.messages.forEach(msg => {
                        this.renderMessage(msg.type, msg.title, msg.content, false);
                    });
                    
                    // Attach project selection listeners if any messages contain project options
                    setTimeout(() => {
                        this.attachProjectSelectionListeners();
                    }, 100);
                    
                    // Scroll to bottom
                    messagesContainer.scrollTop = messagesContainer.scrollHeight;
                } catch (e) {
                    console.error('Error restoring messages:', e);
                    this.addWelcomeMessage();
                }
            } else {
                this.addWelcomeMessage();
            }
        }

        addWelcomeMessage() {
            const welcome = knowledgeBase['default'];
            this.addMessage('bot', welcome.title, welcome.content);
        }

        addMessage(type, title, content) {
            this.messages.push({ 
                type, 
                title, 
                content, 
                timestamp: new Date().toISOString() 
            });
            
            // Save to localStorage
            localStorage.setItem(STORAGE_KEY_MESSAGES, JSON.stringify(this.messages));
            
            this.renderMessage(type, title, content, true);
        }

        renderMessage(type, title, content, animate) {
            const messagesContainer = document.getElementById('chatbot-messages');
            const message = document.createElement('div');
            message.className = `help-chatbot__message help-chatbot__message--${type}`;
            if (animate) {
                message.style.animation = 'fadeIn 0.3s ease';
            }
            
            const avatar = document.createElement('div');
            avatar.className = 'help-chatbot__avatar';
            avatar.setAttribute('aria-hidden', 'true');
            
            if (type === 'bot') {
                avatar.innerHTML = '<i class="fas fa-robot"></i>';
            } else {
                if (this.userAvatarUrl) {
                    avatar.innerHTML = `<img src="${this.userAvatarUrl}" alt="Your profile picture">`;
                } else {
                    avatar.innerHTML = '<i class="fas fa-user"></i>';
                }
            }

            const messageContent = document.createElement('div');
            messageContent.className = 'help-chatbot__content';
            if (title) {
                messageContent.innerHTML = `<strong>${title}</strong>${content}`;
            } else {
                messageContent.innerHTML = content;
            }

            message.appendChild(avatar);
            message.appendChild(messageContent);
            messagesContainer.appendChild(message);

            // If this message contains project selection buttons, attach listeners after a brief delay
            if (content.includes('help-chatbot__project-option')) {
                setTimeout(() => {
                    this.attachProjectSelectionListeners();
                }, 100);
            }

            // Scroll to bottom
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
        }

        showTypingIndicator() {
            const messagesContainer = document.getElementById('chatbot-messages');
            const typing = document.createElement('div');
            typing.className = 'help-chatbot__message help-chatbot__message--bot';
            typing.id = 'typing-indicator';
            
            const avatar = document.createElement('div');
            avatar.className = 'help-chatbot__avatar';
            avatar.setAttribute('aria-hidden', 'true');
            avatar.innerHTML = '<i class="fas fa-robot"></i>';

            const typingDots = document.createElement('div');
            typingDots.className = 'help-chatbot__typing';
            typingDots.setAttribute('aria-label', 'Bot is typing');
            typingDots.innerHTML = `
                <div class="help-chatbot__typing-dot"></div>
                <div class="help-chatbot__typing-dot"></div>
                <div class="help-chatbot__typing-dot"></div>
            `;

            typing.appendChild(avatar);
            typing.appendChild(typingDots);
            messagesContainer.appendChild(typing);
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
        }

        hideTypingIndicator() {
            const typing = document.getElementById('typing-indicator');
            if (typing) {
                typing.remove();
            }
        }

        async findAnswer(query) {
            const lowerQuery = query.toLowerCase().trim();

            // Check for data queries first (these need API calls)
            // Check for "what projects is <user> on?" pattern
            const userProjectsMatch = query.match(/what.*projects?.*is\s+([^?]+)\s+on|projects?.*is\s+([^?]+)\s+on/i);
            if (userProjectsMatch) {
                const userName = (userProjectsMatch[1] || userProjectsMatch[2] || '').trim();
                if (userName) {
                    return await this.getUserProjectsByName(userName);
                }
            }

            // Check for milestone queries
            if (/what.*milestone|milestone.*coming|upcoming.*milestone|my.*milestone|when.*milestone/i.test(query)) {
                return await this.getUpcomingMilestones();
            }

            // Check for issue queries
            if (/what.*issue|high.*priority.*issue|my.*issue|any.*issue|critical.*issue/i.test(query)) {
                return await this.getHighPriorityIssues();
            }

            // Check for risk queries (but not if asking about RAID features)
            if (/what.*risk|high.*priority.*risk|my.*risk|any.*risk/i.test(query) && 
                !/raid|feature|enable/i.test(query)) {
                return await this.getHighPriorityRisks();
            }

            // Check for RAID features - these are not enabled
            // Only trigger if asking about managing/creating RAID items, not querying data
            const raidManagementPatterns = [
                /(add|create|new|update|edit|remove|delete).*(risk|action|decision|issue)/i,
                /(risk|action|decision|issue).*(add|create|new|update|edit|remove|delete)/i,
                /manage.*(risk|action|decision|issue)/i,
                /(risk|action|decision|issue).*manage/i
            ];
            
            const isRaidManagementQuery = raidManagementPatterns.some(pattern => pattern.test(query));
            if (isRaidManagementQuery) {
                return {
                    title: 'RAID features not yet enabled',
                    content: `<p>RAID (Risks, Actions, Issues, Decisions) features are not currently enabled in COMPASS.</p>
                        <p>These features are planned for future release. For now, you can:</p>
                        <ul>
                            <li>Track milestones and deliverables</li>
                            <li>Manage project status updates</li>
                            <li>Submit performance metrics</li>
                            <li>Conduct functional standards assessments</li>
                        </ul>
                        <p>If you need to track risks, actions, issues, or decisions, please use alternative project management tools until these features are available.</p>`
                };
            }

            // Check if this is a project-related question that needs project context
            const projectRelatedPatterns = [
                /update.*rag|rag.*status|change.*rag/i,
                /update.*priority|change.*priority|set.*priority/i,
                /update.*milestone|milestone.*update|edit.*milestone/i,
                /add.*milestone|create.*milestone|new.*milestone/i,
                /remove.*milestone|delete.*milestone/i,
                /status.*update|update.*status|add.*status/i,
                /add.*outcome|create.*outcome/i,
                /add.*success|create.*success/i,
                /add.*kpi|create.*kpi/i,
                /add.*dependency|create.*dependency/i,
                /add.*deliverable|create.*deliverable/i,
                /add.*team.*member|create.*team/i,
                /update.*team|change.*team/i,
                /add.*artefact|upload.*document/i,
                /update.*funding|change.*funding/i,
                /update.*aim|change.*aim/i
            ];

            const needsProjectContext = projectRelatedPatterns.some(pattern => pattern.test(query));
            
            if (needsProjectContext && !this.selectedProject) {
                // Need to get projects first
                if (this.userProjects.length === 0) {
                    await this.loadUserProjects();
                }

                if (this.userProjects.length === 0) {
                    return {
                        title: 'No projects found',
                        content: `<p>I couldn't find any projects associated with your account.</p>
                            <p>To update project information, you need to be a contact on a project. If you believe you should have access to projects, please contact the Central Operations team.</p>`
                    };
                }

                // Store the question and show project selection
                this.pendingProjectQuestion = query;
                return this.getProjectSelectionResponse();
            }

            // Check patterns first
            for (const pattern of questionPatterns) {
                if (pattern.pattern.test(lowerQuery)) {
                    const answer = knowledgeBase[pattern.key];
                    // If we have a selected project, add context to the answer
                    if (this.selectedProject && needsProjectContext) {
                        return this.addProjectContext(answer, this.selectedProject);
                    }
                    return answer;
                }
            }

            // Check direct matches
            for (const key in knowledgeBase) {
                if (key !== 'default' && lowerQuery.includes(key)) {
                    const answer = knowledgeBase[key];
                    // If we have a selected project, add context to the answer
                    if (this.selectedProject && needsProjectContext) {
                        return this.addProjectContext(answer, this.selectedProject);
                    }
                    return answer;
                }
            }

            // Default response
            return {
                title: "I'm not sure about that",
                content: `<p>I don't have specific information about "${query}".</p>
                    <p>Try asking me about:</p>
                    <ul>
                        <li>Milestones (update, create, remove)</li>
                        <li>Delivery reporting and metrics</li>
                        <li>Functional standards and DDT standards</li>
                        <li>Accessibility management</li>
                        <li>Projects and RAID items</li>
                        <li>Reports and analytics</li>
                    </ul>
                    <p>Or check the documentation for more detailed help.</p>`
            };
        }

        async loadUserProjects() {
            try {
                const response = await fetch('/api/v1/chatbot/user-projects');
                if (response.ok) {
                    this.userProjects = await response.json();
                }
            } catch (error) {
                console.error('Error loading user projects:', error);
                this.userProjects = [];
            }
        }

        getProjectSelectionResponse() {
            if (this.userProjects.length === 0) {
                return {
                    title: 'No projects found',
                    content: `<p>I couldn't find any projects associated with your account.</p>
                        <p>To update project information, you need to be a contact on a project. If you believe you should have access to projects, please contact your administrator.</p>
                        <p>You can also create a new project from the <strong>Projects</strong> section in the left sidebar.</p>`
                };
            }

            const actionPhrase = this.pendingProjectQuestion ? 
                this.pendingProjectQuestion.toLowerCase().replace(/^(update|change|set|add|create|remove|delete)\s+/i, '') : 
                'do this';

            const projectsList = this.userProjects.map((p, index) => {
                const ragClass = p.ragStatus ? 
                    (p.ragStatus.toLowerCase() === 'green' ? 'success' : 
                     p.ragStatus.toLowerCase() === 'amber' ? 'warning' : 
                     p.ragStatus.toLowerCase() === 'red' ? 'danger' : 'secondary') : 
                    'secondary';
                
                return `<button class="help-chatbot__project-option" 
                                data-project-id="${p.id}" 
                                data-project-title="${p.title}" 
                                type="button"
                                aria-label="Select project ${p.title}">
                            <strong>${p.title}</strong>
                            ${p.code ? `<br><small>${p.code}</small>` : ''}
                            ${p.ragStatus ? `<br><span class="badge badge-${ragClass} mt-1">${p.ragStatus}</span>` : ''}
                            ${p.priority ? `<br><small class="text-muted">Priority: ${p.priority}</small>` : ''}
                        </button>`;
            }).join('');

            return {
                title: 'Which project?',
                content: `<p>For which project would you like to ${actionPhrase}?</p>
                    <div class="help-chatbot__project-selection">
                        ${projectsList}
                    </div>
                    <p class="mt-2"><small class="text-muted">Click on a project to select it</small></p>`
            };
        }

        addProjectContext(answer, project) {
            const projectUrl = `/Project/Details/${project.id}`;
            return {
                title: answer.title,
                content: `${answer.content}
                    <div class="alert alert-info mt-3 mb-0">
                        <strong>Selected project:</strong> <a href="${projectUrl}" target="_blank">${project.title}</a>${project.code ? ` (${project.code})` : ''}
                        <button class="btn btn-sm btn-link p-0 ml-2" type="button" onclick="window.chatbotInstance?.clearProjectSelection()">Change</button>
                    </div>
                    <p class="mt-2"><a href="${projectUrl}" target="_blank" class="btn btn-sm btn-primary">Go to project <i class="fas fa-external-link-alt ml-1"></i></a></p>`
            };
        }

        selectProject(projectId, projectTitle) {
            const project = this.userProjects.find(p => p.id === projectId);
            if (project) {
                this.selectedProject = project;
                
                // Add a message showing the selection
                this.addMessage('user', null, `For project: ${project.title}`);
                
                // Re-answer the pending question with project context
                if (this.pendingProjectQuestion) {
                    setTimeout(async () => {
                        this.showTypingIndicator();
                        setTimeout(async () => {
                            this.hideTypingIndicator();
                            const answer = await this.findAnswer(this.pendingProjectQuestion);
                            this.addMessage('bot', answer.title, answer.content);
                            this.pendingProjectQuestion = null;
                            this.saveConversation(false);
                        }, 500);
                    }, 300);
                }
            }
        }

        clearProjectSelection() {
            this.selectedProject = null;
            this.addMessage('bot', 'Project selection cleared', '<p>You can ask about a specific project again, and I\'ll ask which one.</p>');
        }

        async getUpcomingMilestones() {
            try {
                const response = await fetch('/api/v1/chatbot/upcoming-milestones?days=30');
                if (!response.ok) {
                    return {
                        title: 'Error loading milestones',
                        content: '<p>Sorry, I couldn\'t load your milestones. Please try again later.</p>'
                    };
                }

                const milestones = await response.json();
                
                if (!milestones || milestones.length === 0) {
                    return {
                        title: 'No upcoming milestones',
                        content: '<p>You don\'t have any milestones due in the next 30 days.</p>'
                    };
                }

                const milestonesList = milestones.map(m => {
                    const dueDate = new Date(m.dueDate);
                    const daysUntil = Math.ceil((dueDate - new Date()) / (1000 * 60 * 60 * 24));
                    const statusClass = m.status === 'at_risk' || m.status === 'delayed' ? 'warning' : 
                                       m.status === 'complete' ? 'success' : 'info';
                    const projectLink = m.projectId ? `<a href="/Project/Details/${m.projectId}" target="_blank">${m.projectTitle || 'Project'}</a>` : 'Unknown project';
                    
                    return `<div class="help-chatbot__data-item mb-2 p-2 border rounded">
                        <strong>${m.name}</strong>
                        <br><small>Project: ${projectLink}</small>
                        <br><small>Due: ${dueDate.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })} (${daysUntil} ${daysUntil === 1 ? 'day' : 'days'})</small>
                        ${m.status ? `<br><span class="badge badge-${statusClass}">${m.status.replace('_', ' ')}</span>` : ''}
                        ${m.progressPercent !== null ? `<br><small>Progress: ${m.progressPercent}%</small>` : ''}
                    </div>`;
                }).join('');

                return {
                    title: `Your upcoming milestones (${milestones.length})`,
                    content: `<p>Here are your milestones due in the next 30 days:</p>
                        <div class="help-chatbot__data-list">
                            ${milestonesList}
                        </div>`
                };
            } catch (error) {
                console.error('Error fetching milestones:', error);
                return {
                    title: 'Error loading milestones',
                    content: '<p>Sorry, I couldn\'t load your milestones. Please try again later.</p>'
                };
            }
        }

        async getHighPriorityIssues() {
            try {
                const response = await fetch('/api/v1/chatbot/high-priority-issues');
                if (!response.ok) {
                    return {
                        title: 'Error loading issues',
                        content: '<p>Sorry, I couldn\'t load your issues. Please try again later.</p>'
                    };
                }

                const issues = await response.json();
                
                if (!issues || issues.length === 0) {
                    return {
                        title: 'No high priority issues',
                        content: '<p>Great news! You don\'t have any high priority or critical issues in your projects.</p>'
                    };
                }

                const issuesList = issues.map(i => {
                    const severityClass = i.severity === 'critical' ? 'danger' : 
                                        i.severity === 'high' ? 'warning' : 'info';
                    const projectLink = i.projectId ? `<a href="/Project/Details/${i.projectId}" target="_blank">${i.projectTitle || 'Project'}</a>` : 'Unknown project';
                    const detectedDate = new Date(i.detectedDate).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
                    
                    return `<div class="help-chatbot__data-item mb-2 p-2 border rounded">
                        <strong>${i.title}</strong>
                        ${i.blocked ? '<span class="badge badge-danger ml-2">Blocked</span>' : ''}
                        <br><small>Project: ${projectLink}</small>
                        <br><small>Severity: <span class="badge badge-${severityClass}">${i.severity || 'medium'}</span> | Status: ${i.status || 'open'}</small>
                        <br><small>Detected: ${detectedDate}</small>
                        ${i.targetResolutionDate ? `<br><small>Target resolution: ${new Date(i.targetResolutionDate).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })}</small>` : ''}
                    </div>`;
                }).join('');

                return {
                    title: `Your high priority issues (${issues.length})`,
                    content: `<p>Here are your high priority and critical issues:</p>
                        <div class="help-chatbot__data-list">
                            ${issuesList}
                        </div>`
                };
            } catch (error) {
                console.error('Error fetching issues:', error);
                return {
                    title: 'Error loading issues',
                    content: '<p>Sorry, I couldn\'t load your issues. Please try again later.</p>'
                };
            }
        }

        async getHighPriorityRisks() {
            try {
                const response = await fetch('/api/v1/chatbot/high-priority-risks');
                if (!response.ok) {
                    return {
                        title: 'Error loading risks',
                        content: '<p>Sorry, I couldn\'t load your risks. Please try again later.</p>'
                    };
                }

                const risks = await response.json();
                
                if (!risks || risks.length === 0) {
                    return {
                        title: 'No high priority risks',
                        content: '<p>You don\'t have any high priority risks (score 15+) in your projects.</p>'
                    };
                }

                const risksList = risks.map(r => {
                    const projectLink = r.projectId ? `<a href="/Project/Details/${r.projectId}" target="_blank">${r.projectTitle || 'Project'}</a>` : 'Unknown project';
                    const identifiedDate = new Date(r.identifiedDate).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
                    const riskLevel = r.riskScore >= 20 ? 'danger' : r.riskScore >= 15 ? 'warning' : 'info';
                    
                    return `<div class="help-chatbot__data-item mb-2 p-2 border rounded">
                        <strong>${r.title}</strong>
                        <br><small>Project: ${projectLink}</small>
                        <br><small>Risk score: <span class="badge badge-${riskLevel}">${r.riskScore}</span> (Impact: ${r.impactRating}/5, Likelihood: ${r.likelihoodRating}/5)</small>
                        <br><small>Status: ${r.status || 'new'} | Identified: ${identifiedDate}</small>
                        ${r.proximityDate ? `<br><small>Proximity: ${new Date(r.proximityDate).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })}</small>` : ''}
                    </div>`;
                }).join('');

                return {
                    title: `Your high priority risks (${risks.length})`,
                    content: `<p>Here are your high priority risks (score 15+):</p>
                        <div class="help-chatbot__data-list">
                            ${risksList}
                        </div>`
                };
            } catch (error) {
                console.error('Error fetching risks:', error);
                return {
                    title: 'Error loading risks',
                    content: '<p>Sorry, I couldn\'t load your risks. Please try again later.</p>'
                };
            }
        }

        async getUserProjectsByName(userName) {
            try {
                const response = await fetch(`/api/v1/chatbot/user-projects-by-name?userName=${encodeURIComponent(userName)}`);
                if (!response.ok) {
                    return {
                        title: 'Error loading projects',
                        content: '<p>Sorry, I couldn\'t load projects for that user. Please try again later.</p>'
                    };
                }

                const projects = await response.json();
                
                if (!projects || projects.length === 0) {
                    return {
                        title: `No projects found for ${userName}`,
                        content: `<p>I couldn't find any projects for "${userName}". They may not be a contact on any projects, or the name might be spelled differently.</p>`
                    };
                }

                const projectsList = projects.map(p => {
                    const ragClass = p.ragStatus ? 
                        (p.ragStatus.toLowerCase() === 'green' ? 'success' : 
                         p.ragStatus.toLowerCase() === 'amber' ? 'warning' : 
                         p.ragStatus.toLowerCase() === 'red' ? 'danger' : 'secondary') : 
                        'secondary';
                    
                    return `<div class="help-chatbot__data-item mb-2 p-2 border rounded">
                        <strong><a href="/Project/Details/${p.id}" target="_blank">${p.title}</a></strong>
                        ${p.code ? `<br><small>Code: ${p.code}</small>` : ''}
                        ${p.ragStatus ? `<br><span class="badge badge-${ragClass}">${p.ragStatus}</span>` : ''}
                        ${p.priority ? `<br><small>Priority: ${p.priority}</small>` : ''}
                    </div>`;
                }).join('');

                return {
                    title: `Projects for ${userName} (${projects.length})`,
                    content: `<p>Here are the projects ${userName} is involved in:</p>
                        <div class="help-chatbot__data-list">
                            ${projectsList}
                        </div>`
                };
            } catch (error) {
                console.error('Error fetching user projects:', error);
                return {
                    title: 'Error loading projects',
                    content: '<p>Sorry, I couldn\'t load projects for that user. Please try again later.</p>'
                };
            }
        }

        async handleSend() {
            const input = document.getElementById('chatbot-input');
            const query = input.value.trim();

            if (!query) {
                return;
            }

            await this.handleSendWithQuery(query);
            input.value = '';
        }

        async handleSendWithQuery(query) {
            // Add user message
            this.addMessage('user', null, query);

            // Show typing indicator
            this.showTypingIndicator();

            // Simulate thinking time
            setTimeout(async () => {
                this.hideTypingIndicator();
                const answer = await this.findAnswer(query);
                this.addMessage('bot', answer.title, answer.content);
                
                // Auto-save conversation periodically
                this.saveConversation(false);
            }, 500);
        }

        attachProjectSelectionListeners() {
            const projectOptions = document.querySelectorAll('.help-chatbot__project-option:not([data-listener-attached])');
            projectOptions.forEach(option => {
                option.setAttribute('data-listener-attached', 'true');
                option.addEventListener('click', (e) => {
                    e.preventDefault();
                    const projectId = parseInt(e.currentTarget.getAttribute('data-project-id'));
                    const projectTitle = e.currentTarget.getAttribute('data-project-title');
                    this.selectProject(projectId, projectTitle);
                });
            });
        }

        async saveConversation(ended) {
            if (this.messages.length === 0) {
                return;
            }

            try {
                const messagesDto = this.messages.map(msg => ({
                    type: msg.type,
                    title: msg.title || null,
                    content: msg.content,
                    timestamp: msg.timestamp || new Date().toISOString()
                }));

                const response = await fetch('/api/v1/chatbot/conversation', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        messages: messagesDto,
                        startedAt: this.sessionStart || new Date(),
                        endedAt: ended ? new Date() : null
                    })
                });

                if (!response.ok) {
                    console.error('Failed to save conversation');
                }
            } catch (error) {
                console.error('Error saving conversation:', error);
            }
        }
    }

    // Initialize chatbot when DOM is ready
    let chatbotInstance;
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            chatbotInstance = new HelpChatbot();
            window.chatbotInstance = chatbotInstance; // Make accessible globally for inline handlers
        });
    } else {
        chatbotInstance = new HelpChatbot();
        window.chatbotInstance = chatbotInstance; // Make accessible globally for inline handlers
    }
})();
