"""
NGEOS Project Analysis Report PDF Generator
Creates a professional project report PDF with:
- NGEOS + Aptech logos side by side on cover
- Title page with Sir Name, Student ID, Name, Instructor
- Project analysis, tech stack, database schema
- System flowchart
- No changes to the project folder
"""
from reportlab.lib.pagesizes import A4
from reportlab.lib.units import mm, inch
from reportlab.lib.colors import HexColor, white, black, Color
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.enums import TA_CENTER, TA_LEFT, TA_JUSTIFY, TA_RIGHT
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Image, Table, TableStyle,
    PageBreak, Flowable, HRFlowable, KeepTogether
)
from reportlab.graphics.shapes import Drawing, Rect, String, Circle, Polygon, Path, Line
from reportlab.graphics import renderPDF
from reportlab.pdfgen import canvas as pdfcanvas
import os

# ---------- Colors ----------
NGEOS_ORANGE = HexColor("#FF7A29")
NGEOS_DARK = HexColor("#0B192C")
NGEOS_GOLD = HexColor("#F59E0B")
APTECH_ORANGE = HexColor("#F5A623")
APTECH_DARK = HexColor("#000000")
GREY_LIGHT = HexColor("#F4F6F8")
GREY_MED = HexColor("#6B7280")
ACCENT = HexColor("#2563EB")

PAGE_W, PAGE_H = A4
MARGIN = 20 * mm

# ---------- Styles ----------
styles = getSampleStyleSheet()

title_style = ParagraphStyle(
    'ReportTitle', parent=styles['Title'],
    fontSize=26, textColor=NGEOS_DARK, alignment=TA_CENTER, spaceAfter=6,
    fontName='Helvetica-Bold'
)
subtitle_style = ParagraphStyle(
    'ReportSubtitle', parent=styles['Normal'],
    fontSize=14, textColor=GREY_MED, alignment=TA_CENTER, spaceAfter=2,
    fontName='Helvetica'
)
h1_style = ParagraphStyle(
    'H1', parent=styles['Heading1'],
    fontSize=16, textColor=NGEOS_DARK, spaceBefore=14, spaceAfter=8,
    fontName='Helvetica-Bold'
)
h2_style = ParagraphStyle(
    'H2', parent=styles['Heading2'],
    fontSize=13, textColor=ACCENT, spaceBefore=10, spaceAfter=6,
    fontName='Helvetica-Bold'
)
body_style = ParagraphStyle(
    'Body', parent=styles['Normal'],
    fontSize=10, textColor=black, alignment=TA_JUSTIFY, spaceAfter=6,
    fontName='Helvetica', leading=14
)
body_left = ParagraphStyle(
    'BodyLeft', parent=body_style, alignment=TA_LEFT
)
bullet_style = ParagraphStyle(
    'Bullet', parent=body_style, leftIndent=14, bulletIndent=4, spaceAfter=3,
    alignment=TA_LEFT
)
table_header_style = ParagraphStyle(
    'TableHeader', parent=styles['Normal'],
    fontSize=9, textColor=white, fontName='Helvetica-Bold', alignment=TA_CENTER
)
table_cell_style = ParagraphStyle(
    'TableCell', parent=styles['Normal'],
    fontSize=8.5, textColor=black, fontName='Helvetica', alignment=TA_LEFT, leading=11
)
table_cell_c = ParagraphStyle(
    'TableCellC', parent=table_cell_style, alignment=TA_CENTER
)

# ---------- Logo Drawings ----------

