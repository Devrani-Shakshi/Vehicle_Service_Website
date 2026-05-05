

// --- Toast Notifications ---
function showToast(message, type = 'success') {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = `toast-notification ${type === 'error' ? 'error' : ''}`;
    toast.innerHTML = `
        <span class="toast-icon">${type === 'error' ? '<i class="fas fa-exclamation-circle" style="color:var(--danger);"></i>' : '<i class="fas fa-check-circle" style="color:var(--success);"></i>'}</span>
        <span class="toast-message">${message}</span>
        <button class="toast-close" onclick="this.parentElement.remove()"><i class="fas fa-times"></i></button>
    `;
    container.appendChild(toast);

    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateX(100px)';
        toast.style.transition = 'all 0.3s ease';
        setTimeout(() => toast.remove(), 300);
    }, 4000);
}

// --- Loading Spinner ---
function showSpinner() {
    const spinner = document.getElementById('loadingSpinner');
    if (spinner) spinner.classList.add('active');
}

function hideSpinner() {
    const spinner = document.getElementById('loadingSpinner');
    if (spinner) spinner.classList.remove('active');
}

// --- Notification System (SignalR) ---
let notificationConnection = null;

function initSignalR() {
    notificationConnection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/notification")
        .withAutomaticReconnect()
        .build();

    notificationConnection.on("ReceiveNotification", function (notification) {
        showToast(notification.message, 'success');
        updateNotificationBell();
        addNotificationToDropdown(notification);

        // Instant Data Reflection
        if (notification.type === "RequestUpdate") {
            const statusEl = document.getElementById(`request-status-${notification.id}`);
            if (statusEl) {
                statusEl.textContent = notification.status;
                statusEl.className = `badge-custom badge-${notification.status.toLowerCase()}`;

                // Add a highlight effect
                statusEl.parentElement.parentElement.classList.add('highlight-update');
                setTimeout(() => statusEl.parentElement.parentElement.classList.remove('highlight-update'), 3000);
            }
            if (notification.providerName) {
                const providerEl = document.getElementById(`request-provider-${notification.id}`);
                if (providerEl) providerEl.textContent = notification.providerName;
            }
        }
    });

    notificationConnection.start().catch(function (err) {
        console.log("SignalR connection error:", err);
    });
}

function toggleNotifications() {
    const dropdown = document.getElementById('notifDropdown');
    if (dropdown) {
        dropdown.classList.toggle('show');
        if (dropdown.classList.contains('show')) {
            loadNotifications();
        }
    }
}

function toggleUserProfile() {
    const dropdown = document.getElementById('userDropdown');
    const notifDropdown = document.getElementById('notifDropdown');
    if (dropdown) {
        if (notifDropdown && notifDropdown.classList.contains('show')) {
            notifDropdown.classList.remove('show');
        }
        dropdown.classList.toggle('show');
    }
}

// Close dropdowns when clicking outside
document.addEventListener('click', function (e) {
    const notifDropdown = document.getElementById('notifDropdown');
    const notifBell = document.getElementById('notifBell');
    if (notifDropdown && notifBell && !notifDropdown.contains(e.target) && !notifBell.contains(e.target)) {
        notifDropdown.classList.remove('show');
    }

    const userDropdown = document.getElementById('userDropdown');
    const userBtn = document.getElementById('userProfileBtn');
    if (userDropdown && userBtn && !userDropdown.contains(e.target) && !userBtn.contains(e.target)) {
        userDropdown.classList.remove('show');
    }
});

// --- Theme Toggle ---
function toggleTheme() {
    const currentTheme = document.documentElement.getAttribute('data-theme') || 'dark';
    const newTheme = currentTheme === 'dark' ? 'light' : 'dark';

    document.documentElement.setAttribute('data-theme', newTheme);
    localStorage.setItem('theme', newTheme);

    const themeIcon = document.getElementById('themeIcon');
    if (themeIcon) {
        themeIcon.className = newTheme === 'light' ? 'fas fa-sun' : 'fas fa-moon';
    }
}

