/**
 * ═══════════════════════════════════════════════════════════════════
 * SabeekaGold - Mobile Navigation Component
 * ═══════════════════════════════════════════════════════════════════
 * يتم تضمين هذا الملف في جميع الصفحات لتوحيد التنقل
 */

// Determine if we're in pages folder or root
const isInPagesFolder = window.location.pathname.includes('/pages/');
const pathPrefix = isInPagesFolder ? '../' : '';
const pagesPrefix = isInPagesFolder ? '' : 'pages/';

// Get current page name
const currentPage = window.location.pathname.split('/').pop() || 'index.html';

// Navigation links configuration
const navLinks = [
    { href: 'index.html', icon: '🏠', text: 'الرئيسية', root: true },
    { href: 'analysis.html', icon: '📈', text: 'التحليلات' },
    { href: 'gold-assistant.html', icon: '🧠', text: 'المستشار الذكي' },
    { href: 'calculator.html', icon: '🧮', text: 'الحاسبة' },
    { href: 'portfolio.html', icon: '💼', text: 'محفظتي' },
    { href: 'news.html', icon: '📰', text: 'الأخبار' },
    { href: 'reports.html', icon: '📊', text: 'التقارير' },
    { href: 'alerts.html', icon: '🔔', text: 'التنبيهات' }
];

// Build href based on current location
function buildHref(link) {
    if (link.root) {
        return isInPagesFolder ? '../index.html' : 'index.html';
    }
    return isInPagesFolder ? link.href : 'pages/' + link.href;
}

// Check if link is active
function isActive(link) {
    if (link.root) {
        return currentPage === 'index.html' || currentPage === '';
    }
    return currentPage === link.href;
}

// Inject mobile navigation HTML
function injectMobileNav() {
    // Create mobile menu HTML
    const mobileMenuHTML = `
    <aside id="mobile-menu" class="fixed top-0 right-0 w-64 h-full bg-zinc-900/98 backdrop-blur-xl z-[60] transform translate-x-full transition-transform duration-300 lg:hidden border-r border-gold-500/20">
        <div class="p-4 h-full flex flex-col">
            <div class="flex items-center justify-between mb-4">
                <div class="flex items-center gap-2">
                    <img src="${pathPrefix}images/logo.png" alt="SabeekaGold" class="h-8 w-auto">
                    <span class="font-bold text-gold-400 text-sm">SabeekaGold</span>
                </div>
                <button onclick="toggleMobileMenu()" class="text-gray-400 hover:text-white p-1">
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                    </svg>
                </button>
            </div>
            
            <!-- User Welcome (shown when logged in) -->
            <div id="mobile-menu-user" class="hidden mb-4 p-3 bg-zinc-800/50 rounded-xl border border-zinc-700">
                <div class="flex items-center gap-3">
                    <div class="w-10 h-10 bg-gradient-to-br from-gold-500 to-gold-600 rounded-full flex items-center justify-center text-zinc-900 font-bold" id="mobile-menu-avatar">م</div>
                    <div>
                        <p class="text-xs text-gray-400">مرحباً بك</p>
                        <p id="mobile-menu-name" class="text-sm text-white font-medium">مستخدم</p>
                    </div>
                </div>
            </div>
            
            <nav class="space-y-1 flex-1 overflow-y-auto">
                ${navLinks.map(link => `
                <a href="${buildHref(link)}" class="mobile-nav-link flex items-center gap-3 px-3 py-2.5 rounded-lg ${isActive(link) ? 'bg-gold-500/10 text-gold-400 border border-gold-500/20' : 'text-gray-300 hover:bg-gold-500/10 hover:text-gold-400'} transition-all">
                    <span class="text-base">${link.icon}</span><span class="text-sm">${link.text}</span>
                </a>
                `).join('')}
                
                <!-- User-only links -->
                <div id="mobile-menu-user-links" class="hidden pt-2 mt-2 border-t border-gold-500/20 space-y-1">
                    <a id="mobile-menu-admin" href="${pathPrefix}admin/dashboard.html" class="hidden flex items-center gap-3 px-3 py-2.5 rounded-lg bg-indigo-500/20 text-indigo-400 border border-indigo-500/30">
                        <span class="text-base">🎛️</span><span class="text-sm">لوحة التحكم</span>
                    </a>
                    <a href="${pagesPrefix}settings.html" class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-gray-300 hover:bg-gold-500/10 hover:text-gold-400 transition-all">
                        <span class="text-base">⚙️</span><span class="text-sm">الإعدادات</span>
                    </a>
                    <button onclick="logoutUser(); toggleMobileMenu();" class="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-rose-400 hover:bg-rose-500/10 transition-all">
                        <span class="text-base">🚪</span><span class="text-sm">تسجيل الخروج</span>
                    </button>
                </div>
            </nav>
        </div>
    </aside>
    
    <!-- Mobile Menu Overlay -->
    <div id="mobile-overlay" class="fixed inset-0 bg-black/60 backdrop-blur-sm z-[55] hidden lg:hidden" onclick="toggleMobileMenu()"></div>
    
    <style>
        #mobile-menu.menu-open { transform: translateX(0) !important; }
    </style>
    `;
    
    // Find existing mobile menu and replace, or append to body
    const existingMenu = document.getElementById('mobile-menu');
    const existingOverlay = document.getElementById('mobile-overlay');
    
    if (existingMenu) existingMenu.remove();
    if (existingOverlay) existingOverlay.remove();
    
    document.body.insertAdjacentHTML('afterbegin', mobileMenuHTML);
}

