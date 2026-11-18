# NGEOS Website to Native WPF Desktop Feature Mapping & Technical Specification

**Document Version:** 1.0.0  
**Target Solution:** NGEOS Desktop Edition (`NGEOS.Desktop.slnx`)  
**Architecture:** 100% Native WPF / XAML / .NET 10 / C# / MVVM / SQLite WAL & SQL Server Sync  
**Execution Date:** 2026-11-10  

---

## 1. Executive Architecture Summary

This technical inventory and mapping document establishes 1-to-1 parity between the **NGEOS Humanitarian Welfare & Donation Website** (`NGEOS/`) and the **NGEOS Native Desktop Application** (`src/NGEOS.Desktop.UI`).

### Zero Web View Policy
- **No WebView2**, **no embedded CEFSharp**, **no web wrappers**, and **no JavaScript/HTML rendering engines**.
- Every single screen is rendered through genuine, compiled **WPF XAML** with hardware-accelerated DirectX rendering, native layout controls (`Grid`, `StackPanel`, `WrapPanel`, `ScrollViewer`, `Border`), and strong data binding via `CommunityToolkit.Mvvm`.

---

## 2. Comprehensive Website to Desktop Inventory & Parity Matrix

| # | Website Page & Route | Website Implementation (MVC / Views) | Website Data Entities & Tables | Desktop WPF View (`.xaml`) | Desktop ViewModel | Core Desktop Service / Logic | Implementation Status |
|---|----------------------|---------------------------------------|--------------------------------|----------------------------|-------------------|------------------------------|-----------------------|
| **1** | **Homepage** (`/`, `/Home/Index`) | `HomeController.Index` & `Home/Index.cshtml` | `Causes`, `Donations`, `Programmes` | `HomePortalView.xaml` | `HomePortalViewModel` | `NgeosDbContext` aggregate metrics, featured causes, latest donations | ✅ Implemented & Active |
| **2** | **Causes Catalog** (`/Causes`, `/Causes/Index`) | `CausesController.Index` & `Causes/Index.cshtml` | `Cause` (Category, Target, Raised) | `CausesCatalogView.xaml` (Public) / `CausesView.xaml` (Admin) | `CausesCatalogViewModel` / `CausesViewModel` | Category filtering, live search, progress computation, direct donate routing | ✅ Complete Native |
| **3** | **Cause Details** (`/Causes/Details/{id}`) | `CausesController.Details` & `Causes/Details.cshtml` | `Cause`, `Donation`, `NGO` | `CauseDetailsView.xaml` | `CauseDetailsViewModel` | Full cause narrative, progress bar, certified NGO badge, recent donors, instant donate | ✅ Complete Native |
| **4** | **Donate Now** (`/Donate`, `/Donate/Index`) | `DonateController.Index` & `Donate/Index.cshtml` | `Donation`, `PaymentTransaction`, `Cause` | `PublicDonateView.xaml` | `PublicDonateViewModel` | Preset chips ($25-$500), custom amount, anonymous flag, offline cash/simulated card, instant QuestPDF receipt | ✅ Complete Native |
| **5** | **Donation Receipt** (`/Donate/ReceiptPrint/{id}`) | `DonateController.ReceiptPrint` & `Donate/ReceiptPrint.cshtml` | `Donation`, `PaymentTransaction` | `DonationReceiptDialog.xaml` | `DonationReceiptViewModel` | QuestPDF receipt generator, cryptographic SHA-256 validation hash, print & PDF export | ✅ Complete Native |
| **6** | **Zakat Calculator** (`/Zakat`, `/Zakat/Index`) | `ZakatController.Index` & `Zakat/Index.cshtml` | In-memory formulas, Live Nisab standards | `ZakatCalculatorView.xaml` | `ZakatCalculatorViewModel` | Cash, gold, silver, investments, liabilities, Nisab threshold, 2.5% calculation, 1-click Pay Zakat | ✅ Complete Native |
| **7** | **Programmes** (`/Programmes`, `/Programmes/Index`) | `ProgrammesController.Index` & `Programmes/Index.cshtml` | `Programme`, `ProgrammeInterest` | `ProgrammesView.xaml` | `ProgrammesViewModel` | Cards by category (Relief, Education, Health), target beneficiaries, details routing | ✅ Complete Native |
| **8** | **Programme Details** (`/Programmes/Details/{id}`) | `ProgrammesController.Details` & `Programmes/Details.cshtml` | `Programme`, `ProgrammeInterest` | `ProgrammeDetailsView.xaml` | `ProgrammeDetailsViewModel` | In-depth programme scope, volunteer & sponsor registration form saving to `ProgrammeInterests` | ✅ Complete Native |
| **9** | **Events & Relief Drives** (`/Events`, `/Events/Index`) | `EventsController.Index` & `Events/Index.cshtml` | `Event` (Title, Date, Location) | `EventsView.xaml` | `EventsViewModel` | Upcoming galas, medical camps, volunteer drives, date/location tags, RSVP action | ✅ Complete Native |
| **10** | **Media Gallery** (`/Gallery`, `/Gallery/Index`) | `GalleryController.Index` & `Gallery/Index.cshtml` | `GalleryItem` (Category, Image, Date) | `GalleryView.xaml` | `GalleryViewModel` | Category filtering (Education, Healthcare, Relief, Community), high-res presentation | ✅ Complete Native |
| **11** | **Accredited NGOs** (`/Ngos`, `/Ngos/Index`, `/Ngos/Details`) | `NgosController.Index/Details` & `Ngos/Index.cshtml` | `NGO` (RegNumber, FocusArea, Contact) | `NgosDirectoryView.xaml` | `NgosDirectoryViewModel` | Registered NGO cards, official verification badge, contact info, active causes supported | ✅ Complete Native |
| **12** | **About Us** (`/About/PageDetail`, `/About/OurTeam`, `/About/OurSupporters`, `/About/CareerWithUs`) | `AboutController` & 4 Razor Views | `AboutPage`, `TeamMember`, `Partner`, `JobOpportunity` | `AboutView.xaml` | `AboutViewModel` | Tabbed presentation: Mission/Story, Leadership Team, Corporate Allies, Career Openings | ✅ Complete Native |
| **13** | **Contact Us & Raise Query** (`/Home/Contact`, `/Home/RaiseQuery`) | `HomeController.Contact/RaiseQuery` & Views | `ContactMessage`, `UserQuery` | `ContactView.xaml` | `ContactViewModel` | General contact form (`ContactMessages`) & authenticated Helpdesk Ticket submission (`UserQueries`) | ✅ Complete Native |
| **14** | **Help Centre & FAQs** (`/Home/HelpCentre`) | `HomeController.HelpCentre` & `Home/HelpCentre.cshtml` | `FAQ` (Question, Answer, Category) | `HelpCentreFaqView.xaml` | `HelpCentreFaqViewModel` | Searchable accordion FAQs categorized by Donations, Tax, Programmes, plus direct ticket link | ✅ Complete Native |
| **15** | **Authentication & Register** (`/Account/Login`, `/Account/Register`) | `AccountController` & Razor Views | `User` (Email, PasswordHash, Salt, Role) | `LoginWindow.xaml` | `LoginViewModel` | PBKDF2 SHA-512 authentication, user registration, instant role-switching, session persistence | ✅ Complete Native |
| **16** | **Citizen User Dashboard** (`/User/Dashboard`, `/User/Profile`, `/User/Donations`, `/User/Programmes`, `/User/Queries`, `/User/InviteFriends`) | `UserController` & 6 Razor Views | `User`, `Donation`, `ProgrammeInterest`, `UserQuery`, `FriendInvitation` | `UserPortalView.xaml` | `UserPortalViewModel` | Donor statistics, profile editor, personal donation receipt downloads, registered programmes, ticket status | ✅ Complete Native |
| **17** | **Admin Dashboard** (`/Admin/Index`) | `AdminController.Index` & `Admin/Index.cshtml` | Aggregate metrics across all tables | `DashboardView.xaml` | `DashboardViewModel` | Key Performance Indicators, financial charts, urgent actions, rapid navigation cards | ✅ Complete Native |
| **18** | **Admin User Management** (`/Admin/Users`) | `AdminController.Users` | `User` | `UsersManagementView.xaml` (inside Admin) | `UsersViewModel` | View all users, search by name/email, toggle active/locked status, role elevation (User <-> Admin) | ✅ Complete Native |
| **19** | **Admin Causes CRUD** (`/Admin/Causes`, `/Admin/CreateCause`, `/Admin/EditCause`) | `AdminController.Causes/CreateCause/EditCause` | `Cause` | `CausesView.xaml` | `CausesViewModel` | Full CRUD, target amount setting, category selection, featured cause toggle, audit logging | ✅ Complete Native |
| **20** | **Admin Donations Ledger** (`/Admin/Donations`) | `AdminController.Donations` | `Donation`, `PaymentTransaction` | `DonationsView.xaml` | `DonationsViewModel` | Search, filter by status/payment method, view full transaction details, generate CSV/Excel exports | ✅ Complete Native |
| **21** | **Admin Programmes CRUD** (`/Admin/Programmes`, `/Admin/CreateProgramme`, `/Admin/EditProgramme`) | `AdminController.Programmes` | `Programme`, `ProgrammeInterest` | `ProgrammesManagementView.xaml` (inside Admin) | `ProgrammesViewModel` | Create/edit welfare programmes, track participant counts, manage volunteer submissions | ✅ Complete Native |
| **22** | **Admin Helpdesk & Queries** (`/Admin/Queries`, `/Admin/ReplyQuery`, `/Admin/ContactMessages`) | `AdminController.Queries/ReplyQuery/ContactMessages` | `Query`, `ContactMessage` | `HelpdeskAdminView.xaml` (inside Admin) | `HelpdeskAdminViewModel` | Review incoming public messages, respond to citizen queries with admin replies, status tracking | ✅ Complete Native |
| **23** | **Reports & Analytics** (`/Admin/Reports`) | `AdminController.Reports` | `Donations`, `Causes`, `Users` | `ReportsView.xaml` | `ReportsViewModel` | ClosedXML Excel workbooks, QuestPDF financial summaries, date-range filtering, donor breakdowns | ✅ Complete Native |
| **24** | **Database Backup & Disaster Recovery** | Desktop Enterprise Feature | Local SQLite database `.db`, WAL logs | `BackupView.xaml` | `BackupViewModel` | Point-in-time SQLite database hot-backup, automated backup scheduling, one-click manual restore | ✅ Complete Native |
| **25** | **Audit Trail & Compliance** | Desktop Enterprise Feature | `AuditLog` | `AuditLogsView.xaml` | `AuditLogsViewModel` | Immutable event log: logins, record changes, exports, backups, sync events with timestamp & user info | ✅ Complete Native |
| **26** | **Cloud Synchronization Engine** | Desktop Enterprise Feature | `SyncQueueItem`, SQLite WAL, SQL Server | `SettingsView.xaml` | `SettingsViewModel` | Automatic background sync, manual 'Sync Now', offline change queuing, two-way conflict handling | ✅ Complete Native |

---

## 3. Visual & Styling Fidelity

### Color Palette Exact Matching
- **Primary Accent Orange:** `#F25C05` (Hover: `#D94E00`, Glow: `rgba(242, 92, 5, 0.25)`)
- **Background Page:** `#071527` (Deep high-contrast navy)
- **Surface & Cards:** `#1E293B` (Modern slate border: `#334155`)
- **Top Navigation Bar:** `#0B192C` (Branded header with quick portal links)
- **Text:** Primary `#F8FAFC`, Secondary `#94A3B8`, Accent `#F25C05`
- **Border Radius:** 10px-14px for cards, 8px for buttons and inputs.
- **Typography:** Modern clean sans-serif (Segoe UI / Plus Jakarta Sans equivalent metrics).