async function loadNotifications() {
    try {
        const response = await fetch('/Notification/GetNotifications');
        const data = await response.json();

        const countEl = document.getElementById('notifCount');
        if (countEl) {
            countEl.textContent = data.unreadCount;
            countEl.style.display = data.unreadCount > 0 ? 'flex' : 'none';
        }

        const listEl = document.getElementById('notifList');
        if (listEl && data.notifications.length > 0) {
            listEl.innerHTML = data.notifications.map(n => `
                <div class="notification-item ${!n.isRead ? 'unread' : ''}" onclick="markNotifRead(${n.id}, '${n.link || ''}')">
                    <h6>${n.title}</h6>
                    <p>${n.message}</p>
                    <small>${new Date(n.createdAt).toLocaleDateString()}</small>
                </div>
            `).join('');
        } else if (listEl) {
            listEl.innerHTML = '<div class="empty-state" style="padding:1.5rem;"><p>No notifications</p></div>';
        }
    } catch (err) {
        console.log('Error loading notifications:', err);
    }
}

function updateNotificationBell() {
    loadNotifications();
}

function addNotificationToDropdown(notification) {
    const listEl = document.getElementById('notifList');
    if (!listEl) return;

    const emptyState = listEl.querySelector('.empty-state');
    if (emptyState) emptyState.remove();

    const item = document.createElement('div');
    item.className = 'notification-item unread';
    item.onclick = () => markNotifRead(notification.id, notification.link || '');
    item.innerHTML = `<h6>${notification.title}</h6><p>${notification.message}</p><small>Just now</small>`;
    listEl.insertBefore(item, listEl.firstChild);
}

async function markNotifRead(id, link) {
    try {
        await fetch(`/Notification/MarkAsRead?id=${id}`, { method: 'POST' });
        loadNotifications();
        if (link) window.location.href = link;
    } catch (err) {
        console.log('Error:', err);
    }
}

async function markAllRead() {
    try {
        await fetch('/Notification/MarkAllAsRead', { method: 'POST' });
        loadNotifications();
    } catch (err) {
        console.log('Error:', err);
    }
}