def draw_ngeos_logo(dwg_width=60, dwg_height=60):
    """Draw NGEOS logo as a reportlab Drawing."""
    d = Drawing(dwg_width, dwg_height)
    cx, cy = dwg_width / 2, dwg_height / 2
    # Outer rounded badge (orange gradient approx)
    badge = Rect(4, 4, dwg_width - 8, dwg_height - 8, rx=8, ry=8,
                 fillColor=NGEOS_ORANGE, strokeColor=None)
    d.add(badge)
    # Inner border
    inner = Rect(5.5, 5.5, dwg_width - 11, dwg_height - 11, rx=7, ry=7,
                 fillColor=None, strokeColor=Color(1, 1, 1, alpha=0.3), strokeWidth=1)
    d.add(inner)
    # Golden sparkles at top
    for (px, py, r) in [(cx, 12, 2), (cx - 9, 14, 1.5), (cx + 9, 14, 1.5)]:
        d.add(Circle(px, py, r, fillColor=NGEOS_GOLD, strokeColor=None))
    # Heart shape (path)
    heart = Path(
        fillColor=Color(1, 1, 1, alpha=0.95), strokeColor=None
    )
    # Simplified heart path scaled to badge
    hx, hy = cx, cy - 2
    heart.moveTo(hx, hy - 6)
    heart.curveTo(hx - 6, hy - 10, hx - 10, hy - 8, hx - 12, hy - 2)
    heart.curveTo(hx - 16, hy + 6, hx - 14, hy + 16, hx - 6, hy + 24)
    heart.curveTo(hx - 1, hy + 28, hx + 4, hy + 30, hx, hy + 31)
    heart.curveTo(hx + 4, hy + 30, hx + 9, hy + 28, hx + 14, hy + 24)
    heart.curveTo(hx + 22, hy + 16, hx + 24, hy + 6, hx + 20, hy - 2)
    heart.curveTo(hx + 16, hy - 8, hx + 12, hy - 10, hx + 6, hy - 6)
    heart.curveTo(hx + 2, hy - 2, hx + 1, hy - 4, hx, hy - 6)
    d.add(heart)
    # Supporting hands (side cradles) - simplified
    left_hand = Path(fillColor=Color(1, 1, 1, alpha=0.9), strokeColor=None)
    left_hand.moveTo(8, 42)
    left_hand.curveTo(6, 36, 6, 28, 10, 24)
    left_hand.curveTo(11, 22, 12, 22, 13, 24)
    left_hand.curveTo(14, 26, 13, 28, 12, 30)
    left_hand.curveTo(10, 33, 10, 36, 12, 39)
    left_hand.curveTo(15, 42, 19, 43, 22, 42)
    left_hand.curveTo(23, 41, 24, 42, 24, 44)
    left_hand.curveTo(24, 46, 23, 48, 22, 49)
    left_hand.curveTo(18, 51, 14, 50, 12, 49)
    left_hand.curveTo(10, 48, 10, 46, 11, 45)
    left_hand.curveTo(12, 44, 13, 43, 12, 42)
    left_hand.curveTo(10, 42, 9, 42, 8, 42)
    d.add(left_hand)

    right_hand = Path(fillColor=Color(1, 1, 1, alpha=0.9), strokeColor=None)
    right_hand.moveTo(dwg_width - 8, 42)
    right_hand.curveTo(dwg_width - 6, 36, dwg_width - 6, 28, dwg_width - 10, 24)
    right_hand.curveTo(dwg_width - 11, 22, dwg_width - 12, 22, dwg_width - 13, 24)
    right_hand.curveTo(dwg_width - 14, 26, dwg_width - 13, 28, dwg_width - 12, 30)
    right_hand.curveTo(dwg_width - 10, 33, dwg_width - 10, 36, dwg_width - 12, 39)
    right_hand.curveTo(dwg_width - 15, 42, dwg_width - 19, 43, dwg_width - 22, 42)
    right_hand.curveTo(dwg_width - 23, 41, dwg_width - 24, 42, dwg_width - 24, 44)
    right_hand.curveTo(dwg_width - 24, 46, dwg_width - 23, 48, dwg_width - 22, 49)
    right_hand.curveTo(dwg_width - 18, 51, dwg_width - 14, 50, dwg_width - 12, 49)
    right_hand.curveTo(dwg_width - 10, 48, dwg_width - 10, 46, dwg_width - 11, 45)
    right_hand.curveTo(dwg_width - 12, 44, dwg_width - 13, 43, dwg_width - 12, 42)
    right_hand.curveTo(dwg_width - 10, 42, dwg_width - 9, 42, dwg_width - 8, 42)
    d.add(right_hand)
    # NGEOS text
    d.add(String(cx, 16, 'NGEOS', fontName='Helvetica-Bold', fontSize=11,
                 fillColor=white, textAnchor='middle'))
    return d


def draw_aptech_logo(dwg_width=100, dwg_height=55):
    """Draw Aptech Learning logo as a reportlab Drawing."""
    d = Drawing(dwg_width, dwg_height)
    # Slanted background parallelogram
    slant = 14
    bg = Polygon(
        [slant, 0, dwg_width, 0, dwg_width - slant, dwg_height, 0, dwg_height],
        fillColor=APTECH_DARK, strokeColor=None
    )
    d.add(bg)
    # White border line around
    border = Polygon(
        [slant + 1, 1, dwg_width - 1, 1, dwg_width - slant - 1, dwg_height - 1, 1, dwg_height - 1],
        fillColor=None, strokeColor=white, strokeWidth=1.5
    )
    d.add(border)
    # "Aptech" text in orange
    d.add(String(20, dwg_height - 22, 'Aptech', fontName='Helvetica-Bold',
                 fontSize=22, fillColor=APTECH_ORANGE, textAnchor='start'))
    # "LEARNING" text in white below
    d.add(String(20, dwg_height - 40, 'LEARNING', fontName='Helvetica-Bold',
                 fontSize=12, fillColor=white, textAnchor='start'))
    # Paw print on right side
    paw_x = dwg_width - 30
    paw_y = dwg_height / 2
    # Paw pad (main)
    d.add(Circle(paw_x + 2, paw_y - 4, 7, fillColor=Color(0.5, 0.5, 0.5), strokeColor=None))
    d.add(Circle(paw_x + 2, paw_y - 4, 3, fillColor=APTECH_ORANGE, strokeColor=None))
    # Toes
    for (px, py) in [(paw_x - 5, paw_y + 3), (paw_x - 1, paw_y + 6),
                     (paw_x + 4, paw_y + 6), (paw_x + 9, paw_y + 3)]:
        d.add(Circle(px, py, 3.5, fillColor=Color(0.5, 0.5, 0.5), strokeColor=None))
    # TM symbol
    d.add(String(dwg_width - 2, 2, 'TM', fontName='Helvetica', fontSize=5,
                 fillColor=white, textAnchor='end'))
    return d


