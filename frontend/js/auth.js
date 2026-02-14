/**
 * ═══════════════════════════════════════════════════════════════════
 * SabeekaGold - نظام المصادقة والتحقق من المستخدمين
 * ═══════════════════════════════════════════════════════════════════
 */

const AuthService = {
    // مفاتيح التخزين
    STORAGE_KEYS: {
        USERS: 'sabeeka_users',
        CURRENT_USER: 'sabeeka_current_user',
        SESSION: 'sabeeka_session'
    },

    // الأدمن الافتراضي
    DEFAULT_ADMIN: {
        id: 'admin_001',
        name: 'مدير النظام',
        email: 'admin@sabeekagold.com',
        password: 'Admin@123',
        role: 'admin',
        createdAt: new Date().toISOString(),
        avatar: null
    },

    /**
     * تهيئة النظام - إنشاء الأدمن الافتراضي
     */
    init() {
        const users = this.getUsers();
        const adminExists = users.some(u => u.role === 'admin');
        
        if (!adminExists) {
            users.push(this.DEFAULT_ADMIN);
            localStorage.setItem(this.STORAGE_KEYS.USERS, JSON.stringify(users));
        }
    },

    /**
     * الحصول على جميع المستخدمين
     */
    getUsers() {
        try {
            return JSON.parse(localStorage.getItem(this.STORAGE_KEYS.USERS)) || [];
        } catch {
            return [];
        }
    },

    /**
     * تسجيل مستخدم جديد
     */
    register(userData) {
        const users = this.getUsers();
        
        // التحقق من عدم وجود البريد
        if (users.some(u => u.email === userData.email)) {
            return { success: false, error: 'البريد الإلكتروني مستخدم بالفعل' };
        }

        // التحقق من صحة البيانات
        if (!userData.name || userData.name.length < 2) {
            return { success: false, error: 'الاسم يجب أن يكون حرفين على الأقل' };
        }

        if (!userData.email || !this.isValidEmail(userData.email)) {
            return { success: false, error: 'البريد الإلكتروني غير صحيح' };
        }

        if (!userData.password || userData.password.length < 6) {
            return { success: false, error: 'كلمة المرور يجب أن تكون 6 أحرف على الأقل' };
        }

        // إنشاء المستخدم
        const newUser = {
            id: 'user_' + Date.now(),
            name: userData.name,
            email: userData.email,
            phone: userData.phone || '',
            password: userData.password, // في الإنتاج يجب تشفير كلمة المرور
            role: 'user',
            createdAt: new Date().toISOString(),
            avatar: null,
            preferences: {
                notifications: true,
                emailAlerts: true,
                darkMode: true
            }
        };

        users.push(newUser);
        localStorage.setItem(this.STORAGE_KEYS.USERS, JSON.stringify(users));

        return { success: true, user: { ...newUser, password: undefined } };
    },

    /**
     * تسجيل الدخول
     */
    login(email, password, remember = false) {
        this.init(); // التأكد من وجود الأدمن
        
        const users = this.getUsers();
        const user = users.find(u => u.email === email && u.password === password);

        if (!user) {
            return { success: false, error: 'البريد الإلكتروني أو كلمة المرور غير صحيحة' };
        }

        // إنشاء الجلسة
        const session = {
            userId: user.id,
            token: this.generateToken(),
            expiresAt: remember 
                ? new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString() // 30 يوم
                : new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString(), // 24 ساعة
            createdAt: new Date().toISOString()
        };

        const storage = remember ? localStorage : sessionStorage;
        storage.setItem(this.STORAGE_KEYS.SESSION, JSON.stringify(session));
        storage.setItem(this.STORAGE_KEYS.CURRENT_USER, JSON.stringify({ ...user, password: undefined }));

        return { success: true, user: { ...user, password: undefined } };
    },

    /**
     * تسجيل الخروج
     */
    logout() {
        localStorage.removeItem(this.STORAGE_KEYS.SESSION);
        localStorage.removeItem(this.STORAGE_KEYS.CURRENT_USER);
        sessionStorage.removeItem(this.STORAGE_KEYS.SESSION);
        sessionStorage.removeItem(this.STORAGE_KEYS.CURRENT_USER);
        
        return { success: true };
    },

    /**
     * الحصول على المستخدم الحالي
     */
    getCurrentUser() {
        // التحقق من الجلسة أولاً
        let session = null;
        let user = null;

        try {
            session = JSON.parse(localStorage.getItem(this.STORAGE_KEYS.SESSION)) ||
                     JSON.parse(sessionStorage.getItem(this.STORAGE_KEYS.SESSION));
            
            user = JSON.parse(localStorage.getItem(this.STORAGE_KEYS.CURRENT_USER)) ||
                   JSON.parse(sessionStorage.getItem(this.STORAGE_KEYS.CURRENT_USER));
        } catch {
            return null;
        }

        if (!session || !user) return null;

        // التحقق من انتهاء الجلسة
        if (new Date(session.expiresAt) < new Date()) {
            this.logout();
            return null;
        }

        return user;
    },

    /**
     * التحقق من تسجيل الدخول
     */
    isLoggedIn() {
        return this.getCurrentUser() !== null;
    },

    /**
     * التحقق من صلاحيات الأدمن
     */
    isAdmin() {
        const user = this.getCurrentUser();
        return user && user.role === 'admin';
    },

    /**
     * تحديث بيانات المستخدم
     */
    updateUser(userId, updates) {
        const users = this.getUsers();
        const index = users.findIndex(u => u.id === userId);

        if (index === -1) {
            return { success: false, error: 'المستخدم غير موجود' };
        }

        // لا يمكن تغيير البريد إلى بريد موجود
        if (updates.email && updates.email !== users[index].email) {
            if (users.some(u => u.email === updates.email)) {
                return { success: false, error: 'البريد الإلكتروني مستخدم بالفعل' };
            }
        }

        users[index] = { ...users[index], ...updates, id: userId };
        localStorage.setItem(this.STORAGE_KEYS.USERS, JSON.stringify(users));

        // تحديث المستخدم الحالي إذا كان هو نفسه
        const currentUser = this.getCurrentUser();
        if (currentUser && currentUser.id === userId) {
            const storage = localStorage.getItem(this.STORAGE_KEYS.SESSION) ? localStorage : sessionStorage;
            storage.setItem(this.STORAGE_KEYS.CURRENT_USER, JSON.stringify({ ...users[index], password: undefined }));
        }

        return { success: true, user: { ...users[index], password: undefined } };
    },

    /**
     * حذف مستخدم (للأدمن فقط)
     */
    deleteUser(userId) {
        if (!this.isAdmin()) {
            return { success: false, error: 'غير مصرح لك بهذا الإجراء' };
        }

        const users = this.getUsers();
        const user = users.find(u => u.id === userId);

        if (!user) {
            return { success: false, error: 'المستخدم غير موجود' };
        }

        if (user.role === 'admin') {
            return { success: false, error: 'لا يمكن حذف حساب الأدمن' };
        }

        const filtered = users.filter(u => u.id !== userId);
        localStorage.setItem(this.STORAGE_KEYS.USERS, JSON.stringify(filtered));

        return { success: true };
    },

    /**
     * الحصول على إحصائيات المستخدمين (للأدمن)
     */
    getStats() {
        const users = this.getUsers();
        
        return {
            total: users.length,
            admins: users.filter(u => u.role === 'admin').length,
            users: users.filter(u => u.role === 'user').length,
            thisMonth: users.filter(u => {
                const created = new Date(u.createdAt);
                const now = new Date();
                return created.getMonth() === now.getMonth() && created.getFullYear() === now.getFullYear();
            }).length
        };
    },

    /**
     * توليد رمز عشوائي
     */
    generateToken() {
        return 'xxxx-xxxx-xxxx-xxxx'.replace(/x/g, () => 
            Math.floor(Math.random() * 16).toString(16)
        );
    },

    /**
     * التحقق من صحة البريد الإلكتروني
     */
    isValidEmail(email) {
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    },

    /**
     * حماية الصفحة - توجيه غير المسجلين
     */
    requireAuth(redirectTo = '/pages/login.html') {
        if (!this.isLoggedIn()) {
            window.location.href = redirectTo;
            return false;
        }
        return true;
    },

    /**
     * حماية صفحات الأدمن
     */
    requireAdmin(redirectTo = '/pages/login.html') {
        if (!this.isAdmin()) {
            window.location.href = redirectTo;
            return false;
        }
        return true;
    }
};

// تهيئة النظام عند تحميل الصفحة
AuthService.init();

// تصدير للاستخدام العام
window.AuthService = AuthService;