// Mobile Menu Toggle
function toggleMobileMenu() {
    const menu = document.getElementById('mobile-menu');
    const overlay = document.getElementById('mobile-overlay');
    
    if (!menu || !overlay) return;
    
    const isOpen = menu.classList.contains('menu-open');
    
    if (isOpen) {
        menu.classList.remove('menu-open');
        overlay.classList.add('hidden');
        document.body.style.overflow = '';
    } else {
        menu.classList.add('menu-open');
        overlay.classList.remove('hidden');
        document.body.style.overflow = 'hidden';
    }
}

// Update auth UI for mobile
function updateMobileAuthUI() {
    if (typeof AuthService === 'undefined') return;
    
    const user = AuthService.getCurrentUser();
    
    // Mobile Header elements
    const mobileHeaderGuest = document.getElementById('mobile-header-guest');
    const mobileHeaderUser = document.getElementById('mobile-header-user');
    const mobileHeaderAvatar = document.getElementById('mobile-header-avatar');
    
    // Mobile Menu elements
    const mobileMenuUser = document.getElementById('mobile-menu-user');
    const mobileMenuAvatar = document.getElementById('mobile-menu-avatar');
    const mobileMenuName = document.getElementById('mobile-menu-name');
    const mobileMenuUserLinks = document.getElementById('mobile-menu-user-links');
    const mobileMenuAdmin = document.getElementById('mobile-menu-admin');
    
    if (user) {
        const initial = user.name ? user.name.charAt(0) : 'م';
        
        // Mobile Header
        if (mobileHeaderGuest) mobileHeaderGuest.classList.add('hidden');
        if (mobileHeaderUser) {
            mobileHeaderUser.classList.remove('hidden');
            mobileHeaderUser.classList.add('flex');
        }
        if (mobileHeaderAvatar) mobileHeaderAvatar.textContent = initial;
        
        // Mobile Menu
        if (mobileMenuUser) mobileMenuUser.classList.remove('hidden');
        if (mobileMenuAvatar) mobileMenuAvatar.textContent = initial;
        if (mobileMenuName) mobileMenuName.textContent = user.name;
        if (mobileMenuUserLinks) mobileMenuUserLinks.classList.remove('hidden');
        
        // Admin links
        if (user.role === 'admin' && mobileMenuAdmin) {
            mobileMenuAdmin.classList.remove('hidden');
            mobileMenuAdmin.classList.add('flex');
        }
    } else {
        // Mobile Header
        if (mobileHeaderGuest) {
            mobileHeaderGuest.classList.remove('hidden');
            mobileHeaderGuest.classList.add('flex');
        }
        if (mobileHeaderUser) mobileHeaderUser.classList.add('hidden');
        
        // Mobile Menu
        if (mobileMenuUser) mobileMenuUser.classList.add('hidden');
        if (mobileMenuUserLinks) mobileMenuUserLinks.classList.add('hidden');
    }
}

// Logout function
function logoutUser() {
    if (typeof AuthService !== 'undefined') {
        AuthService.logout();
    }
    window.location.reload();
}

// Initialize on DOM ready
document.addEventListener('DOMContentLoaded', () => {
    injectMobileNav();
    setTimeout(updateMobileAuthUI, 100); // Small delay to ensure auth is loaded
});

// Make functions globally available
window.toggleMobileMenu = toggleMobileMenu;
window.logoutUser = logoutUser;
