// ===== ServicePlatform - Site JavaScript =====

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
});

// Table Search Filter Function
function initTableSearch(inputId, tableId) {
    const searchInput = document.getElementById(inputId);
    const table = document.getElementById(tableId);
    if (!searchInput || !table) return;

    searchInput.addEventListener('keyup', function() {
        const value = this.value.toLowerCase();
        const rows = table.querySelectorAll('tbody tr');
        
        rows.forEach(row => {
            const text = row.textContent.toLowerCase();
            row.style.display = text.indexOf(value) > -1 ? '' : 'none';
        });
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
    
    const csvFile = new Blob([csv.join('\n')], {type: 'text/csv'});
    const downloadLink = document.createElement('a');
    downloadLink.download = filename;
    downloadLink.href = window.URL.createObjectURL(csvFile);
    downloadLink.style.display = 'none';
    document.body.appendChild(downloadLink);
    downloadLink.click();
    document.body.removeChild(downloadLink);
}