# ---------- Flowchart ----------

def draw_flowchart(dwg_width=170, dwg_height=200):
    """Draw a simple system flow chart."""
    d = Drawing(dwg_width, dwg_height)
    box_w, box_h = 42, 22
    gap_x, gap_y = 8, 26

    # Box style
    def add_box(x, y, text, fill=NGEOS_DARK, text_color=white, fontsize=8):
        d.add(Rect(x, y, box_w, box_h, rx=4, ry=4, fillColor=fill, strokeColor=None))
        d.add(String(x + box_w / 2, y + box_h / 2 + 2, text,
                     fontName='Helvetica-Bold', fontSize=fontsize,
                     fillColor=text_color, textAnchor='middle'))

    def add_arrow(x1, y1, x2, y2):
        d.add(Line(x1, y1, x2, y2, strokeColor=GREY_MED, strokeWidth=1.2))
        # arrowhead
        d.add(Polygon([x2, y2, x2 - 3, y2 - 3, x2 - 3, y2 + 3],
                      fillColor=GREY_MED, strokeColor=None))

    # Row 1: User
    add_box(10, dwg_height - box_h - 10, 'User / Donor', fill=ACCENT)
    # Row 2: Web Frontend
    add_box(10, dwg_height - 2 * box_h - 2 * gap_y - 10, 'ASP.NET Core MVC Frontend', fill=NGEOS_ORANGE)
    # Row 3: Controllers
    add_box(10, dwg_height - 3 * box_h - 3 * gap_y - 10, 'MVC Controllers', fill=NGEOS_DARK)
    # Row 4: Services / Models
    add_box(10, dwg_height - 4 * box_h - 4 * gap_y - 10, 'Services & Models (EF Core)', fill=NGEOS_DARK)
    # Row 5: Database
    add_box(10, dwg_height - 5 * box_h - 5 * gap_y - 10, 'SQL Server Database', fill=ACCENT)

    # Arrows down
    for i in range(5):
        y_top = dwg_height - (i + 1) * box_h - (i + 1) * gap_y - 10 + box_h
        y_bot = dwg_height - (i + 2) * box_h - (i + 2) * gap_y - 10
        add_arrow(10 + box_w / 2, y_top, 10 + box_w / 2, y_bot)

    # Side branch: Firebase OTP
    add_box(10 + box_w + gap_x + 60, dwg_height - box_h - 10, 'Firebase SMS OTP', fill=GREY_MED, fontsize=7)
    add_arrow(10 + box_w, dwg_height - box_h - 10 + box_h / 2,
              10 + box_w + gap_x + 60, dwg_height - box_h - 10 + box_h / 2)

    # Side branch: Email SMTP
    add_box(10 + box_w + gap_x + 60, dwg_height - 2 * box_h - 2 * gap_y - 10, 'Gmail SMTP Email', fill=GREY_MED, fontsize=7)
    add_arrow(10 + box_w, dwg_height - 2 * box_h - 2 * gap_y - 10 + box_h / 2,
              10 + box_w + gap_x + 60, dwg_height - 2 * box_h - 2 * gap_y - 10 + box_h / 2)

    # Side branch: Desktop App
    add_box(dwg_width - box_w - 10, dwg_height - box_h - 10, 'Windows Desktop App', fill=NGEOS_ORANGE, fontsize=7)
    add_arrow(10 + box_w, dwg_height - box_h - 10 + box_h / 2,
              dwg_width - box_w - 10, dwg_height - box_h - 10 + box_h / 2)

    return d


# ---------- Page Number / Footer ----------

class NumberedCanvas(pdfcanvas.Canvas):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self._saved_page_states = []

    def showPage(self):
        self._saved_page_states.append(dict(self.__dict__))
        self._startPage()

    def save(self):
        num_pages = len(self._saved_page_states)
        for state in self._saved_page_states:
            self.__dict__.update(state)
            self.draw_footer(num_pages)
            super().showPage()
        super().save()

    def draw_footer(self, total_pages):
        self.saveState()
        self.setStrokeColor(GREY_MED)
        self.setLineWidth(0.5)
        self.line(MARGIN, 14 * mm, PAGE_W - MARGIN, 14 * mm)
        self.setFont('Helvetica', 7.5)
        self.setFillColor(GREY_MED)
        self.drawString(MARGIN, 10 * mm, 'NGEOS Project Analysis Report - Aptech Learning, Shahrah-e-Faisal Center')
        self.drawRightString(PAGE_W - MARGIN, 10 * mm, f'Page {self._pageNumber} of {total_pages}')
        self.restoreState()


