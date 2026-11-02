using NGEOS.Helpers;
using NGEOS.Models;

namespace NGEOS.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            // 1. Seed Admin User if not exists (No regular user is seeded - users register directly)
            if (!context.Users.Any(u => u.Role == "Admin"))
            {
                var admin = new User
                {
                    FullName = "NGEOS Administrator",
                    Email = "admin@ngeos.org",
                    PasswordHash = PasswordHelper.HashPassword("Admin@123"),
                    Phone = "+1 (800) 555-0199",
                    Address = "100 Charity Boulevard, Suite 500, New York, NY 10001",
                    Profession = "NGO Managing Director",
                    Organization = "NGEOS Central Secretariat",
                    Role = "Admin",
                    ProfileImageUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=300&auto=format&fit=crop&q=80",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                context.Users.Add(admin);
                context.SaveChanges();
            }

            // 2. Seed Associated NGOs if none exist
            if (!context.NGOs.Any())
            {
                var ngos = new List<NGO>
                {
                    new NGO
                    {
                        Name = "Hope For Children Foundation",
                        RegistrationNumber = "NGO-88210-CH",
                        ShortDescription = "Dedicated to child welfare, education, nutritional support and protecting vulnerable orphans worldwide.",
                        FullDescription = "Hope For Children Foundation is an international non-profit working across 14 nations to provide safe shelters, nutrition, quality education, and medical healthcare for vulnerable and orphaned children.",
                        LogoUrl = "https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?w=400&auto=format&fit=crop&q=80",
                        BannerUrl = "https://images.unsplash.com/photo-1542810634-71277d95dcbb?w=1200&auto=format&fit=crop&q=80",
                        FocusArea = "Children Welfare & Protection",
                        Website = "https://hopeforchildren.example.org",
                        Email = "contact@hopeforchildren.org",
                        Phone = "+1 (555) 349-8821",
                        Address = "45 Hope Street, Boston, MA",
                        EstablishedYear = 2012,
                        IsVerified = true,
                        IsActive = true
                    },
                    new NGO
                    {
                        Name = "Bright Future Education Trust",
                        RegistrationNumber = "NGO-44109-ED",
                        ShortDescription = "Empowering underprivileged children and youth through quality schooling, digital labs and scholarships.",
                        FullDescription = "Bright Future Education Trust builds schools, distributes learning kits, and trains teachers in rural and disadvantaged communities to break the cycle of poverty through education.",
                        LogoUrl = "https://images.unsplash.com/photo-1509062522246-3755977927d7?w=400&auto=format&fit=crop&q=80",
                        BannerUrl = "https://images.unsplash.com/photo-1497633762265-9d179a990aa6?w=1200&auto=format&fit=crop&q=80",
                        FocusArea = "Education & Literacy",
                        Website = "https://brightfuture.example.org",
                        Email = "info@brightfuturetrust.org",
                        Phone = "+1 (555) 782-4410",
                        Address = "128 Academy Lane, Chicago, IL",
                        EstablishedYear = 2015,
                        IsVerified = true,
                        IsActive = true
                    },
                    new NGO
                    {
                        Name = "Global Health Aid Initiative",
                        RegistrationNumber = "NGO-77123-HL",
                        ShortDescription = "Providing emergency medical relief, mobile clinic caravans, and vital surgeries to remote communities.",
                        FullDescription = "Global Health Aid Initiative operates mobile surgical units, organizes free medical camps, and delivers essential medicines to poverty-stricken areas lacking basic healthcare infrastructure.",
                        LogoUrl = "https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?w=400&auto=format&fit=crop&q=80",
                        BannerUrl = "https://images.unsplash.com/photo-1584515979956-d9f6e5d09982?w=1200&auto=format&fit=crop&q=80",
                        FocusArea = "Healthcare & Medical Relief",
                        Website = "https://globalhealthaid.example.org",
                        Email = "help@globalhealthaid.org",
                        Phone = "+1 (555) 901-7712",
                        Address = "720 Medical Plaza, Atlanta, GA",
                        EstablishedYear = 2014,
                        IsVerified = true,
                        IsActive = true
                    },
                    new NGO
                    {
                        Name = "Ability & Beyond Foundation",
                        RegistrationNumber = "NGO-33190-DS",
                        ShortDescription = "Championing accessibility, prosthetic care, and vocational training for specially challenged and disabled individuals.",
                        FullDescription = "Ability & Beyond Foundation equips individuals with physical, auditory, and visual challenges with assistive tech, mobility wheelchairs, prosthetics, and sustainable job opportunities.",
                        LogoUrl = "https://images.unsplash.com/photo-1516726817505-f5ed825624d8?w=400&auto=format&fit=crop&q=80",
                        BannerUrl = "https://images.unsplash.com/photo-1526628953301-3e589a6a8b74?w=1200&auto=format&fit=crop&q=80",
                        FocusArea = "Disabled Persons Welfare",
                        Website = "https://abilityandbeyond.example.org",
                        Email = "care@abilitybeyond.org",
                        Phone = "+1 (555) 412-3319",
                        Address = "88 Inclusion Way, Seattle, WA",
                        EstablishedYear = 2017,
                        IsVerified = true,
                        IsActive = true
                    },
                    new NGO
                    {
                        Name = "Empower Women Global",
                        RegistrationNumber = "NGO-99314-WM",
                        ShortDescription = "Fostering female leadership, vocational skills, legal aid, and micro-grants for marginalized women.",
                        FullDescription = "Empower Women Global runs community centers teaching tailoring, digital design, financial literacy, and provides micro-seed capital to help women become financially independent.",
                        LogoUrl = "https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?w=400&auto=format&fit=crop&q=80",
                        BannerUrl = "https://images.unsplash.com/photo-1529156069898-49953e39b3ac?w=1200&auto=format&fit=crop&q=80",
                        FocusArea = "Women Welfare & Empowerment",
                        Website = "https://empowerwomen.example.org",
                        Email = "info@empowerwomen.org",
                        Phone = "+1 (555) 633-9931",
                        Address = "500 Horizon Blvd, Austin, TX",
                        EstablishedYear = 2016,
                        IsVerified = true,
                        IsActive = true
                    },
                    new NGO
                    {
                        Name = "Silver Years Care Society",
                        RegistrationNumber = "NGO-22108-EL",
                        ShortDescription = "Providing comfortable shelter, daily companionship, and specialized geriatric care for abandoned seniors.",
                        FullDescription = "Silver Years Care Society operates dignified assisted-living homes, medical outreach, and community wellness programs ensuring our elders live their golden years with peace and respect.",
                        LogoUrl = "https://images.unsplash.com/photo-1516307365426-bea591f05011?w=400&auto=format&fit=crop&q=80",
                        BannerUrl = "https://images.unsplash.com/photo-1581579438747-1dc8d17bbce4?w=1200&auto=format&fit=crop&q=80",
                        FocusArea = "Elderly Care & Senior Welfare",
                        Website = "https://silveryearscare.example.org",
                        Email = "support@silveryears.org",
                        Phone = "+1 (555) 219-2210",
                        Address = "310 Magnolia Drive, Miami, FL",
                        EstablishedYear = 2018,
                        IsVerified = true,
                        IsActive = true
                    }
                };
                context.NGOs.AddRange(ngos);
                context.SaveChanges();
            }

            // 3. Seed Causes if none exist
            if (!context.Causes.Any())
            {
                var ngo1 = context.NGOs.FirstOrDefault(n => n.Name.Contains("Hope For Children"));
                var ngo2 = context.NGOs.FirstOrDefault(n => n.Name.Contains("Bright Future"));
                var ngo3 = context.NGOs.FirstOrDefault(n => n.Name.Contains("Global Health"));
                var ngo4 = context.NGOs.FirstOrDefault(n => n.Name.Contains("Ability & Beyond"));
                var ngo5 = context.NGOs.FirstOrDefault(n => n.Name.Contains("Empower Women"));
                var ngo6 = context.NGOs.FirstOrDefault(n => n.Name.Contains("Silver Years"));

                var causes = new List<Cause>
                {
                    new Cause
                    {
                        Title = "Nutritious Food & Health Support for 500 Orphan Children",
                        Category = "Children",
                        ShortDescription = "Provide daily balanced meals, clean drinking water, and essential vitamins to 500 orphaned children in shelter homes.",
                        FullDescription = "Malnutrition impairs physical growth and cognitive development. This campaign ensures 500 orphaned children receive three hot, nutritious meals daily, regular pediatric screenings, clean water filtration, and vital health supplements for an entire year.",
                        TargetAmount = 45000,
                        RaisedAmount = 31250,
                        ImageUrl = "https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?w=800&auto=format&fit=crop&q=80",
                        IsFeatured = true,
                        IsActive = true,
                        NgoId = ngo1?.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-30)
                    },
                    new Cause
                    {
                        Title = "Build Smart Digital Classrooms for Rural Students",
                        Category = "Education",
                        ShortDescription = "Equip 12 rural community schools with solar power, interactive smartboards, laptops, and certified digital learning curriculum.",
                        FullDescription = "Bridge the rural-urban digital divide. Your donations provide modern computing hardware, offline digital encyclopedias, solar-powered generators, and certified coding/literacy instructors for over 1,800 rural students.",
                        TargetAmount = 60000,
                        RaisedAmount = 43800,
                        ImageUrl = "https://images.unsplash.com/photo-1509062522246-3755977927d7?w=800&auto=format&fit=crop&q=80",
                        IsFeatured = true,
                        IsActive = true,
                        NgoId = ngo2?.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-25)
                    },
                    new Cause
                    {
                        Title = "Prosthetic Limbs & Rehabilitation for Disabled Youth",
                        Category = "Disabled Persons",
                        ShortDescription = "Deliver custom prosthetic limbs, motorized wheelchairs, and physical therapy to 120 disabled young individuals.",
                        FullDescription = "Mobility is independence. By funding lightweight, modern prosthetic limbs and comprehensive physical rehabilitation, you give specially challenged youth the dignity to walk, attend school, and pursue their dreams.",
                        TargetAmount = 38000,
                        RaisedAmount = 24900,
                        ImageUrl = "https://images.unsplash.com/photo-1516726817505-f5ed825624d8?w=800&auto=format&fit=crop&q=80",
                        IsFeatured = true,
                        IsActive = true,
                        NgoId = ngo4?.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-20)
                    },
                    new Cause
                    {
                        Title = "Mobile Health Clinics for Remote Tribal Villages",
                        Category = "Healthcare",
                        ShortDescription = "Deploy 2 fully-equipped mobile medical vans with diagnostic labs and medications to reach 25 remote villages weekly.",
                        FullDescription = "Hundreds of remote families live hours away from the nearest clinic. Our mobile vans bring doctors, nurses, ultrasound diagnostics, maternal checkups, and life-saving treatments right to their doorsteps.",
                        TargetAmount = 52000,
                        RaisedAmount = 37100,
                        ImageUrl = "https://images.unsplash.com/photo-1584515979956-d9f6e5d09982?w=800&auto=format&fit=crop&q=80",
                        IsFeatured = false,
                        IsActive = true,
                        NgoId = ngo3?.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-18)
                    },
                    new Cause
                    {
                        Title = "Vocational Tailoring & Micro-Enterprise Grants for Women",
                        Category = "Women",
                        ShortDescription = "Empower 250 low-income single mothers with sewing machines, business training, and seed capital to launch businesses.",
                        FullDescription = "Financial independence breaks domestic vulnerability. This program provides professional sewing equipment, digital e-commerce selling training, financial literacy workshops, and a $200 startup micro-grant to 250 determined women.",
                        TargetAmount = 28000,
                        RaisedAmount = 19600,
                        ImageUrl = "https://images.unsplash.com/photo-1529156069898-49953e39b3ac?w=800&auto=format&fit=crop&q=80",
                        IsFeatured = false,
                        IsActive = true,
                        NgoId = ngo5?.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-15)
                    },
                    new Cause
                    {
                        Title = "Warm Care, Medical Care & Meals for Abandoned Elderly",
                        Category = "Elderly",
                        ShortDescription = "Sponsor warm shelter, daily geriatric healthcare, physical therapy, and nutritious meals for 85 abandoned senior citizens.",
                        FullDescription = "No senior citizen should face poverty, sickness, and loneliness alone. Your support provides clean bedding, warm clothing, specialized medications, nutritious soft meals, and compassionate social activities.",
                        TargetAmount = 32000,
                        RaisedAmount = 22400,
                        ImageUrl = "https://images.unsplash.com/photo-1581579438747-1dc8d17bbce4?w=800&auto=format&fit=crop&q=80",
                        IsFeatured = false,
                        IsActive = true,
                        NgoId = ngo6?.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-12)
                    },
                    new Cause
                    {
                        Title = "Youth Tech Skills & Career Apprenticeship Bootcamp",
                        Category = "Youth",
                        ShortDescription = "Train 300 disadvantaged youth in coding, graphic design, and customer support with guaranteed job placement assistance.",
                        FullDescription = "Empowering high-school dropouts and underprivileged youth with modern marketable tech skills, career mentorship, interview coaching, and paid apprenticeships in leading partner organizations.",
                        TargetAmount = 40000,
                        RaisedAmount = 26500,
                        ImageUrl = "https://images.unsplash.com/photo-1531482615713-2afd69097998?w=800&auto=format&fit=crop&q=80",
                        IsFeatured = false,
                        IsActive = true,
                        NgoId = ngo2?.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-10)
                    }
                };
                context.Causes.AddRange(causes);
                context.SaveChanges();
            }

            // 4. Seed Partners if none exist
            if (!context.Partners.Any())
            {
                var partners = new List<Partner>
                {
                    new Partner
                    {
                        Name = "Apex Global Technologies",
                        Category = "Technology Partner",
                        LogoUrl = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=300&auto=format&fit=crop&q=80",
                        WebsiteUrl = "https://apexglobal.example.com",
                        Description = "Supporting digital infrastructure, cloud services, and computer lab equipment for our education causes.",
                        DisplayOrder = 1,
                        IsActive = true
                    },
                    new Partner
                    {
                        Name = "Horizon Healthcare Group",
                        Category = "Healthcare Partner",
                        LogoUrl = "https://images.unsplash.com/photo-1505751172876-fa1923c5c528?w=300&auto=format&fit=crop&q=80",
                        WebsiteUrl = "https://horizonhealth.example.com",
                        Description = "Donating medicines, surgical equipment, and medical volunteers for rural mobile clinic operations.",
                        DisplayOrder = 2,
                        IsActive = true
                    },
                    new Partner
                    {
                        Name = "Starlight Financial Corp",
                        Category = "Corporate Sponsor",
                        LogoUrl = "https://images.unsplash.com/photo-1559526324-4b87b5e36e44?w=300&auto=format&fit=crop&q=80",
                        WebsiteUrl = "https://starlightfin.example.com",
                        Description = "Matching employee donations and funding micro-grant seed funds for women entrepreneurs.",
                        DisplayOrder = 3,
                        IsActive = true
                    },
                    new Partner
                    {
                        Name = "Vanguard Logistics Alliance",
                        Category = "Community Ally",
                        LogoUrl = "https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?w=300&auto=format&fit=crop&q=80",
                        WebsiteUrl = "https://vanguardlog.example.com",
                        Description = "Providing free nationwide freight transport for emergency relief supplies and medical shipments.",
                        DisplayOrder = 4,
                        IsActive = true
                    },
                    new Partner
                    {
                        Name = "Nexus Media Group",
                        Category = "Media & Outreach",
                        LogoUrl = "https://images.unsplash.com/photo-1557804506-669a67965ba0?w=300&auto=format&fit=crop&q=80",
                        WebsiteUrl = "https://nexusmedia.example.com",
                        Description = "Broadcasting public awareness campaigns and promoting NGEOS events pro-bono across national channels.",
                        DisplayOrder = 5,
                        IsActive = true
                    },
                    new Partner
                    {
                        Name = "EcoGreen Energy Systems",
                        Category = "Sustainability Sponsor",
                        LogoUrl = "https://images.unsplash.com/photo-1497435334941-8c899ee9e8e9?w=300&auto=format&fit=crop&q=80",
                        WebsiteUrl = "https://ecogreen.example.com",
                        Description = "Installing clean solar power systems in rural schools and senior citizen shelter facilities.",
                        DisplayOrder = 6,
                        IsActive = true
                    }
                };
                context.Partners.AddRange(partners);
                context.SaveChanges();
            }

            // 5. Seed Programmes if none exist
            if (!context.Programmes.Any())
            {
                var ngo1 = context.NGOs.FirstOrDefault(n => n.Name.Contains("Hope For Children"));
                var ngo2 = context.NGOs.FirstOrDefault(n => n.Name.Contains("Bright Future"));
                var ngo3 = context.NGOs.FirstOrDefault(n => n.Name.Contains("Global Health"));
                var ngo4 = context.NGOs.FirstOrDefault(n => n.Name.Contains("Ability & Beyond"));
                var ngo5 = context.NGOs.FirstOrDefault(n => n.Name.Contains("Empower Women"));

                var programmes = new List<Programme>
                {
                    new Programme
                    {
                        Title = "Education For Every Child: Smart Schooling Drive",
                        Category = "Education",
                        NgoId = ngo2?.Id,
                        ShortDescription = "Comprehensive learning initiative distributing 1,000 back-to-school kits, digital tablets, and school uniforms to rural kids.",
                        FullDescription = "Education is the foundation for lasting change. This 3-month programme equips underprivileged children with all necessary school supplies, access to digital e-learning libraries, and after-school remedial tutoring.",
                        ImageUrl = "https://images.unsplash.com/photo-1497633762265-9d179a990aa6?w=800&auto=format&fit=crop&q=80",
                        StartDate = DateTime.UtcNow.AddDays(14),
                        EndDate = DateTime.UtcNow.AddMonths(3),
                        Location = "Metropolitan Convention Hall & 12 Rural Centers",
                        TargetBeneficiaries = 1000,
                        IsActive = true
                    },
                    new Programme
                    {
                        Title = "Free Pediatric & Eye Care Medical Caravan",
                        Category = "Health Care",
                        NgoId = ngo3?.Id,
                        ShortDescription = "Free medical checkups, vision tests, corrective eyeglasses, and immunization shots for over 1,500 children.",
                        FullDescription = "Organized by volunteer doctors and pediatricians, this healthcare drive provides full diagnostic checkups, deworming medicines, corrective glasses, and urgent referrals for specialized pediatric surgeries.",
                        ImageUrl = "https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?w=800&auto=format&fit=crop&q=80",
                        StartDate = DateTime.UtcNow.AddDays(21),
                        EndDate = DateTime.UtcNow.AddDays(28),
                        Location = "North District Community Center",
                        TargetBeneficiaries = 1500,
                        IsActive = true
                    },
                    new Programme
                    {
                        Title = "Specially Challenged Children Art & Sports Festival",
                        Category = "Privileged children (Specially challenged)",
                        NgoId = ngo4?.Id,
                        ShortDescription = "A celebratory, inclusive festival featuring adaptive sports, sensory art workshops, and musical performances.",
                        FullDescription = "Celebrating the courage, creativity, and unique talents of children with special needs. The festival includes wheelchair races, adaptive painting competitions, sensory music therapy, and family counseling.",
                        ImageUrl = "https://images.unsplash.com/photo-1516726817505-f5ed825624d8?w=800&auto=format&fit=crop&q=80",
                        StartDate = DateTime.UtcNow.AddDays(30),
                        EndDate = DateTime.UtcNow.AddDays(32),
                        Location = "Olympic City Sports Complex",
                        TargetBeneficiaries = 600,
                        IsActive = true
                    },
                    new Programme
                    {
                        Title = "Women Tech & Digital Leadership Accelerator",
                        Category = "Women Empowerment",
                        NgoId = ngo5?.Id,
                        ShortDescription = "8-week intensive workshop teaching digital marketing, freelancing, and small business financial management to 200 women.",
                        FullDescription = "Equipping ambitious women from low-income communities with high-demand digital skills, business branding techniques, and micro-loan matching to foster independent sustainable livelihoods.",
                        ImageUrl = "https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?w=800&auto=format&fit=crop&q=80",
                        StartDate = DateTime.UtcNow.AddDays(40),
                        EndDate = DateTime.UtcNow.AddMonths(2),
                        Location = "NGEOS Innovation Hub, Downtown",
                        TargetBeneficiaries = 200,
                        IsActive = true
                    }
                };
                context.Programmes.AddRange(programmes);
                context.SaveChanges();
            }

            // 6. Seed Events if none exist
            if (!context.Events.Any())
            {
                var events = new List<Event>
                {
                    new Event
                    {
                        Title = "Annual NGEOS Global Charity Gala & Awards 2026",
                        ShortDescription = "Join international philanthropists, civic leaders, and NGO heroes for an inspiring evening of celebration and fundraising.",
                        FullDescription = "Our flagship annual gala brings together over 500 compassionate donors, corporate partners, and grassroots workers to celebrate humanitarian milestones and raise crucial funding for the coming year.",
                        EventDate = DateTime.UtcNow.AddDays(45),
                        Location = "Grand Ballroom, Marriott Marquis Hotel, NY",
                        ImageUrl = "https://images.unsplash.com/photo-1511578314322-379afb476865?w=800&auto=format&fit=crop&q=80",
                        Organizer = "NGEOS Central Secretariat",
                        IsUpcoming = true
                    },
                    new Event
                    {
                        Title = "5K Charity Walk & Marathon for Children's Education",
                        ShortDescription = "Lace up your sneakers and run or walk to raise funds for school supplies and rural classrooms.",
                        FullDescription = "A family-friendly 5K run and community festival with live music, food stalls, and medals for all finishers. All registration fees go directly to our Education causes.",
                        EventDate = DateTime.UtcNow.AddDays(25),
                        Location = "Central City Waterfront Park",
                        ImageUrl = "https://images.unsplash.com/photo-1530549387789-4c1017266635?w=800&auto=format&fit=crop&q=80",
                        Organizer = "Bright Future Education Trust & NGEOS",
                        IsUpcoming = true
                    },
                    new Event
                    {
                        Title = "Healthcare for All: Community Health Expo & Blood Donation",
                        ShortDescription = "Free wellness screenings, blood donation camp, CPR workshops, and pediatric consultations open to all.",
                        FullDescription = "Partnering with leading medical institutions to offer full health diagnostics, blood typing, voluntary blood donation drive, and child wellness nutrition consultations.",
                        EventDate = DateTime.UtcNow.AddDays(15),
                        Location = "Civic Plaza Pavilion",
                        ImageUrl = "https://images.unsplash.com/photo-1584515979956-d9f6e5d09982?w=800&auto=format&fit=crop&q=80",
                        Organizer = "Global Health Aid Initiative",
                        IsUpcoming = true
                    }
                };
                context.Events.AddRange(events);
                context.SaveChanges();
            }

            // 7. Seed Gallery Items if none exist
            if (!context.GalleryItems.Any())
            {
                var gallery = new List<GalleryItem>
                {
                    new GalleryItem
                    {
                        Title = "Distributing School Kits & Books in Rural Primary Schools",
                        Category = "Education",
                        ImageUrl = "https://images.unsplash.com/photo-1497633762265-9d179a990aa6?w=800&auto=format&fit=crop&q=80",
                        Description = "Volunteers handing out backpacks, stationery, and books to eager elementary students.",
                        EventDate = DateTime.UtcNow.AddDays(-20)
                    },
                    new GalleryItem
                    {
                        Title = "Free Eye Diagnostics & Corrective Spectacles Camp",
                        Category = "Healthcare",
                        ImageUrl = "https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?w=800&auto=format&fit=crop&q=80",
                        Description = "Medical team conducting detailed eye examinations for elderly villagers.",
                        EventDate = DateTime.UtcNow.AddDays(-35)
                    },
                    new GalleryItem
                    {
                        Title = "Happy Smiles at the Annual Orphanage Food & Fun Carnival",
                        Category = "Children",
                        ImageUrl = "https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?w=800&auto=format&fit=crop&q=80",
                        Description = "Nutritious feasts, games, and gifts distributed to over 300 children.",
                        EventDate = DateTime.UtcNow.AddDays(-50)
                    },
                    new GalleryItem
                    {
                        Title = "Wheelchair & Assistive Equipment Distribution Ceremony",
                        Category = "Relief",
                        ImageUrl = "https://images.unsplash.com/photo-1516726817505-f5ed825624d8?w=800&auto=format&fit=crop&q=80",
                        Description = "Handing over motorized wheelchairs and mobility aids to specially challenged participants.",
                        EventDate = DateTime.UtcNow.AddDays(-40)
                    },
                    new GalleryItem
                    {
                        Title = "Women Tailoring & Vocational Academy Graduation",
                        Category = "Events",
                        ImageUrl = "https://images.unsplash.com/photo-1529156069898-49953e39b3ac?w=800&auto=format&fit=crop&q=80",
                        Description = "Celebrating 80 female graduates receiving certified diplomas and brand-new sewing machines.",
                        EventDate = DateTime.UtcNow.AddDays(-15)
                    },
                    new GalleryItem
                    {
                        Title = "Emergency Winter Blanket & Warm Clothing Distribution",
                        Category = "Relief",
                        ImageUrl = "https://images.unsplash.com/photo-1469571486292-0ba58a3f068b?w=800&auto=format&fit=crop&q=80",
                        Description = "Delivering thermal blankets and warm clothes to elderly shelters during freezing winter.",
                        EventDate = DateTime.UtcNow.AddDays(-60)
                    }
                };
                context.GalleryItems.AddRange(gallery);
                context.SaveChanges();
            }

            // 8. Seed About Us Sub-pages (all 7 sub-pages)
            if (!context.AboutPages.Any())
            {
                var aboutPages = new List<AboutPage>
                {
                    new AboutPage
                    {
                        Slug = "what-we-do",
                        Title = "What We Do",
                        Subtitle = "Transforming Vulnerable Communities Through Sustainable Humanitarian Aid",
                        BannerImageUrl = "https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?w=1200&auto=format&fit=crop&q=80",
                        ContentHtml = @"
                            <h3>Empowering Lives Across Every Sector of Need</h3>
                            <p class='lead'>NGEOS serves as a unified umbrella platform connecting verified non-governmental organizations with compassionate global citizens and corporate donors.</p>
                            <div class='row g-4 my-4'>
                                <div class='col-md-6'>
                                    <div class='p-3 bg-light rounded-3 border-start border-4 border-warning'>
                                        <h5><i class='bi bi-heart-pulse-fill text-warning me-2'></i>Child & Orphan Welfare</h5>
                                        <p class='small text-muted mb-0'>Ensuring orphaned and vulnerable children have safe housing, nourishing meals, trauma-informed counseling, and uninterrupted access to primary education.</p>
                                    </div>
                                </div>
                                <div class='col-md-6'>
                                    <div class='p-3 bg-light rounded-3 border-start border-4 border-primary'>
                                        <h5><i class='bi bi-book-fill text-primary me-2'></i>Universal Education</h5>
                                        <p class='small text-muted mb-0'>Supplying learning kits, modern solar-powered computer labs, and scholarships to eliminate poverty through high-quality literacy.</p>
                                    </div>
                                </div>
                                <div class='col-md-6'>
                                    <div class='p-3 bg-light rounded-3 border-start border-4 border-danger'>
                                        <h5><i class='bi bi-hospital-fill text-danger me-2'></i>Emergency & Community Health</h5>
                                        <p class='small text-muted mb-0'>Operating mobile diagnostic vans, pediatric surgeries, maternal health programs, and delivering essential medicines directly to marginalized villages.</p>
                                    </div>
                                </div>
                                <div class='col-md-6'>
                                    <div class='p-3 bg-light rounded-3 border-start border-4 border-success'>
                                        <h5><i class='bi bi-person-wheelchair text-success me-2'></i>Disabled Persons Support</h5>
                                        <p class='small text-muted mb-0'>Providing custom prosthetics, motorized wheelchairs, adaptive sports festivals, and accessibility equipment for specially challenged youth.</p>
                                    </div>
                                </div>
                            </div>
                            <p>Through stringent due diligence, 100% transparency in fund allocation, and real-time transaction tracking, NGEOS ensures every single dollar reaches the individuals who need it most.</p>
                        ",
                        LastUpdated = DateTime.UtcNow
                    },
                    new AboutPage
                    {
                        Slug = "our-mission",
                        Title = "Our Mission & Vision",
                        Subtitle = "Building a Compassionate World Free From Poverty, Illness, and Disparity",
                        BannerImageUrl = "https://images.unsplash.com/photo-1542810634-71277d95dcbb?w=1200&auto=format&fit=crop&q=80",
                        ContentHtml = @"
                            <h3>Our Core Mission</h3>
                            <p class='lead'>To mobilize global resources, champion high-impact grassroots NGOs, and create transparent, direct channels for donors to uplift vulnerable children, women, disabled persons, and elderly citizens worldwide.</p>
                            <h4 class='mt-4'>Our Vision</h4>
                            <p>A just, equitable society where every human being—regardless of socio-economic background, physical ability, or geographic location—enjoys access to quality education, world-class healthcare, dignity, and economic opportunity.</p>
                            <h4 class='mt-4'>Our Core Values</h4>
                            <ul class='list-unstyled'>
                                <li class='mb-2'><strong>1. Radical Transparency:</strong> Every dollar donated is accounted for with detailed impact reports and public transaction validation.</li>
                                <li class='mb-2'><strong>2. Unwavering Empathy:</strong> We serve with unconditional compassion, placing human dignity at the center of all initiatives.</li>
                                <li class='mb-2'><strong>3. Sustainable Impact:</strong> Rather than temporary handouts, we prioritize long-term skills, infrastructure, and self-reliance.</li>
                                <li class='mb-2'><strong>4. Inclusive Collaboration:</strong> We bridge civil society, NGOs, philanthropic corporations, and volunteers to multiply our positive impact.</li>
                            </ul>
                        ",
                        LastUpdated = DateTime.UtcNow
                    },
                    new AboutPage
                    {
                        Slug = "our-team",
                        Title = "Our Team",
                        Subtitle = "Passionate Humanitarians, Experienced Leaders, and Dedicated Field Directors",
                        BannerImageUrl = "https://images.unsplash.com/photo-1522071820081-009f0129c71c?w=1200&auto=format&fit=crop&q=80",
                        ContentHtml = @"
                            <h3>The People Behind NGEOS</h3>
                            <p class='lead'>Our interdisciplinary team brings decades of combined experience across international relief, healthcare logistics, education management, and humanitarian governance.</p>
                            <p>We work tirelessly across our central secretariat and field partner hubs to ensure that all welfare programmes are executed with the highest operational excellence and ethical integrity.</p>
                        ",
                        LastUpdated = DateTime.UtcNow
                    },
                    new AboutPage
                    {
                        Slug = "career-with-us",
                        Title = "Career With Us",
                        Subtitle = "Turn Your Passion for Social Justice into a Fulfilling Career",
                        BannerImageUrl = "https://images.unsplash.com/photo-1531482615713-2afd69097998?w=1200&auto=format&fit=crop&q=80",
                        ContentHtml = @"
                            <h3>Join Our Mission-Driven Organization</h3>
                            <p class='lead'>At NGEOS, we believe that the best work happens when passionate, skilled individuals come together to solve some of the world's most pressing challenges.</p>
                            <p>We offer competitive compensation, comprehensive health benefits, flexible remote-friendly work environments, and the unbeatable satisfaction of seeing the direct human impact of your daily efforts.</p>
                            <h4 class='mt-4'>Why Work With NGEOS?</h4>
                            <div class='row g-3 my-2'>
                                <div class='col-md-4'><div class='p-3 bg-light rounded text-center'><h6>Impact-First Culture</h6><p class='small text-muted mb-0'>Make a tangible difference in thousands of lives every day.</p></div></div>
                                <div class='col-md-4'><div class='p-3 bg-light rounded text-center'><h6>Growth & Mentorship</h6><p class='small text-muted mb-0'>Continuous professional development and leadership coaching.</p></div></div>
                                <div class='col-md-4'><div class='p-3 bg-light rounded text-center'><h6>Inclusive Workplace</h6><p class='small text-muted mb-0'>A diverse, collaborative global team driven by shared empathy.</p></div></div>
                            </div>
                        ",
                        LastUpdated = DateTime.UtcNow
                    },
                    new AboutPage
                    {
                        Slug = "our-achievements",
                        Title = "Our Achievements",
                        Subtitle = "Milestones, Lives Impacted & Global Humanitarian Recognition",
                        BannerImageUrl = "https://images.unsplash.com/photo-1511578314322-379afb476865?w=1200&auto=format&fit=crop&q=80",
                        ContentHtml = @"
                            <h3>A Decade of Transformational Humanitarian Impact</h3>
                            <p class='lead'>Since our inception, NGEOS has mobilized millions of dollars in philanthropic funding to change lives across 45 communities worldwide.</p>
                            <div class='row g-4 my-4 text-center'>
                                <div class='col-6 col-md-3'><div class='p-4 bg-light rounded-3 shadow-sm'><h2 class='text-warning fw-bold mb-1'>50K+</h2><p class='text-muted small mb-0'>Lives Directly Uplifted</p></div></div>
                                <div class='col-6 col-md-3'><div class='p-4 bg-light rounded-3 shadow-sm'><h2 class='text-primary fw-bold mb-1'>$4.2M</h2><p class='text-muted small mb-0'>Total Funds Mobilized</p></div></div>
                                <div class='col-6 col-md-3'><div class='p-4 bg-light rounded-3 shadow-sm'><h2 class='text-success fw-bold mb-1'>35+</h2><p class='text-muted small mb-0'>Partner NGOs Supported</p></div></div>
                                <div class='col-6 col-md-3'><div class='p-4 bg-light rounded-3 shadow-sm'><h2 class='text-danger fw-bold mb-1'>100%</h2><p class='text-muted small mb-0'>Audited Transparency</p></div></div>
                            </div>
                            <h4>Key Milestones</h4>
                            <ul>
                                <li><strong>2021:</strong> Launched the Rural Smart Classroom Initiative, equipping 50 schools with solar power.</li>
                                <li><strong>2023:</strong> Awarded the International Humanitarian Excellence Citation for fast medical emergency deployments.</li>
                                <li><strong>2025:</strong> Cross-funded over 120 custom prosthetic limbs for disabled youth in underserved regions.</li>
                                <li><strong>2026:</strong> Expanded NGEOS unified digital donation portal with real-time NGO transparency.</li>
                            </ul>
                        ",
                        LastUpdated = DateTime.UtcNow
                    },
                    new AboutPage
                    {
                        Slug = "our-supporters",
                        Title = "Our Supporters",
                        Subtitle = "Honoring the Generous Corporate Donors, Philanthropic Foundations & Allies",
                        BannerImageUrl = "https://images.unsplash.com/photo-1559526324-4b87b5e36e44?w=1200&auto=format&fit=crop&q=80",
                        ContentHtml = @"
                            <h3>Standing Strong Together With Our Benefactors</h3>
                            <p class='lead'>NGEOS has been established and continuously powered by the generous contributions of major global corporations, philanthropic trusts, and visionary individual donors.</p>
                            <p>Their corporate social responsibility (CSR) investments fund operational infrastructure, technology tools, logistics fleets, and emergency match-funds, ensuring maximum impact for grassroots causes.</p>
                            <p>We extend our deepest gratitude to our corporate sponsors, institutional partners, and every individual donor who stands beside us in our mission to bring hope to humanity.</p>
                        ",
                        LastUpdated = DateTime.UtcNow
                    },
                    new AboutPage
                    {
                        Slug = "read-about-us",
                        Title = "Read About NGEOS",
                        Subtitle = "The Comprehensive Story of NGEOS's Founding, Growth, and Future",
                        BannerImageUrl = "https://images.unsplash.com/photo-1469571486292-0ba58a3f068b?w=1200&auto=format&fit=crop&q=80",
                        ContentHtml = @"
                            <h3>The Story of NGEOS</h3>
                            <p class='lead'>NGEOS was founded with a single powerful realization: while millions of generous individuals want to help the underprivileged, they often lack a trusted, transparent, and unified bridge to verify and support genuine grassroots NGOs.</p>
                            <p>Established as a centralized welfare ecosystem, NGEOS brings together accredited non-governmental organizations dedicated to children welfare, education literacy, emergency healthcare, disabled assistance, women empowerment, and senior citizen care.</p>
                            <h4>How NGEOS Works</h4>
                            <ol>
                                <li><strong>Rigorous NGO Accreditation:</strong> Only verified NGOs with proven track records, legal registrations, and financial integrity are onboarded.</li>
                                <li><strong>Targeted Welfare Causes:</strong> Causes are categorized clearly so donors can directly fund what they care about most.</li>
                                <li><strong>100% Direct Allocation:</strong> Transparent donation tracking ensures funds are allocated directly to planned activities and medical interventions.</li>
                                <li><strong>Holistic Community Engagement:</strong> Citizens can donate, join volunteer programmes, raise queries, invite friends, and participate in community charity events.</li>
                            </ol>
                            <p>Join us as we continue to expand our reach, innovate in charitable technology, and transform thousands more lives every single day.</p>
                        ",
                        LastUpdated = DateTime.UtcNow
                    }
                };
                context.AboutPages.AddRange(aboutPages);
                context.SaveChanges();
            }

            // 9. Seed Team Members if none exist
            if (!context.TeamMembers.Any())
            {
                var team = new List<TeamMember>
                {
                    new TeamMember
                    {
                        Name = "Dr. Eleanor Vance",
                        Role = "Executive Director & Founder",
                        Bio = "Former UN humanitarian advisor with over 18 years leading crisis relief and global child welfare initiatives.",
                        PhotoUrl = "https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?w=400&auto=format&fit=crop&q=80",
                        Email = "e.vance@ngeos.org",
                        LinkedInUrl = "https://linkedin.com/in/eleanor-vance-ngeos",
                        DisplayOrder = 1,
                        IsActive = true
                    },
                    new TeamMember
                    {
                        Name = "Marcus Sterling",
                        Role = "Director of Field Operations & Logistics",
                        Bio = "Logistics expert specializing in disaster relief supply chains and mobile healthcare caravans across rural regions.",
                        PhotoUrl = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=400&auto=format&fit=crop&q=80",
                        Email = "m.sterling@ngeos.org",
                        LinkedInUrl = "https://linkedin.com/in/marcus-sterling-ngeos",
                        DisplayOrder = 2,
                        IsActive = true
                    },
                    new TeamMember
                    {
                        Name = "Dr. Sophia Chen, MD",
                        Role = "Chief Medical Officer",
                        Bio = "Pediatric surgeon and community health advocate leading our mobile clinics and free pediatric surgery drives.",
                        PhotoUrl = "https://images.unsplash.com/photo-1559839734-2b71ea197ec2?w=400&auto=format&fit=crop&q=80",
                        Email = "s.chen@ngeos.org",
                        LinkedInUrl = "https://linkedin.com/in/sophia-chen-ngeos",
                        DisplayOrder = 3,
                        IsActive = true
                    },
                    new TeamMember
                    {
                        Name = "David Rodriguez",
                        Role = "Head of NGO Partnerships & Compliance",
                        Bio = "Oversees due diligence, auditing, and quality assurance for all partner non-governmental organizations.",
                        PhotoUrl = "https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=400&auto=format&fit=crop&q=80",
                        Email = "d.rodriguez@ngeos.org",
                        LinkedInUrl = "https://linkedin.com/in/david-rodriguez-ngeos",
                        DisplayOrder = 4,
                        IsActive = true
                    }
                };
                context.TeamMembers.AddRange(team);
                context.SaveChanges();
            }

            // 10. Seed Job Opportunities if none exist
            if (!context.JobOpportunities.Any())
            {
                var jobs = new List<JobOpportunity>
                {
                    new JobOpportunity
                    {
                        Title = "Regional Humanitarian Coordinator",
                        Department = "Field Operations",
                        JobType = "Full Time",
                        Location = "Northeast Region / Hybrid",
                        Description = "Coordinate on-ground logistics for mobile health clinics, distribute relief supplies, and liaise with local partner NGOs.",
                        Requirements = "Bachelor's degree in Social Work or International Development, 3+ years NGO field experience, strong communication and emergency management skills.",
                        IsActive = true,
                        PostedDate = DateTime.UtcNow.AddDays(-10)
                    },
                    new JobOpportunity
                    {
                        Title = "Digital Fundraising & Campaign Manager",
                        Department = "Marketing & Communications",
                        JobType = "Full Time",
                        Location = "Headquarters / Remote",
                        Description = "Lead digital donor engagement strategies, manage social campaigns, and optimize fundraising donation funnels.",
                        Requirements = "3+ years digital marketing experience in non-profit sector, proficiency in analytics, email marketing, and content storytelling.",
                        IsActive = true,
                        PostedDate = DateTime.UtcNow.AddDays(-8)
                    },
                    new JobOpportunity
                    {
                        Title = "Volunteer Education & Mentorship Lead",
                        Department = "Education Programs",
                        JobType = "Part Time",
                        Location = "Various Rural Centers",
                        Description = "Recruit, train, and guide volunteer tutors providing remedial classes and digital literacy to underprivileged children.",
                        Requirements = "Background in teaching, education management or youth leadership. Passion for community service.",
                        IsActive = true,
                        PostedDate = DateTime.UtcNow.AddDays(-5)
                    }
                };
                context.JobOpportunities.AddRange(jobs);
                context.SaveChanges();
            }

            // 11. Seed FAQs if none exist
            if (!context.FAQs.Any())
            {
                var faqs = new List<FAQ>
                {
                    new FAQ
                    {
                        Question = "How do I make a donation on NGEOS?",
                        Answer = "You can browse our active causes under the 'Donate' or 'Causes' menu, select any cause of your choice (e.g. Children Welfare, Education, Healthcare, Disabled Persons), enter your desired amount, fill in your details and dummy payment card information, and submit. You will immediately receive a transaction receipt.",
                        Category = "Donations & Payment",
                        DisplayOrder = 1,
                        IsActive = true
                    },
                    new FAQ
                    {
                        Question = "Is this a real payment gateway?",
                        Answer = "No, NGEOS operates with a secure dummy payment simulation designed for university and educational demonstrations. No real money or actual card credentials are processed or stored.",
                        Category = "Donations & Payment",
                        DisplayOrder = 2,
                        IsActive = true
                    },
                    new FAQ
                    {
                        Question = "How can an NGO partner with NGEOS?",
                        Answer = "NGOs can apply for partnership by reaching out via our Contact Us page or submitting a partnership query. Our compliance team verifies registration documents, past audit statements, and field credibility before onboarding.",
                        Category = "NGO Partnership",
                        DisplayOrder = 3,
                        IsActive = true
                    },
                    new FAQ
                    {
                        Question = "How can I participate or volunteer in an upcoming programme?",
                        Answer = "Visit the 'Our Programmes' section from the main navigation, select any programme (such as Education Drives, Healthcare Caravans, or Special Needs Festivals), and click 'Show Interest / Volunteer' to submit your participation details.",
                        Category = "Programmes & Volunteering",
                        DisplayOrder = 4,
                        IsActive = true
                    },
                    new FAQ
                    {
                        Question = "Can I invite friends and colleagues to support a cause?",
                        Answer = "Yes! Registered users can use the 'Invite Friends' feature from their dashboard or any cause page to send personalized email invitations to friends.",
                        Category = "General",
                        DisplayOrder = 5,
                        IsActive = true
                    },
                    new FAQ
                    {
                        Question = "How do I raise a query if I need help?",
                        Answer = "You can use the 'Raise Query' button in the Help Centre or contact form. You can select your query category, submit your message, and track the Admin's response directly from your user portal.",
                        Category = "General",
                        DisplayOrder = 6,
                        IsActive = true
                    }
                };
                context.FAQs.AddRange(faqs);
                context.SaveChanges();
            }
        }
    }
}
