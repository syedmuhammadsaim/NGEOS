// NGEOS Modern Frontend Interactions & Helpers

document.addEventListener('DOMContentLoaded', function () {
    // 0. Auto-relocate all Bootstrap modals to <body> to prevent stacking context or transform clipping
    const moveModalsToBody = () => {
        document.querySelectorAll('.modal').forEach(m => {
            if (m.parentElement && m.parentElement !== document.body) {
                document.body.appendChild(m);
            }
        });
    };
    moveModalsToBody();

    // Prevent Bootstrap from injecting unwanted padding-right that pushes navbar out of screen
    document.addEventListener('show.bs.modal', function () {
        document.body.style.paddingRight = '0px';
    });
    document.addEventListener('shown.bs.modal', function () {
        document.body.style.paddingRight = '0px';
    });
    document.addEventListener('hidden.bs.modal', function () {
        document.body.style.paddingRight = '0px';
    });

    document.addEventListener('show.bs.modal', function (e) {
        if (e.target && e.target.parentElement !== document.body) {
            document.body.appendChild(e.target);
        }
    });

    // 1. Sticky Navbar Shadow Enhancement on Scroll
    const navbar = document.querySelector('.giveaid-navbar');
    window.addEventListener('scroll', function () {
        if (window.scrollY > 30) {
            navbar.classList.add('scrolled');
        } else {
            navbar.classList.remove('scrolled');
        }
    });

    // 2. Donation Preset Amount Selector
    const amountButtons = document.querySelectorAll('.btn-preset-amount');
    const customAmountInput = document.getElementById('donationAmountInput');

    if (amountButtons.length > 0 && customAmountInput) {
        amountButtons.forEach(btn => {
            btn.addEventListener('click', function () {
                amountButtons.forEach(b => b.classList.remove('active', 'btn-orange'));
                amountButtons.forEach(b => b.classList.add('btn-outline-navy'));
                
                this.classList.remove('btn-outline-navy');
                this.classList.add('active', 'btn-orange');
                
                const val = this.getAttribute('data-amount');
                customAmountInput.value = val;
            });
        });

        customAmountInput.addEventListener('input', function () {
            amountButtons.forEach(b => {
                if (b.getAttribute('data-amount') === customAmountInput.value) {
                    b.classList.remove('btn-outline-navy');
                    b.classList.add('active', 'btn-orange');
                } else {
                    b.classList.remove('active', 'btn-orange');
                    b.classList.add('btn-outline-navy');
                }
            });
        });
    }

    // 3. Card Number Auto-formatting (4-4-4-4)
    const cardNumberInput = document.getElementById('cardNumberInput');
    if (cardNumberInput) {
        cardNumberInput.addEventListener('input', function (e) {
            let val = e.target.value.replace(/\D/g, '');
            val = val.substring(0, 16);
            let formatted = val.match(/.{1,4}/g)?.join(' ') || val;
            e.target.value = formatted;
        });
    }

    // 4. Expiry Date Auto-formatting (MM/YY)
    const expiryInput = document.getElementById('cardExpiryInput');
    if (expiryInput) {
        expiryInput.addEventListener('input', function (e) {
            let val = e.target.value.replace(/\D/g, '');
            if (val.length >= 2) {
                let month = val.substring(0, 2);
                let year = val.substring(2, 4);
                e.target.value = month + (val.length > 2 ? '/' + year : '');
            } else {
                e.target.value = val;
            }
        });
    }

    // 5. CVV 3-4 Digits restriction
    const cvvInput = document.getElementById('cardCvvInput');
    if (cvvInput) {
        cvvInput.addEventListener('input', function (e) {
            e.target.value = e.target.value.replace(/\D/g, '').substring(0, 4);
        });
    }

    // 6. Interactive Gallery Lightbox with Prev/Next Carousel & Keyboard Navigation
    const galleryCards = document.querySelectorAll('.gallery-card');
    const lightboxModalEl = document.getElementById('galleryLightboxModal');

    // Move modal directly to <body> so it can NEVER be trapped behind parent transforms or backdrops
    if (lightboxModalEl && document.body && lightboxModalEl.parentElement !== document.body) {
        document.body.appendChild(lightboxModalEl);
    }

    if (galleryCards.length > 0 && lightboxModalEl) {
        const lightboxModal = bootstrap.Modal.getOrCreateInstance(lightboxModalEl);
        const lightboxImage = document.getElementById('lightboxImage');
        const lightboxTitle = document.getElementById('lightboxTitle');
        const lightboxDesc = document.getElementById('lightboxDesc');
        const lightboxCategory = document.getElementById('lightboxCategory');
        const lightboxCounter = document.getElementById('lightboxCounter');
        const prevBtn = document.getElementById('lightboxPrevBtn');
        const nextBtn = document.getElementById('lightboxNextBtn');

        let currentIndex = 0;
        const items = [];

        galleryCards.forEach((card, idx) => {
            const src = card.getAttribute('data-img-src') || card.querySelector('img')?.src || '';
            const title = card.getAttribute('data-img-title') || card.querySelector('h6')?.textContent?.trim() || '';
            const desc = card.getAttribute('data-img-desc') || '';
            const cat = card.getAttribute('data-img-cat') || card.querySelector('.badge')?.textContent?.trim() || 'Photo';

            items.push({ src, title, desc, cat });

            card.addEventListener('click', function () {
                currentIndex = idx;
                renderLightbox(currentIndex);
                lightboxModal.show();
            });
        });

        function renderLightbox(index) {
            if (index < 0 || index >= items.length) return;
            const item = items[index];

            if (lightboxImage) {
                lightboxImage.style.opacity = '0.3';
                lightboxImage.src = item.src;
                lightboxImage.onload = () => { lightboxImage.style.opacity = '1'; };
            }
            if (lightboxTitle) lightboxTitle.textContent = item.title;
            if (lightboxDesc) {
                lightboxDesc.textContent = item.desc;
                lightboxDesc.style.display = item.desc ? 'block' : 'none';
            }
            if (lightboxCategory) lightboxCategory.textContent = item.cat;
            if (lightboxCounter) lightboxCounter.textContent = `${index + 1} / ${items.length}`;

            if (prevBtn) prevBtn.style.display = items.length > 1 ? 'flex' : 'none';
            if (nextBtn) nextBtn.style.display = items.length > 1 ? 'flex' : 'none';
        }

        if (prevBtn) {
            prevBtn.addEventListener('click', function (e) {
                e.stopPropagation();
                currentIndex = (currentIndex - 1 + items.length) % items.length;
                renderLightbox(currentIndex);
            });
        }

        if (nextBtn) {
            nextBtn.addEventListener('click', function (e) {
                e.stopPropagation();
                currentIndex = (currentIndex + 1) % items.length;
                renderLightbox(currentIndex);
            });
        }

        // Keyboard arrow navigation
        document.addEventListener('keydown', function (e) {
            if (!lightboxModalEl.classList.contains('show')) return;
            if (e.key === 'ArrowLeft') {
                currentIndex = (currentIndex - 1 + items.length) % items.length;
                renderLightbox(currentIndex);
            } else if (e.key === 'ArrowRight') {
                currentIndex = (currentIndex + 1) % items.length;
                renderLightbox(currentIndex);
            }
        });
    }

    // 7. Auto-dismiss alerts (Logout alert after 2 seconds, other alerts after 4 seconds)
    const logoutAlert = document.getElementById('logoutAlert');
    if (logoutAlert) {
        setTimeout(() => {
            if (window.bootstrap && bootstrap.Alert) {
                bootstrap.Alert.getOrCreateInstance(logoutAlert).close();
            } else {
                logoutAlert.remove();
            }
        }, 2000);
    }

    const otherAlerts = document.querySelectorAll('.alert-dismissible:not(#logoutAlert)');
    otherAlerts.forEach(alert => {
        setTimeout(() => {
            if (window.bootstrap && bootstrap.Alert) {
                bootstrap.Alert.getOrCreateInstance(alert).close();
            }
        }, 4000);
    });

    // 8. Animation Engine (Public Pages Only, 0 in Admin)
    const isAdmin = document.body.classList.contains('admin-body') ||
                    window.location.pathname.toLowerCase().startsWith('/admin') ||
                    document.querySelector('.admin-topbar, .admin-shell') !== null;

    if (isAdmin) {
        // ADMIN PANEL: Strictly disable any animations and remove AOS attributes
        document.querySelectorAll('[data-aos]').forEach(el => {
            el.removeAttribute('data-aos');
            el.removeAttribute('data-aos-delay');
            el.removeAttribute('data-aos-duration');
        });
    } else if (typeof AOS !== 'undefined') {
        // PUBLIC SITE: Auto-decorate sections & cards with data-aos
        try {
            // 1. Hero / Header Banners
            document.querySelectorAll('.giveaid-hero, .about-hero, main > div:first-child[class*="py-"], .page-header').forEach(hero => {
                if (!hero.hasAttribute('data-aos')) {
                    hero.setAttribute('data-aos', 'fade-down');
                }
                hero.setAttribute('data-aos-once', 'true');
            });

            // 2. Sections / Containers
            document.querySelectorAll('section, .section-padding, .container.py-5, .container.my-5').forEach(sec => {
                if (!sec.hasAttribute('data-aos') && !sec.querySelector('[data-aos]')) {
                    sec.setAttribute('data-aos', 'fade-up');
                }
                if (sec.hasAttribute('data-aos')) {
                    sec.setAttribute('data-aos-once', 'true');
                }
            });

            // 3. Section Titles & Tags
            document.querySelectorAll('.section-tag, .hero-tag, .section-title, .section-sub').forEach(heading => {
                if (!heading.hasAttribute('data-aos')) {
                    heading.setAttribute('data-aos', 'fade-up');
                }
                heading.setAttribute('data-aos-once', 'true');
            });

            // 4. Cards in rows & grids (with gentle staggered delay)
            document.querySelectorAll('.row').forEach(row => {
                const cards = row.querySelectorAll(':scope > div > .giveaid-card, :scope > div > .card, :scope > div > .action-card, :scope > div > .programme-card, :scope > div > .gallery-card, :scope > div > .team-card');
                cards.forEach((card, idx) => {
                    if (!card.hasAttribute('data-aos')) {
                        card.setAttribute('data-aos', 'fade-up');
                        card.setAttribute('data-aos-delay', String((idx % 4) * 80));
                    }
                    card.setAttribute('data-aos-once', 'true');
                });
            });

            // 5. Sidebars
            document.querySelectorAll('.sticky-sidebar, .about-sidebar').forEach(sidebar => {
                if (!sidebar.hasAttribute('data-aos')) {
                    sidebar.setAttribute('data-aos', 'fade-left');
                }
                sidebar.setAttribute('data-aos-once', 'true');
            });

            // 6. About content or single main article cards
            document.querySelectorAll('.about-content-card, .giveaid-card.p-4:not(.row *)').forEach(card => {
                if (!card.hasAttribute('data-aos')) {
                    card.setAttribute('data-aos', 'fade-up');
                }
                card.setAttribute('data-aos-once', 'true');
            });

            // Ensure all elements with data-aos have data-aos-once="true"
            document.querySelectorAll('[data-aos]').forEach(el => {
                el.setAttribute('data-aos-once', 'true');
            });

            // 7. Initialize AOS with fluid 750ms silky smooth easing
            AOS.init({
                duration: 750,
                easing: 'ease-out-cubic',
                once: true,    // Animate once permanently per element
                mirror: false,  // Do not revert when scrolling past
                offset: 35,    // Trigger naturally as soon as it enters view
                delay: 0,
                disableMutationObserver: true // Avoid unnecessary DOM tracking
            });

            // Handle browser back/forward cache (bfcache) navigation
            window.addEventListener('pageshow', function (e) {
                if (e.persisted) {
                    AOS.refreshHard();
                }
            });
        } catch (err) {
            console.warn('AOS init warning:', err);
        }
    }

    // ==========================================================================
    // 8. Natural Smooth In-Page Anchor Navigation
    // ==========================================================================
    document.querySelectorAll('a[href^="#"]:not([href="#"]):not([data-bs-toggle])').forEach(link => {
        link.addEventListener('click', function (e) {
            const targetId = this.getAttribute('href');
            if (targetId && targetId.length > 1) {
                const targetEl = document.querySelector(targetId);
                if (targetEl) {
                    e.preventDefault();
                    const topOffset = targetEl.getBoundingClientRect().top + window.pageYOffset - 80;
                    window.scrollTo({
                        top: topOffset,
                        behavior: 'smooth'
                    });
                }
            }
        });
    });

    // ==========================================================================
    // 9. Auto-dismiss Donation Login Alert after exactly 3 seconds
    // ==========================================================================
    const donationLoginAlert = document.getElementById('donationLoginAlert');
    if (donationLoginAlert) {
        setTimeout(() => {
            donationLoginAlert.style.transition = 'opacity 0.5s ease, max-height 0.5s ease, margin 0.5s ease, padding 0.5s ease';
            donationLoginAlert.style.opacity = '0';
            setTimeout(() => {
                if (donationLoginAlert.parentNode) {
                    donationLoginAlert.remove();
                }
            }, 500);
        }, 3000);
    }

    // ==========================================================================
    // 10. Strict Name Validation: Only Letters and Spaces Allowed
    // ==========================================================================
    function initNameValidation() {
        const nameInputs = document.querySelectorAll('[data-rule="letters-only"], input[name="FullName"], input[name="DonorName"], input[name="CardHolderName"], input[name="Profession"]');
        nameInputs.forEach(input => {
            if (input.dataset.nameRuleBound) return;
            input.dataset.nameRuleBound = "true";

            input.addEventListener('keydown', function (e) {
                const navKeys = ['Backspace', 'Delete', 'Tab', 'ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End'];
                if (navKeys.includes(e.key) || e.ctrlKey || e.metaKey) {
                    return;
                }
                // Disallow numbers and special characters
                if (!/^[a-zA-Z\s'-]$/.test(e.key)) {
                    e.preventDefault();
                }
            });

            input.addEventListener('input', function () {
                let clean = this.value.replace(/[^a-zA-Z\s'-]/g, '');
                clean = clean.replace(/^\s+/, ''); // No leading space
                clean = clean.replace(/\s{2,}/g, ' '); // No double space
                if (this.value !== clean) {
                    this.value = clean;
                }
            });
        });
    }
    initNameValidation();

    // ==========================================================================
    // 11. Strict Number Validation: Only Digits 0-9 Allowed
    // ==========================================================================
    function initNumberValidation() {
        const numberInputs = document.querySelectorAll('[data-rule="numbers-only"]');
        numberInputs.forEach(input => {
            if (input.dataset.numberRuleBound) return;
            input.dataset.numberRuleBound = "true";

            input.addEventListener('keydown', function (e) {
                const navKeys = ['Backspace', 'Delete', 'Tab', 'ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End'];
                if (navKeys.includes(e.key) || e.ctrlKey || e.metaKey) {
                    return;
                }
                if (!/^[0-9]$/.test(e.key)) {
                    e.preventDefault();
                }
            });

            input.addEventListener('input', function () {
                let clean = this.value.replace(/\D/g, '');
                const maxLen = this.getAttribute('maxlength');
                if (maxLen && clean.length > parseInt(maxLen)) {
                    clean = clean.substring(0, parseInt(maxLen));
                }
                if (this.value !== clean) {
                    this.value = clean;
                }
            });
        });
    }
    initNumberValidation();

    // ==========================================================================
    // 12. International Country Phone Picker with Flag & Digit Restriction
    // ==========================================================================
    const COUNTRY_LIST = [
        { name: "Pakistan", code: "+92", flag: "🇵🇰", digits: 10, placeholder: "300 1234567" },
        { name: "Saudi Arabia", code: "+966", flag: "🇸🇦", digits: 9, placeholder: "50 123 4567" },
        { name: "United Arab Emirates", code: "+971", flag: "🇦🇪", digits: 9, placeholder: "50 123 4567" },
        { name: "United States", code: "+1", flag: "🇺🇸", digits: 10, placeholder: "202 555 0199" },
        { name: "United Kingdom", code: "+44", flag: "🇬🇧", digits: 10, placeholder: "7911 123456" },
        { name: "Canada", code: "+1", flag: "🇨🇦", digits: 10, placeholder: "416 555 0199" },
        { name: "India", code: "+91", flag: "🇮🇳", digits: 10, placeholder: "98765 43210" },
        { name: "Qatar", code: "+974", flag: "🇶🇦", digits: 8, placeholder: "3312 3456" },
        { name: "Oman", code: "+968", flag: "🇴🇲", digits: 8, placeholder: "9123 4567" },
        { name: "Kuwait", code: "+965", flag: "🇰🇼", digits: 8, placeholder: "5123 4567" },
        { name: "Bahrain", code: "+973", flag: "🇧🇭", digits: 8, placeholder: "3912 3456" },
        { name: "Turkey", code: "+90", flag: "🇹🇷", digits: 10, placeholder: "532 123 4567" },
        { name: "Australia", code: "+61", flag: "🇦🇺", digits: 9, placeholder: "412 345 678" },
        { name: "Germany", code: "+49", flag: "🇩🇪", digits: 10, placeholder: "151 23456789" },
        { name: "Malaysia", code: "+60", flag: "🇲🇾", digits: 9, placeholder: "12 345 6789" },
        { name: "Bangladesh", code: "+880", flag: "🇧🇩", digits: 10, placeholder: "1712 345678" },
        { name: "Egypt", code: "+20", flag: "🇪🇬", digits: 10, placeholder: "10 1234 5678" },
        { name: "South Africa", code: "+27", flag: "🇿🇦", digits: 9, placeholder: "71 234 5678" },
        { name: "France", code: "+33", flag: "🇫🇷", digits: 9, placeholder: "6 12 34 56 78" },
        { name: "Italy", code: "+39", flag: "🇮🇹", digits: 10, placeholder: "312 345 6789" },
        { name: "Spain", code: "+34", flag: "🇪🇸", digits: 9, placeholder: "612 345 678" },
        { name: "China", code: "+86", flag: "🇨🇳", digits: 11, placeholder: "138 0013 8000" },
        { name: "Japan", code: "+81", flag: "🇯🇵", digits: 10, placeholder: "90 1234 5678" },
        { name: "Singapore", code: "+65", flag: "🇸🇬", digits: 8, placeholder: "8123 4567" },
        { name: "Indonesia", code: "+62", flag: "🇮🇩", digits: 10, placeholder: "812 3456 7890" },
        { name: "New Zealand", code: "+64", flag: "🇳🇿", digits: 9, placeholder: "21 123 4567" },
        { name: "Ireland", code: "+353", flag: "🇮🇪", digits: 9, placeholder: "83 123 4567" },
        { name: "Netherlands", code: "+31", flag: "🇳🇱", digits: 9, placeholder: "6 12345678" },
        { name: "Sweden", code: "+46", flag: "🇸🇪", digits: 9, placeholder: "70 123 4567" },
        { name: "Switzerland", code: "+41", flag: "🇨🇭", digits: 9, placeholder: "78 123 4567" },
        { name: "Norway", code: "+47", flag: "🇳🇴", digits: 8, placeholder: "412 34 567" },
        { name: "Denmark", code: "+45", flag: "🇩🇰", digits: 8, placeholder: "20 12 34 56" },
        { name: "Finland", code: "+358", flag: "🇫🇮", digits: 9, placeholder: "40 123 4567" },
        { name: "Belgium", code: "+32", flag: "🇧🇪", digits: 9, placeholder: "470 12 34 56" },
        { name: "Austria", code: "+43", flag: "🇦🇹", digits: 10, placeholder: "664 1234567" },
        { name: "Poland", code: "+48", flag: "🇵🇱", digits: 9, placeholder: "512 345 678" },
        { name: "Brazil", code: "+55", flag: "🇧🇷", digits: 11, placeholder: "11 91234 5678" },
        { name: "Mexico", code: "+52", flag: "🇲🇽", digits: 10, placeholder: "55 1234 5678" },
        { name: "Nigeria", code: "+234", flag: "🇳🇬", digits: 10, placeholder: "802 123 4567" },
        { name: "Philippines", code: "+63", flag: "🇵🇭", digits: 10, placeholder: "917 123 4567" },
        { name: "Sri Lanka", code: "+94", flag: "🇱🇰", digits: 9, placeholder: "71 234 5678" },
        { name: "Nepal", code: "+977", flag: "🇳🇵", digits: 10, placeholder: "984 1234567" },
        { name: "Afghanistan", code: "+93", flag: "🇦🇫", digits: 9, placeholder: "70 123 4567" },
        { name: "Iran", code: "+98", flag: "🇮🇷", digits: 10, placeholder: "912 123 4567" },
        { name: "Iraq", code: "+964", flag: "🇮🇶", digits: 10, placeholder: "770 123 4567" },
        { name: "Jordan", code: "+962", flag: "🇯🇴", digits: 9, placeholder: "7 9012 3456" },
        { name: "Lebanon", code: "+961", flag: "🇱🇧", digits: 8, placeholder: "70 123 456" }
    ];

    function initIntlPhonePickers() {
        document.querySelectorAll('.intl-phone-wrapper').forEach(wrapper => {
            if (wrapper.dataset.phoneInitialized) return;
            wrapper.dataset.phoneInitialized = "true";

            const btn = wrapper.querySelector('.country-picker-btn');
            const menu = wrapper.querySelector('.country-dropdown-menu');
            const searchInput = wrapper.querySelector('.country-search-input');
            const phoneInput = wrapper.querySelector('.country-phone-input');
            const hiddenInput = wrapper.querySelector('.phone-actual-input');
            const helperText = wrapper.querySelector('.phone-helper-text');
            const counter = wrapper.querySelector('.phone-digit-counter');
            const listContainer = wrapper.querySelector('.country-items-list');

            if (!btn || !menu || !phoneInput || !hiddenInput) return;

            // Render country list
            function renderCountries(filter = '') {
                if (!listContainer) return;
                listContainer.innerHTML = '';
                const query = filter.toLowerCase().trim();

                const filtered = COUNTRY_LIST.filter(c => 
                    c.name.toLowerCase().includes(query) ||
                    c.code.includes(query)
                );

                if (filtered.length === 0) {
                    listContainer.innerHTML = '<li class="text-muted small text-center py-2">No country found</li>';
                    return;
                }

                filtered.forEach(c => {
                    const li = document.createElement('li');
                    li.className = 'country-item';
                    li.innerHTML = `
                        <div class="country-item-left">
                            <span class="country-flag-icon">${c.flag}</span>
                            <span class="country-name">${c.name}</span>
                        </div>
                        <span class="country-code-badge">${c.code}</span>
                    `;
                    li.addEventListener('click', function (e) {
                        e.preventDefault();
                        selectCountry(c);
                        const bsDropdown = bootstrap.Dropdown.getInstance(btn);
                        if (bsDropdown) bsDropdown.hide();
                    });
                    listContainer.appendChild(li);
                });
            }

            renderCountries();

            if (searchInput) {
                searchInput.addEventListener('input', function () {
                    renderCountries(this.value);
                });
                searchInput.addEventListener('click', function (e) {
                    e.stopPropagation();
                });
            }

            let currentCountry = COUNTRY_LIST[0]; // Default: Pakistan (+92)

            function selectCountry(c) {
                currentCountry = c;
                btn.querySelector('.country-flag-icon').textContent = c.flag;
                btn.querySelector('.country-dial-code').textContent = c.code;

                phoneInput.setAttribute('maxlength', c.digits);
                phoneInput.setAttribute('placeholder', c.placeholder);

                // Re-validate existing digits
                let digits = phoneInput.value.replace(/\D/g, '');
                if (digits.length > c.digits) {
                    digits = digits.substring(0, c.digits);
                    phoneInput.value = digits;
                }

                updateCounterAndHidden(digits);
            }

            function updateCounterAndHidden(digits) {
                if (helperText) {
                    helperText.textContent = `Max ${currentCountry.digits} digits for ${currentCountry.name} (${currentCountry.code})`;
                }
                if (counter) {
                    counter.textContent = `${digits.length}/${currentCountry.digits}`;
                    if (digits.length === currentCountry.digits) {
                        counter.classList.add('valid');
                    } else {
                        counter.classList.remove('valid');
                    }
                }

                if (digits.length > 0) {
                    hiddenInput.value = `${currentCountry.code} ${digits}`;
                } else {
                    hiddenInput.value = '';
                }
            }

            // Check if there is an initial value in hiddenInput
            const initialVal = hiddenInput.value.trim();
            if (initialVal) {
                const matchedCountry = COUNTRY_LIST.find(c => initialVal.startsWith(c.code));
                if (matchedCountry) {
                    selectCountry(matchedCountry);
                    const rawDigits = initialVal.replace(matchedCountry.code, '').replace(/\D/g, '');
                    phoneInput.value = rawDigits.substring(0, matchedCountry.digits);
                    updateCounterAndHidden(phoneInput.value);
                } else {
                    selectCountry(COUNTRY_LIST[0]);
                    const rawDigits = initialVal.replace(/\D/g, '');
                    phoneInput.value = rawDigits.substring(0, COUNTRY_LIST[0].digits);
                    updateCounterAndHidden(phoneInput.value);
                }
            } else {
                selectCountry(COUNTRY_LIST[0]);
            }

            // Real-time input and keydown on phoneInput
            phoneInput.addEventListener('keydown', function (e) {
                const navKeys = ['Backspace', 'Delete', 'Tab', 'ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End'];
                if (navKeys.includes(e.key) || e.ctrlKey || e.metaKey) {
                    return;
                }
                // Only allow numbers
                if (!/^[0-9]$/.test(e.key)) {
                    e.preventDefault();
                    return;
                }
                // Prevent typing more than currentCountry.digits
                let digits = this.value.replace(/\D/g, '');
                if (digits.length >= currentCountry.digits && this.selectionStart === this.selectionEnd) {
                    e.preventDefault();
                }
            });

            phoneInput.addEventListener('input', function () {
                let digits = this.value.replace(/\D/g, '');
                if (digits.length > currentCountry.digits) {
                    digits = digits.substring(0, currentCountry.digits);
                }
                if (this.value !== digits) {
                    this.value = digits;
                }
                updateCounterAndHidden(digits);
            });
        });
    }
    initIntlPhonePickers();

    // Footer Newsletter Interactive Quick Feedback & AJAX
    function initFooterNewsletter() {
        const form = document.getElementById('footerNewsletterForm');
        if (!form) return;

        const emailInput = document.getElementById('newsletterEmail');
        const submitBtn = document.getElementById('newsletterSubmitBtn');
        const btnIcon = document.getElementById('newsletterBtnIcon');
        const feedback = document.getElementById('newsletterFeedback');

        form.addEventListener('submit', async function (e) {
            e.preventDefault();

            const emailVal = (emailInput.value || '').trim();
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

            if (!emailVal || !emailRegex.test(emailVal)) {
                feedback.style.display = 'block';
                feedback.innerHTML = '<span class="text-warning fw-semibold"><i class="bi bi-exclamation-triangle-fill me-1"></i> Please enter a valid email address.</span>';
                emailInput.focus();
                return;
            }

            // Loading state
            submitBtn.disabled = true;
            btnIcon.className = 'spinner-border spinner-border-sm';
            feedback.style.display = 'none';

            try {
                const formData = new FormData(form);
                const response = await fetch(form.action, {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });

                const data = await response.json();

                if (data.success) {
                    feedback.style.display = 'block';
                    feedback.innerHTML = '<span class="text-success fw-semibold"><i class="bi bi-check-circle-fill me-1"></i> ' + data.message + '</span>';
                    btnIcon.className = 'bi bi-check-lg text-white';
                    emailInput.value = '';
                    setTimeout(function () {
                        btnIcon.className = 'bi bi-send-fill';
                        submitBtn.disabled = false;
                    }, 4000);
                } else {
                    feedback.style.display = 'block';
                    feedback.innerHTML = '<span class="text-warning fw-semibold"><i class="bi bi-exclamation-circle-fill me-1"></i> ' + (data.message || 'Subscription failed. Please try again.') + '</span>';
                    btnIcon.className = 'bi bi-send-fill';
                    submitBtn.disabled = false;
                }
            } catch (err) {
                feedback.style.display = 'block';
                feedback.innerHTML = '<span class="text-danger fw-semibold"><i class="bi bi-x-circle-fill me-1"></i> Connection error. Please try again.</span>';
                btnIcon.className = 'bi bi-send-fill';
                submitBtn.disabled = false;
            }
        });
    }
    initFooterNewsletter();

    // ============================================================
    // Progressive Web App (PWA) & Native Desktop App Installation
    // ============================================================
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/sw.js')
                .then(function (reg) {
                    console.log('NGEOS Desktop App ServiceWorker active:', reg.scope);
                })
                .catch(function (err) {
                    console.warn('NGEOS ServiceWorker registration notice:', err);
                });
        });
    }

    
    // Desktop App state handling

    let pwaInstallPrompt = null;
    const btnInstallPwa = document.getElementById('btnInstallPwa');
    const footerInstallPwa = document.getElementById('footerInstallPwa');
    const footerDesktopSection = document.getElementById('footerDesktopSection');
    const btnFooterDownloadDesktop = document.getElementById('btnFooterDownloadDesktop');
    const btnModalInstallYes = document.getElementById('btnModalInstallYes');
    const btnModalInstallNo = document.getElementById('btnModalInstallNo');
    const desktopModalEl = document.getElementById('ngeosDesktopAppModal');

    function hideDesktopInstallUI() {
        if (footerDesktopSection) {
            footerDesktopSection.style.display = 'none';
        }
        if (footerInstallPwa) {
            footerInstallPwa.style.display = 'none';
        }
        if (btnInstallPwa) {
            btnInstallPwa.classList.add('d-none');
            btnInstallPwa.classList.remove('d-inline-flex');
        }
    }

    // Check if already installed, downloaded, or running inside standalone desktop app window
    const isAppWindow = window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone;

    // Only hide if already inside the standalone Desktop App window
    if (isAppWindow) {
        hideDesktopInstallUI();
    }

    window.addEventListener('beforeinstallprompt', function (e) {
        e.preventDefault();
        pwaInstallPrompt = e;
        if (!isAppWindow && btnInstallPwa) {
            btnInstallPwa.classList.remove('d-none');
            btnInstallPwa.classList.add('d-inline-flex');
        }
    });

    function downloadDesktopExe() {
        const link = document.createElement('a');
        link.href = '/downloads/NGEOS-Setup.exe';
        link.download = 'NGEOS-Setup.exe';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }

    async function handleDesktopInstallOrDownload() {
        sessionStorage.setItem('ngeos_desktop_modal_shown', 'true');

        if (pwaInstallPrompt) {
            pwaInstallPrompt.prompt();
            const choiceResult = await pwaInstallPrompt.userChoice;
            console.log('NGEOS install outcome:', choiceResult.outcome);
            pwaInstallPrompt = null;
            if (choiceResult.outcome === 'accepted') {
                return; // User installed native desktop app directly with icon!
            }
        }
        // Fallback: Download standalone NGEOS-Setup.exe directly (NO ZIP!)
        downloadDesktopExe();
    }

    if (btnInstallPwa) {
        btnInstallPwa.addEventListener('click', handleDesktopInstallOrDownload);
    }

    if (footerInstallPwa) {
        footerInstallPwa.addEventListener('click', handleDesktopInstallOrDownload);
    }

    if (btnFooterDownloadDesktop) {
        btnFooterDownloadDesktop.addEventListener('click', handleDesktopInstallOrDownload);
    }

    function dismissDesktopModalPermanently() {
        sessionStorage.setItem('ngeos_desktop_modal_shown', 'true');
        localStorage.setItem('ngeos_desktop_modal_dismissed', 'true');
    }

    if (desktopModalEl) {
        desktopModalEl.addEventListener('hidden.bs.modal', function () {
            sessionStorage.setItem('ngeos_desktop_modal_shown', 'true');
        });
    }

    if (btnModalInstallYes) {
        btnModalInstallYes.addEventListener('click', function () {
            dismissDesktopModalPermanently();
            handleDesktopInstallOrDownload();
            if (desktopModalEl && typeof bootstrap !== 'undefined') {
                const inst = bootstrap.Modal.getInstance(desktopModalEl);
                if (inst) inst.hide();
            }
        });
    }

    if (btnModalInstallNo) {
        btnModalInstallNo.addEventListener('click', function () {
            dismissDesktopModalPermanently();
        });
    }

    // Show Desktop install notification exactly ONCE per user/browser (never repeats on other pages)
    const hasAlreadySeenDesktopModal = localStorage.getItem('ngeos_desktop_modal_shown') === 'true';

    if (desktopModalEl && !isAppWindow && !hasAlreadySeenDesktopModal) {
        // Mark as seen immediately so navigating to any other page or refreshing will NEVER show it again
        localStorage.setItem('ngeos_desktop_modal_shown', 'true');

        setTimeout(function () {
            try {
                if (typeof bootstrap !== 'undefined') {
                    const modal = new bootstrap.Modal(desktopModalEl);
                    modal.show();
                }
            } catch (err) {
                console.warn('One-time modal show error:', err);
            }
        }, 1800);
    }

    window.addEventListener('appinstalled', function () {
        console.log('NGEOS Desktop App installed successfully');
        localStorage.setItem('ngeos_desktop_installed', 'true');
        hideDesktopInstallUI();
    });



    // Overscroll prevention is handled natively via CSS (overscroll-behavior: none).
    // Active wheel/touchmove listeners with preventDefault() are removed to guarantee smooth mouse wheel scrolling.
    if (document.body.classList.contains('admin-body') || window.location.pathname.toLowerCase().includes('/admin')) {
        document.documentElement.style.setProperty('overflow-y', 'auto', 'important');
        document.documentElement.style.setProperty('overflow-x', 'hidden', 'important');
        document.body.style.setProperty('overflow-y', 'visible', 'important');
        document.body.style.setProperty('overflow-x', 'hidden', 'important');
        const adminMain = document.querySelector('.admin-main');
        if (adminMain) {
            adminMain.style.setProperty('overflow-y', 'visible', 'important');
        }
        const adminShell = document.querySelector('.admin-shell');
        if (adminShell) {
            adminShell.style.setProperty('overflow-y', 'visible', 'important');
        }
    }

    // Ensure English default and clean up any past language overrides
    try {
        localStorage.removeItem('giveaid_site_lang');
        localStorage.removeItem('giveaid_zakat_lang');
        document.documentElement.lang = 'en';
        document.documentElement.dir = 'ltr';
        document.body.classList.remove('global-urdu-mode');
    } catch (e) { }
});