# ---------- Build Content ----------

def build_story(student_name, student_id, sir_name, instructor_name, course_name):
    story = []

    # ======== COVER PAGE ========
    # Top decorative band
    story.append(Spacer(1, 15 * mm))

    # Logos side by side
    ngeos_logo_dwg = draw_ngeos_logo(55, 55)
    aptech_logo_dwg = draw_aptech_logo(110, 50)

    logo_table = Table(
        [[Image(ngeos_logo_dwg, width=55, height=55),
          Image(aptech_logo_dwg, width=110, height=50)]],
        colWidths=[70 * mm, 130 * mm]
    )
    logo_table.setStyle(TableStyle([
        ('VALIGN', (0, 0), (-1, -1), 'MIDDLE'),
        ('ALIGN', (0, 0), (-1, -1), 'CENTER'),
        ('LEFTPADDING', (0, 0), (-1, -1), 0),
        ('RIGHTPADDING', (0, 0), (-1, -1), 0),
        ('TOPPADDING', (0, 0), (-1, -1), 0),
        ('BOTTOMPADDING', (0, 0), (-1, -1), 0),
    ]))
    story.append(logo_table)
    story.append(Spacer(1, 10 * mm))

    # Title
    story.append(Paragraph('Project Analysis Report', title_style))
    story.append(Paragraph('NGEOS - Next-Generation NGO &amp; Welfare Platform', subtitle_style))
    story.append(Spacer(1, 4 * mm))
    story.append(HRFlowable(width='60%', thickness=1, color=NGEOS_ORANGE, hAlign='CENTER'))
    story.append(Spacer(1, 8 * mm))

    # Student Info Table
    info_data = [
        [Paragraph('Student Name', table_header_style),
         Paragraph(student_name, table_cell_style)],
        [Paragraph('Student ID', table_header_style),
         Paragraph(student_id, table_cell_style)],
        [Paragraph('Sir / Supervisor', table_header_style),
         Paragraph(sir_name, table_cell_style)],
        [Paragraph('Instructor', table_header_style),
         Paragraph(instructor_name, table_cell_style)],
        [Paragraph('Course / Subject', table_header_style),
         Paragraph(course_name, table_cell_style)],
        [Paragraph('Institution', table_header_style),
         Paragraph('Aptech Learning - Shahrah-e-Faisal Center', table_cell_style)],
    ]
    info_table = Table(info_data, colWidths=[45 * mm, 115 * mm])
    info_table.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (0, -1), GREY_LIGHT),
        ('GRID', (0, 0), (-1, -1), 0.5, GREY_MED),
        ('VALIGN', (0, 0), (-1, -1), 'MIDDLE'),
        ('LEFTPADDING', (0, 0), (-1, -1), 8),
        ('RIGHTPADDING', (0, 0), (-1, -1), 8),
        ('TOPPADDING', (0, 0), (-1, -1), 6),
        ('BOTTOMPADDING', (0, 0), (-1, -1), 6),
    ]))
    story.append(info_table)
    story.append(Spacer(1, 10 * mm))

    # Project tagline
    story.append(Paragraph(
        'Empowering underprivileged communities worldwide across Education, '
        'Healthcare, Food Relief, Elderly &amp; Women Welfare.',
        ParagraphStyle('Tagline', parent=body_style, alignment=TA_CENTER,
                       fontSize=10, textColor=GREY_MED, fontName='Helvetica-Oblique')
    ))
    story.append(PageBreak())

    # ======== TABLE OF CONTENTS ========
    story.append(Paragraph('Table of Contents', h1_style))
    story.append(HRFlowable(width='100%', thickness=1, color=NGEOS_ORANGE))
    story.append(Spacer(1, 6 * mm))

    toc_items = [
        ('1.', 'Project Overview'),
        ('2.', 'Key Features'),
        ('3.', 'Technical Architecture'),
        ('4.', 'Tech Stack &amp; Tools'),
        ('5.', 'Database Schema (18 Tables)'),
        ('6.', 'Controllers &amp; Models'),
        ('7.', 'Authentication &amp; Security'),
        ('8.', 'System Flowchart'),
        ('9.', 'Conclusion'),
    ]
    for num, item in toc_items:
        story.append(Paragraph(f'{num}&nbsp;&nbsp;{item}', ParagraphStyle(
            'TOCItem', parent=body_style, fontSize=11, spaceAfter=5,
            textColor=NGEOS_DARK, fontName='Helvetica-Bold' if num.endswith('.') else 'Helvetica'
        )))
    story.append(PageBreak())

    # ======== 1. PROJECT OVERVIEW ========
    story.append(Paragraph('1. Project Overview', h1_style))
    story.append(HRFlowable(width='100%', thickness=1, color=NGEOS_ORANGE))
    story.append(Spacer(1, 5 * mm))

    story.append(Paragraph(
        '<b>NGEOS</b> (Next-Generation NGO &amp; Welfare Platform) is an enterprise-grade digital charity '
        'and non-profit welfare platform designed to unite accredited NGOs, philanthropists, corporate '
        'benefactors, and community volunteers under a unified ecosystem.<br/><br/>'
        'Built with <b>ASP.NET Core (C#)</b>, <b>Entity Framework Core</b>, and <b>Microsoft SQL Server</b>, '
        'NGEOS supports both modern browser access and a <b>standalone Windows Desktop Application</b> mode '
        'with zero console windows, offline capabilities, and real-time online SMS OTP verification.<br/><br/>'
        'The platform is deployed live at <b>https://ngeos.runasp.net</b> with Let\'s Encrypt SSL encryption '
        'and remote cloud MS SQL database hosting on the MonsterASP.NET cloud platform.',
        body_style
    ))
    story.append(Spacer(1, 4 * mm))

    story.append(Paragraph('Purpose', h2_style))
    story.append(Paragraph(
        'The core objective of NGEOS is to digitize NGO operations, making charity campaigns, donation '
        'management, Zakat calculation, and beneficiary programmes transparent, accessible, and efficient. '
        'It bridges the gap between donors and welfare organizations through a secure, role-based web portal.',
        body_style
    ))

    # ======== 2. KEY FEATURES ========
    story.append(Paragraph('2. Key Features', h1_style))
    story.append(HRFlowable(width='100%', thickness=1, color=NGEOS_ORANGE))
    story.append(Spacer(1, 5 * mm))

    features = [
        ('Causes &amp; Campaigns',
         'Dynamic charity campaigns with goal targets, real-time raised amounts, percentage progress bars, '
         'and categorized relief programs including Children Welfare, Universal Education, Healthcare, '
         'Disabled Persons, Women Welfare, and Elderly Care.'),
        ('Smart Zakat Calculator',
         'Automated Zakat calculation based on Islamic guidelines with live inputs for Gold, Silver, '
         'Liquid Cash, Investments, and Business Merchandise against the Nisab threshold.'),
        ('Instant Donations &amp; Receipts',
         'Streamlined donation workflow with preset denominations ($25, $50, $100, $250), custom inputs, '
         'auto-formatted card details with masking, and instant digital receipt confirmation.'),
        ('Native Windows Desktop Application',
         'Zero-terminal background launcher and single-click executable installer that opens in a clean '
         'standalone window without URL bars or CMD windows.'),
        ('Dual Forgot Password Verification',
         'Email recovery via Gmail SMTP with 6-digit tokens, and SMS phone recovery via Google Firebase OTP.'),
        ('Elite Visual Experience',
         'Dark-themed responsive UI built with Bootstrap 5, Bootstrap Icons, and AOS scroll animations.'),
    ]
    for title, desc in features:
        story.append(Paragraph(
            f'<b>{title}</b>: {desc}', ParagraphStyle(
                'FeatureItem', parent=body_style, leftIndent=10, spaceAfter=5,
                bulletText='\u2022', alignment=TA_LEFT
            )
        ))

    # ======== 3. TECHNICAL ARCHITECTURE ========
    story.append(Paragraph('3. Technical Architecture', h1_style))
    story.append(HRFlowable(width='100%', thickness=1, color=NGEOS_ORANGE))
    story.append(Spacer(1, 5 * mm))

    arch_data = [
        [Paragraph('Layer', table_header_style), Paragraph('Technology / Component', table_header_style),
         Paragraph('Responsibility', table_header_style)],
        [Paragraph('Presentation', table_cell_c), Paragraph('ASP.NET Core MVC + Razor Views + Bootstrap 5', table_cell_style),
         Paragraph('User interface, responsive layouts, forms, and dashboards.', table_cell_style)],
        [Paragraph('Application', table_cell_c), Paragraph('C# Controllers + ViewModels + Services', table_cell_style),
         Paragraph('Request handling, business logic, email/SMS services, OTP verification.', table_cell_style)],
        [Paragraph('Data Access', table_cell_c), Paragraph('Entity Framework Core (Code-First)', table_cell_style),
         Paragraph('ORM mapping, LINQ queries, database migrations.', table_cell_style)],
        [Paragraph('Database', table_cell_c), Paragraph('Microsoft SQL Server (18 tables)', table_cell_style),
         Paragraph('Persistent storage for users, causes, donations, programmes, NGOs, etc.', table_cell_style)],
        [Paragraph('Authentication', table_cell_c), Paragraph('Google OAuth 2.0 + Email/Password + Firebase SMS OTP', table_cell_style),
         Paragraph('Secure login, role-based access (Admin/User), password hashing.', table_cell_style)],
        [Paragraph('Deployment', table_cell_c), Paragraph('MonsterASP.NET Cloud + IIS / Kestrel + Windows Desktop', table_cell_style),
         Paragraph('Live web hosting and standalone desktop installer.', table_cell_style)],
    ]
    arch_table = Table(arch_data, colWidths=[28 * mm, 55 * mm, 77 * mm], repeatRows=1)
    arch_table.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, 0), NGEOS_DARK),
        ('GRID', (0, 0), (-1, -1), 0.5, GREY_MED),
        ('VALIGN', (0, 0), (-1, -1), 'MIDDLE'),
        ('ROWBACKGROUNDS', (0, 1), (-1, -1), [white, GREY_LIGHT]),
        ('LEFTPADDING', (0, 0), (-1, -1), 6),
        ('RIGHTPADDING', (0, 0), (-1, -1), 6),
        ('TOPPADDING', (0, 0), (-1, -1), 5),
        ('BOTTOMPADDING', (0, 0), (-1, -1), 5),
    ]))
    story.append(arch_table)
    story.append(PageBreak())

    # ======== 4. TECH STACK ========
    story.append(Paragraph('4. Tech Stack &amp; Tools', h1_style))
    story.append(HRFlowable(width='100%', thickness=1, color=NGEOS_ORANGE))
    story.append(Spacer(1, 5 * mm))

    stack_data = [
        [Paragraph('Component', table_header_style), Paragraph('Technology', table_header_style),
         Paragraph('Details', table_header_style)],
        [Paragraph('Backend Framework', table_cell_style), Paragraph('.NET 10 / ASP.NET Core MVC (C#)', table_cell_style),
         Paragraph('Server-side web framework.', table_cell_style)],
        [Paragraph('Database Engine', table_cell_style), Paragraph('Microsoft SQL Server', table_cell_style),
         Paragraph('Local &amp; cloud production database.', table_cell_style)],
        [Paragraph('ORM', table_cell_style), Paragraph('Entity Framework Core', table_cell_style),
         Paragraph('Code-First with migrations.', table_cell_style)],
        [Paragraph('Frontend Styling', table_cell_style), Paragraph('Bootstrap 5.3.3', table_cell_style),
         Paragraph('Responsive UI design system.', table_cell_style)],
        [Paragraph('Icons', table_cell_style), Paragraph('Bootstrap Icons 1.11.3', table_cell_style),
         Paragraph('UI iconography.', table_cell_style)],
        [Paragraph('SMS OTP', table_cell_style), Paragraph('Firebase Authentication', table_cell_style),
         Paragraph('Phone number OTP verification.', table_cell_style)],
        [Paragraph('Email SMTP', table_cell_style), Paragraph('Gmail SMTP', table_cell_style),
         Paragraph('Secure SSL mailer service.', table_cell_style)],
        [PasswordReset := Paragraph('Desktop Mode', table_cell_style), Paragraph('Windows Desktop App', table_cell_style),
         Paragraph('PWA Standalone + VBScript/WinExe launcher.', table_cell_style)],
        [Paragraph('Hosting', table_cell_style), Paragraph('MonsterASP.NET Cloud', table_cell_style),
         Paragraph('Production web &amp; database hosting.', table_cell_style)],
        [Paragraph('SSL', table_cell_style), Paragraph('Let\'s Encrypt', table_cell_style),
         Paragraph('256-bit HTTPS certificate.', table_cell_style)],
    ]
    stack_table = Table(stack_data, colWidths=[35 * mm, 50 * mm, 75 * mm], repeatRows=1)
    stack_table.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, 0), NGEOS_DARK),
        ('GRID', (0, 0), (-1, -1), 0.5, GREY_MED),
        ('VALIGN', (0, 0), (-1, -1), 'MIDDLE'),
        ('ROWBACKGROUNDS', (0, 1), (-1, -1), [white, GREY_LIGHT]),
        ('LEFTPADDING', (0, 0), (-1, -1), 6),
        ('RIGHTPADDING', (0, 0), (-1, -1), 6),
        ('TOPPADDING', (0, 0), (-1, -1), 4),
        ('BOTTOMPADDING', (0, 0), (-1, -1), 4),
    ]))
    story.append(stack_table)

    # ======== 5. DATABASE SCHEMA ========
    story.append(Paragraph('5. Database Schema (18 Tables)', h1_style))
    story.append(HRFlowable(width='100%', thickness=1, color=NGEOS_ORANGE))
    story.append(Spacer(1, 5 * mm))

    story.append(Paragraph(
        'The NGEOS database (NGEOSDb) contains 18 core tables created via Entity Framework Core Code-First '
        'migrations. The schema supports users, NGOs, causes, donations, programmes, events, and more.',
        body_style
    ))
    story.append(Spacer(1, 3 * mm))

    db_data = [
        [Paragraph('Table', table_header_style), Paragraph('Purpose', table_header_style)],
        [Paragraph('Users', table_cell_style), Paragraph('Platform users with roles (Admin/User), authentication, profiles.', table_cell_style)],
        [Paragraph('Roles', table_cell_style), Paragraph('Role-based access control definitions.', table_cell_style)],
        [Paragraph('Causes', table_cell_style), Paragraph('Charity campaigns with targets, raised amounts, categories.', table_cell_style)],
        [Paragraph('Donations', table_cell_style), Paragraph('Donation transactions, payment status, masked card details.', table_cell_style)],
        [Paragraph('NGOs', table_cell_style), Paragraph('Registered non-governmental organizations.', table_cell_style)],
        [Paragraph('Programmes', table_cell_style), Paragraph('Welfare programmes linked to NGOs.', table_cell_style)],
        [Paragraph('ProgrammeInterests', table_cell_style), Paragraph('User interest submissions for programmes.', table_cell_style)],
        [Paragraph('Events', table_cell_style), Paragraph('NGO events with dates, locations, organizers.', table_cell_style)],
        [Paragraph('GalleryItems', table_cell_style), Paragraph('Event/gallery photos and media.', table_cell_style)],
        [Paragraph('AboutPages', table_cell_style), Paragraph('Static about page content.', table_cell_style)],
        [Paragraph('FAQs', table_cell_style), Paragraph('Frequently asked questions by category.', table_cell_style)],
        [Paragraph('ContactMessages', table_cell_style), Paragraph('Visitor contact form submissions.', table_cell_style)],
        [Paragraph('UserQueries', table_cell_style), Paragraph('User support queries with admin replies.', table_cell_style)],
        [Paragraph('JobOpportunities', table_cell_style), Paragraph('NGO job listings.', table_cell_style)],
        [Paragraph('Partners', table_cell_style), Paragraph('Partner organizations and logos.', table_cell_style)],
        [Paragraph('TeamMembers', table_cell_style), Paragraph('NGO team member profiles.', table_cell_style)],
        [Paragraph('PasswordResetTokens', table_cell_style), Paragraph('Password reset OTP tokens and sessions.', table_cell_style)],
        [Paragraph('ZakatCalculations', table_cell_style), Paragraph('Zakat calculation records.', table_cell_style)],
        [Paragraph('FriendInvitations', table_cell_style), Paragraph('Referral invitations between users.', table_cell_style)],
    ]
    db_table = Table(db_data, colWidths=[40 * mm, 120 * mm], repeatRows=1)
    db_table.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, 0), NGEOS_DARK),
        ('GRID', (0, 0), (-1, -1), 0.5, GREY_MED),
        ('VALIGN', (0, 0), (-1, -1), 'MIDDLE'),
        ('ROWBACKGROUNDS', (0, 1), (-1, -1), [white, GREY_LIGHT]),
        ('LEFTPADDING', (0, 0), (-1, -1), 6),
        ('RIGHTPADDING', (0, 0), (-1, -1), 6),
        ('TOPPADDING', (0, 0), (-1, -1), 4),
        ('BOTTOMPADDING', (0, 0), (-1, -1), 4),
    ]))
    story.append(db_table)
    story.append(PageBreak())

    # ======== 6. CONTROLLERS & MODELS ========
    story.append(Paragraph('6. Controllers &amp; Models', h1_style))
    story.append(HRFlowable(width='100%', thickness=1, color=NGEOS_ORANGE))
    story.append(Spacer(1, 5 * mm))

    story.append(Paragraph('Controllers (12):', h2_style))
    controllers = [
        'HomeController', 'AccountController', 'AdminController', 'CausesController',
        'DonateController', 'EventsController', 'GalleryController', 'NgosController',
        'ProgrammesController', 'UserController', 'ZakatController', 'AboutController'
    ]
    ctrl_text = ' | '.join(controllers)
    story.append(Paragraph(ctrl_text, ParagraphStyle('CtrlList', parent=body_style, alignment=TA_CENTER, spaceAfter=6, fontSize=9)))
    story.append(Spacer(1, 2 * mm))

    story.append(Paragraph('Models (18):', h2_style))
    models = [
        'User', 'Cause', 'Donation', 'NGO', 'Programme', 'Event', 'AboutPage', 'FAQ',
        'ContactMessage', 'UserQuery', 'JobOpportunity', 'Partner', 'TeamMember',
        'GalleryItem', 'PasswordResetToken', 'ProgrammeInterest', 'FriendInvitation', 'ZakatCalculation'
    ]
    model_text = ' | '.join(models)
    story.append(Paragraph(model_text, ParagraphStyle('ModelList', parent=body_style, alignment=TA_CENTER, spaceAfter=6, fontSize=9)))

    # ======== 7. AUTHENTICATION & SECURITY ========
    story.append(Paragraph('7. Authentication &amp; Security', h1_style))
    story.append(HRFlowable(width='100%', thickness=1, color=NGEOS_ORANGE))
    story.append(Spacer(1, 5 * mm))

    security_points = [
        'Dual Authentication: Google OAuth 2.0 Single Sign-On + Email &amp; Password authentication.',
        'Role-Based Access Control (RBAC): Admin and User authorization policies (AdminOnly, UserOnly).',
        'Anti-Forgery Validation: CSRF token verification across all forms and donation checkout requests.',
        'Password Hashing: PBKDF2 / Argon2 cryptographic password hashing via PasswordHelper.',
        'Firebase SMS OTP Verification: High-security one-time SMS verification to user mobile numbers.',
        'Email Recovery: Secure 6-digit tokens via Gmail SMTP for forgot password.',
    ]
    for pt in security_points:
        story.append(Paragraph(
            f'<b>\u2022</b>&nbsp;&nbsp;{pt}', ParagraphStyle(
                'SecItem', parent=body_style, leftIndent=10, spaceAfter=3, alignment=TA_LEFT
            )
        ))

    story.append(PageBreak())

    # ======== 8. FLOWCHART ========
    story.append(Paragraph('8. System Flowchart', h1_style))
    story.append(HRFlowable(width='100%', thickness=1, color=NGEOS_ORANGE))
    story.append(Spacer(1, 5 * mm))

    story.append(Paragraph(
        'The flowchart below illustrates the high-level system architecture and data flow from the end user '
        'through the application layers to the database, including external integrations (Firebase SMS OTP, '
        'Gmail SMTP) and the Windows Desktop Application mode.',
        body_style
    ))
    story.append(Spacer(1, 8 * mm))

    fc_drawing = draw_flowchart(170, 210)
    story.append(Image(fc_drawing, width=170 * mm, height=210 * mm))
    story.append(Spacer(1, 6 * mm))
    story.append(Paragraph(
        'Figure 1: NGEOS System Architecture Flowchart - User interaction flow through MVC controllers, '
        'services/models, SQL Server database, and external notification services.',
        ParagraphStyle('FigCaption', parent=body_style, alignment=TA_CENTER, fontSize=9, textColor=GREY_MED)
    ))
    story.append(PageBreak())

    # ======== 9. CONCLUSION ========
    story.append(Paragraph('9. Conclusion', h1_style))
    story.append(HRFlowable(width='100%', thickness=1, color=NGEOS_ORANGE))
    story.append(Spacer(1, 5 * mm))

    story.append(Paragraph(
        'NGEOS is a comprehensive, production-ready NGO and welfare management platform that combines a '
        'modern ASP.NET Core web application with a native Windows desktop experience. With robust '
        'authentication, secure donation processing, Zakat calculation, and a well-structured 18-table '
        'database, NGEOS provides a complete digital ecosystem for charitable organizations to manage '
        'campaigns, donors, and beneficiaries efficiently.<br/><br/>'
        'The platform is already live at <b>https://ngeos.runasp.net</b> and demonstrates a professional-grade '
        'solution suitable for real-world NGO deployment.',
        body_style
    ))
    story.append(Spacer(1, 10 * mm))

    # Footer note
    story.append(HRFlowable(width='100%', thickness=0.75, color=GREY_MED))
    story.append(Spacer(1, 4 * mm))
    story.append(Paragraph(
        'Prepared for: Aptech Learning - Shahrah-e-Faisal Center<br/>'
        'Project: NGEOS - Next-Generation NGO &amp; Welfare Platform<br/>'
        'No modifications were made to the original project folder during this analysis.',
        ParagraphStyle('FooterNote', parent=body_style, alignment=TA_CENTER, fontSize=8.5, textColor=GREY_MED)
    ))

    return story


# ---------- Main ----------

def main():
    # Placeholder details (as requested by user)
    student_name = '[Student Name]'
    student_id = '[Student ID]'
    sir_name = '[Sir / Supervisor Name]'
    instructor_name = '[Instructor Name]'
    course_name = 'NGO'

    output_path = 'C:/Users/Saim/Desktop/NGEOS/NGEOS_Project_Analysis_Report.pdf'

    doc = SimpleDocTemplate(
        output_path,
        pagesize=A4,
        leftMargin=MARGIN, rightMargin=MARGIN,
        topMargin=18 * mm, bottomMargin=20 * mm,
        title='NGEOS Project Analysis Report',
        author='NGEOS Project Team',
        subject='NGO & Welfare Platform Project Report',
        creator='ReportLab'
    )

    story = build_story(student_name, student_id, sir_name, instructor_name, course_name)
    doc.build(story, canvasmaker=NumberedCanvas)
    print(f'PDF generated successfully: {output_path}')


if __name__ == '__main__':
    main()