// --- Initialize ---
document.addEventListener('DOMContentLoaded', function () {
    // Try to connect SignalR if authenticated
    if (document.querySelector('.dashboard-container')) {
        initSignalR();
        loadNotifications();
    }

    // Animate elements on scroll
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.opacity = '1';
                entry.target.style.transform = 'translateY(0)';
            }
        });
    }, { threshold: 0.1 });

    document.querySelectorAll('.stat-card, .data-card, .product-card').forEach(el => {
        observer.observe(el);
    });

    // Initialize Bootstrap tooltips globally
    if (typeof bootstrap !== 'undefined') {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }

    // Apply saved language globally
    const savedLang = localStorage.getItem('language') || 'en';
    if (savedLang !== 'en') {
        applyGlobalLanguage(savedLang);
    }

    // Global SweetAlert2 Form Interceptor for 'onsubmit' confirms
    document.querySelectorAll('form').forEach(form => {
        const onsubmitAttr = form.getAttribute('onsubmit');
        if (onsubmitAttr && onsubmitAttr.includes('confirm(')) {
            // Extract message
            const match = onsubmitAttr.match(/confirm\(['"](.*?)['"]\)/);
            const msg = match ? match[1] : "Are you sure you want to proceed?";

            // Remove the inline handler to prevent dual-firing
            form.removeAttribute('onsubmit');

            form.addEventListener('submit', function (e) {
                e.preventDefault();
                if (typeof Swal !== 'undefined') {
                    Swal.fire({
                        title: 'Are you sure?',
                        text: msg,
                        icon: 'warning',
                        showCancelButton: true,
                        confirmButtonColor: '#ef4444',
                        cancelButtonColor: '#64748b',
                        confirmButtonText: 'Yes, proceed!'
                    }).then((result) => {
                        if (result.isConfirmed) {
                            form.submit();
                        }
                    });
                } else {
                    // Fallback if sweetalert fails to load
                    if (confirm(msg)) {
                        form.submit();
                    }
                }
            });
        }
    });
});

// --- Global Language System ---
const globalTranslations = {
    en: {
        // Sidebar
        "Overview": "Overview", "Dashboard": "Dashboard", "Services": "Services",
        "New Request": "New Request", "My Requests": "My Requests",
        "Book Appointment": "Book Appointment", "Appointments": "Appointments",
        "Shopping": "Shopping", "Shop": "Shop", "Cart": "Cart",
        "My Orders": "My Orders", "Account": "Account",
        "Payment History": "Payment History", "Feedback": "Feedback",
        "Policy": "Policy", "Settings": "Settings", "Logout": "Logout",
        "Edit Profile": "Edit Profile",
        // Common buttons
        "Submit": "Submit", "Save": "Save", "Cancel": "Cancel",
        "Delete": "Delete", "Edit": "Edit", "View": "View",
        "Search": "Search", "Save Changes": "Save Changes",
        "Loading...": "Loading...", "No data found": "No data found",
        "Submit Feedback": "Submit Feedback", "Book Appointment": "Book Appointment",
        "Submit Request": "Submit Request",
        // Modal
        "Confirm Logout": "Confirm Logout",
        "Are you sure you want to logout?": "Are you sure you want to logout?",
        // Notifications
        "Notifications": "Notifications", "Mark all read": "Mark all read",
        "No notifications": "No notifications",
        // Dashboard stats
        "Pending Requests": "Pending Requests", "Completed": "Completed",
        "Total Orders": "Total Orders", "Upcoming": "Upcoming",
        // Statuses
        "Pending": "Pending", "Accepted": "Accepted", "In Progress": "In Progress",
        "InProgress": "InProgress", "Rejected": "Rejected", "Cancelled": "Cancelled",
        "Requested": "Requested", "Scheduled": "Scheduled", "Confirmed": "Confirmed",
        // Quick actions
        "New Service Request": "New Service Request",
        "Browse Shop": "Browse Shop", "View Cart": "View Cart",
        "Send Feedback": "Send Feedback",
        // Page titles
        "Feedback & Suggestions": "Feedback & Suggestions",
        "New Service Request": "New Service Request",
        "My Appointments": "My Appointments",
        "All Service Requests": "All Service Requests",
        // Tables
        "Title": "Title", "Date": "Date", "Time": "Time",
        "Provider": "Provider", "Status": "Status",
        "Not assigned": "Not assigned", "Book New": "Book New",
        "Request": "Request", "Appointment": "Appointment",
        "Category": "Category", "Action": "Action",
        "Type": "Type", "User": "User",
        // Dashboard content
        "Welcome back,": "Welcome back,",
        "Here's an overview of your activity": "Here's an overview of your activity",
        "Quick Actions": "Quick Actions",
        "Recent Service Requests": "Recent Service Requests",
        "Upcoming Appointments": "Upcoming Appointments",
        "No requests yet": "No requests yet",
        "No appointments": "No appointments",
        // Form labels
        "Request Title": "Request Title", "Description": "Description",
        "Urgency Level": "Urgency Level", "Vehicle / Service Type": "Vehicle / Service Type",
        "Appointment Title": "Appointment Title", "Time Slot": "Time Slot",
        "Rating": "Rating", "Your Feedback": "Your Feedback",
        "Full Name": "Full Name", "Email": "Email", "Mobile": "Mobile",
        "State": "State", "Address": "Address",
        // Service Provider sidebar
        "Requests": "Requests", "My Requests": "My Requests",
        "Ratings & Reviews": "Ratings & Reviews",
        // Shopkeeper sidebar
        "Products": "Products", "My Products": "My Products",
        "Add Product": "Add Product", "Orders": "Orders",
        // Profile
        "Management": "Management", "Profile": "Profile"
    },
    hi: {
        // Sidebar
        "Overview": "अवलोकन", "Dashboard": "डैशबोर्ड", "Services": "सेवाएं",
        "New Request": "नया अनुरोध", "My Requests": "मेरे अनुरोध",
        "Book Appointment": "अपॉइंटमेंट बुक करें", "Appointments": "अपॉइंटमेंट्स",
        "Shopping": "खरीदारी", "Shop": "दुकान", "Cart": "कार्ट",
        "My Orders": "मेरे ऑर्डर", "Account": "खाता",
        "Payment History": "भुगतान इतिहास", "Feedback": "प्रतिक्रिया",
        "Policy": "नीति", "Settings": "सेटिंग्स", "Logout": "लॉगआउट",
        "Edit Profile": "प्रोफ़ाइल संपादित करें",
        // Common buttons
        "Submit": "जमा करें", "Save": "सहेजें", "Cancel": "रद्द करें",
        "Delete": "हटाएं", "Edit": "संपादित करें", "View": "देखें",
        "Search": "खोजें", "Save Changes": "परिवर्तन सहेजें",
        "Loading...": "लोड हो रहा है...", "No data found": "कोई डेटा नहीं मिला",
        "Submit Feedback": "प्रतिक्रिया भेजें", "Book Appointment": "अपॉइंटमेंट बुक करें",
        "Submit Request": "अनुरोध भेजें",
        // Modal
        "Confirm Logout": "लॉगआउट की पुष्टि करें",
        "Are you sure you want to logout?": "क्या आप वाकई लॉगआउट करना चाहते हैं?",
        // Notifications
        "Notifications": "सूचनाएं", "Mark all read": "सब पढ़ा हुआ",
        "No notifications": "कोई सूचना नहीं",
        // Dashboard stats
        "Pending Requests": "लंबित अनुरोध", "Completed": "पूर्ण",
        "Total Orders": "कुल ऑर्डर", "Upcoming": "आगामी",
        // Statuses
        "Pending": "लंबित", "Accepted": "स्वीकृत", "In Progress": "प्रगति में",
        "InProgress": "प्रगति में", "Rejected": "अस्वीकृत", "Cancelled": "रद्द",
        "Requested": "अनुरोधित", "Scheduled": "अनुसूचित", "Confirmed": "पुष्टि",
        // Quick actions
        "New Service Request": "नई सेवा अनुरोध",
        "Browse Shop": "दुकान देखें", "View Cart": "कार्ट देखें",
        "Send Feedback": "प्रतिक्रिया भेजें",
        // Page titles
        "Feedback & Suggestions": "प्रतिक्रिया और सुझाव",
        "New Service Request": "नई सेवा अनुरोध",
        "My Appointments": "मेरी अपॉइंटमेंट्स",
        "All Service Requests": "सभी सेवा अनुरोध",
        // Tables
        "Title": "शीर्षक", "Date": "तारीख", "Time": "समय",
        "Provider": "प्रदाता", "Status": "स्थिति",
        "Not assigned": "असाइन नहीं किया", "Book New": "नया बुक करें",
        "Request": "अनुरोध", "Appointment": "अपॉइंटमेंट",
        "Category": "श्रेणी", "Action": "कार्रवाई",
        "Type": "प्रकार", "User": "उपयोगकर्ता",
        // Dashboard content
        "Welcome back,": "वापसी पर स्वागत है,",
        "Here's an overview of your activity": "यहाँ आपकी गतिविधि का अवलोकन है",
        "Quick Actions": "त्वरित कार्य",
        "Recent Service Requests": "हाल की सेवा अनुरोध",
        "Upcoming Appointments": "आगामी अपॉइंटमेंट्स",
        "No requests yet": "अभी तक कोई अनुरोध नहीं",
        "No appointments": "कोई अपॉइंटमेंट नहीं",
        // Form labels
        "Request Title": "अनुरोध शीर्षक", "Description": "विवरण",
        "Urgency Level": "तात्कालिकता स्तर", "Vehicle / Service Type": "वाहन / सेवा प्रकार",
        "Appointment Title": "अपॉइंटमेंट शीर्षक", "Time Slot": "समय स्लॉट",
        "Rating": "रेटिंग", "Your Feedback": "आपकी प्रतिक्रिया",
        "Full Name": "पूरा नाम", "Email": "ईमेल", "Mobile": "मोबाइल",
        "State": "राज्य", "Address": "पता",
        // Service Provider sidebar
        "Requests": "अनुरोध", "My Requests": "मेरे अनुरोध",
        "Ratings & Reviews": "रेटिंग्स और समीक्षाएं",
        // Shopkeeper sidebar
        "Products": "उत्पाद", "My Products": "मेरे उत्पाद",
        "Add Product": "उत्पाद जोड़ें", "Orders": "ऑर्डर",
        // Profile
        "Management": "प्रबंधन", "Profile": "प्रोफ़ाइल",
        // New Features
        "AI Diagnostic Bot": "AI डायग्नोस्टिक बॉट",
        "Charging Station": "चार्जिंग स्टेशन",
        "Vehicle Health Analytics": "वाहन स्वास्थ्य विश्लेषण",
        "Quick Actions": "त्वरित कार्य",
        "Pending": "लंबित",
        "Total Orders": "कुल ऑर्डर",
        "Appointments": "अपॉइंटमेंट्स",
        "Service Request": "सेवा अनुरोध",
        "Appointment": "अपॉइंटमेंट",
        "Browse Shop": "दुकान देखें",
        "Settings": "सेटिंग्स",
        "Send Feedback": "प्रतिक्रिया भेजें",
        "Welcome back,": "वापसी पर स्वागत है,",
        "Here's an overview of your activity": "यहाँ आपकी गतिविधि का अवलोकन है",
        "Ask AI for Help": "AI से मदद मांगें",
        "Ask AI": "AI से पूछें"
    }
};

function applyGlobalLanguage(lang) {
    const trans = globalTranslations[lang];
    if (!trans) return;

    // Translate sidebar nav links
    document.querySelectorAll('.sidebar-nav a').forEach(link => {
        const textNode = link.childNodes[link.childNodes.length - 1];
        if (textNode && textNode.nodeType === Node.TEXT_NODE) {
            const trimmed = textNode.textContent.trim();
            if (trans[trimmed]) {
                textNode.textContent = ' ' + trans[trimmed];
            }
        }
    });

    // Translate sidebar section titles
    document.querySelectorAll('.sidebar-section-title').forEach(title => {
        const text = title.textContent.trim();
        if (trans[text]) {
            title.textContent = trans[text];
        }
    });

    // Translate logout button text (sidebar)
    document.querySelectorAll('.sidebar-nav button').forEach(btn => {
        const text = btn.textContent.trim();
        if (text === 'Logout' && trans['Logout']) {
            const icon = btn.querySelector('i');
            if (icon) {
                btn.innerHTML = '';
                btn.appendChild(icon);
                btn.appendChild(document.createTextNode(' ' + trans['Logout']));
            }
        }
    });

    // Translate elements with data-i18n
    document.querySelectorAll('[data-i18n]').forEach(el => {
        const key = el.getAttribute('data-i18n');
        if (typeof translations !== 'undefined' && translations[lang] && translations[lang][key]) {
            el.innerHTML = translations[lang][key];
        }
    });

    // Translate table headers
    document.querySelectorAll('.table-custom thead th').forEach(th => {
        const text = th.textContent.trim();
        if (trans[text]) {
            th.textContent = trans[text];
        }
    });

    // Translate page title in top navbar
    const navTitle = document.querySelector('.top-navbar h5');
    if (navTitle) {
        const text = navTitle.textContent.trim();
        if (trans[text]) {
            navTitle.textContent = trans[text];
        }
    }

    // Translate notification dropdown
    const notifTitle = document.querySelector('.notification-dropdown strong');
    if (notifTitle && trans['Notifications']) {
        notifTitle.textContent = trans['Notifications'];
    }

    // Translate empty states
    document.querySelectorAll('.empty-state p').forEach(p => {
        const text = p.textContent.trim();
        if (trans[text]) {
            p.textContent = trans[text];
        }
    });

    // Translate status badges & generic text
    document.querySelectorAll('.badge-custom, h3, h4, h5, p, span, label').forEach(el => {
        if (el.children.length === 0 || (el.children.length === 1 && el.querySelector('i'))) {
            let textNode = Array.from(el.childNodes).find(n => n.nodeType === Node.TEXT_NODE);
            if (textNode) {
                const trimmed = textNode.textContent.trim();
                if (trans[trimmed]) {
                    textNode.textContent = (textNode.textContent.startsWith(' ') ? ' ' : '') + trans[trimmed];
                }
            }
        }
    });

    // Translate logout modal
    const logoutModalTitle = document.querySelector('#logoutModal h5');
    if (logoutModalTitle && trans['Confirm Logout']) {
        const icon = logoutModalTitle.querySelector('i');
        if (icon) {
            logoutModalTitle.innerHTML = '';
            logoutModalTitle.appendChild(icon);
            logoutModalTitle.appendChild(document.createTextNode(trans['Confirm Logout']));
        }
    }

    const logoutModalText = document.querySelector('#logoutModal p');
    if (logoutModalText && trans['Are you sure you want to logout?']) {
        logoutModalText.textContent = trans['Are you sure you want to logout?'];
    }

    // Translate stat card labels
    document.querySelectorAll('.stat-info p').forEach(p => {
        const text = p.textContent.trim();
        if (trans[text]) {
            p.textContent = trans[text];
        }
    });

    // Translate data-card-header titles
    document.querySelectorAll('.data-card-header h5').forEach(h5 => {
        // Skip if already translated via data-i18n
        if (h5.querySelector('[data-i18n]')) return;

        const textNodes = [];
        h5.childNodes.forEach(node => {
            if (node.nodeType === Node.TEXT_NODE && node.textContent.trim()) {
                textNodes.push(node);
            }
        });
        textNodes.forEach(node => {
            const t = node.textContent.trim();
            if (trans[t]) node.textContent = trans[t];
        });
    });

    // Translate quick action cards text
    document.querySelectorAll('.quick-action-card span').forEach(span => {
        const text = span.textContent.trim();
        if (trans[text]) {
            span.textContent = trans[text];
        }
    });

    // Translate form labels
    document.querySelectorAll('.form-floating-custom label').forEach(label => {
        // Skip data-i18n labels
        if (label.hasAttribute('data-i18n')) return;
        const text = label.textContent.trim();
        if (trans[text]) {
            label.textContent = trans[text];
        }
    });

    // Translate button text inside btn-primary-custom and btn-outline-custom
    document.querySelectorAll('.btn-primary-custom span, .btn-outline-custom span').forEach(span => {
        const text = span.textContent.trim();
        if (trans[text]) {
            span.textContent = trans[text];
        }
    });

    // Translate user dropdown items
    document.querySelectorAll('.user-dropdown-item').forEach(item => {
        const textNode = item.childNodes[item.childNodes.length - 1];
        if (textNode && textNode.nodeType === Node.TEXT_NODE) {
            const trimmed = textNode.textContent.trim();
            if (trans[trimmed]) {
                textNode.textContent = ' ' + trans[trimmed];
            }
        }
    });

    // Translate dashboard welcome text
    document.querySelectorAll('h4.animate-fadeInUp, p.animate-fadeInUp').forEach(el => {
        const text = el.textContent.trim();
        for (const [key, val] of Object.entries(trans)) {
            if (text.startsWith(key)) {
                // preserve dynamic content after key
                el.childNodes.forEach(node => {
                    if (node.nodeType === Node.TEXT_NODE) {
                        const t = node.textContent.trim();
                        if (t === key || t.startsWith(key)) {
                            node.textContent = node.textContent.replace(key, val);
                        }
                    }
                });
            }
        }
    });

    // Translate Edit Profile modal
    const editProfileTitle = document.querySelector('#editProfileModal h5');
    if (editProfileTitle && trans['Edit Profile']) {
        const icon = editProfileTitle.querySelector('i');
        if (icon) {
            editProfileTitle.innerHTML = '';
            editProfileTitle.appendChild(icon);
            editProfileTitle.appendChild(document.createTextNode(trans['Edit Profile']));
        }
    }
}

// Table Search Filter Function
function initTableSearch(inputId, tableId) {
    const searchInput = document.getElementById(inputId);
    const table = document.getElementById(tableId);
    if (!searchInput || !table) return;

    searchInput.addEventListener('input', function () {
        const value = this.value.toLowerCase().trim();
        const rows = table.querySelectorAll('tbody tr');

        rows.forEach(row => {
            if (row.classList.contains('empty-state')) return;
            const text = row.innerText.toLowerCase();
            row.style.display = text.indexOf(value) > -1 ? '' : 'none';
        });

        // Show empty state if no rows match
        const visibleRows = Array.from(rows).filter(r => r.style.display !== 'none' && !r.classList.contains('empty-state'));
        let emptyStateRow = table.querySelector('.search-empty-state');
        
        if (visibleRows.length === 0) {
            if (!emptyStateRow) {
                emptyStateRow = document.createElement('tr');
                emptyStateRow.className = 'search-empty-state';
                emptyStateRow.innerHTML = `<td colspan="100%" class="empty-state"><i class="fas fa-search"></i><p>No matches found for "${value}"</p></td>`;
                table.querySelector('tbody').appendChild(emptyStateRow);
            }
        } else if (emptyStateRow) {
            emptyStateRow.remove();
        }
    });
}

// Export Table to CSV
function exportTableToCSV(tableId, filename) {
    const table = document.getElementById(tableId);
    if (!table) return;

    let csv = [];
    const rows = table.querySelectorAll('tr');

    for (let i = 0; i < rows.length; i++) {
        let row = [], cols = rows[i].querySelectorAll('td, th');

        for (let j = 0; j < cols.length; j++) {
            let data = cols[j].innerText.replace(/"/g, '""');
            row.push('"' + data + '"');
        }
        csv.push(row.join(','));
    }

    // Add UTF-8 BOM for Excel compatibility with non-ASCII characters
    const BOM = new Uint8Array([0xEF, 0xBB, 0xBF]);
    const csvFile = new Blob([BOM, csv.join('\n')], { type: 'text/csv;charset=utf-8' });
    const downloadLink = document.createElement('a');
    downloadLink.download = filename;
    downloadLink.href = window.URL.createObjectURL(csvFile);
    downloadLink.style.display = 'none';
    document.body.appendChild(downloadLink);
    downloadLink.click();
    document.body.removeChild(downloadLink);
}

function togglePasswordVisibility(inputId, icon) {
    const input = document.getElementById(inputId);
    if (input.type === 'password') {
        input.type = 'text';
        icon.classList.remove('fa-eye');
        icon.classList.add('fa-eye-slash');
    } else {
        input.type = 'password';
        icon.classList.remove('fa-eye-slash');
        icon.classList.add('fa-eye');
    }
}

function validateStrictEmail(input) {
    const regex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    if (input.value && !regex.test(input.value)) {
        input.style.borderColor = 'var(--danger)';
        input.classList.add('is-invalid');
    } else {
        input.style.borderColor = 'var(--border-color)';
        input.classList.remove('is-invalid');
    }
}
