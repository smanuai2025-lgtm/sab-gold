# 🥇 Mr. Golden Bader - غرفة العمليات الذكية

نظام متابعة وتحليل وتوصيات تداول الذهب - عالمياً ومحلياً في الكويت

---

## 📋 وصف المشروع

**Mr. Golden Bader** هو نظام ويب احترافي مخصص لمالك واحد، يعمل كغرفة عمليات ذكية لمتابعة أسعار الذهب عالمياً ومحلياً في الكويت لحظياً، مع:
- تحليل أسعار متقدم
- تحليل أخبار ذكي
- تنبيهات لحظية
- توصيات شراء/بيع مبنية على نماذج ذكاء اصطناعي

---

## 🏗️ هيكل المشروع

```
Mr.Golden Bader/
├── Backend/                          # الواجهة الخلفية
│   ├── MrGoldenBader.sln            # Solution File
│   ├── MrGoldenBader.API/           # Web API (ASP.NET Core 8)
│   ├── MrGoldenBader.Application/   # طبقة التطبيق (Services, DTOs)
│   ├── MrGoldenBader.Domain/        # طبقة المجال (Entities, Interfaces)
│   └── MrGoldenBader.Infrastructure/# طبقة البنية التحتية (EF Core, Repos)
│
├── frontend/                         # الواجهة الأمامية
│   ├── index.html                   # لوحة التحكم الرئيسية
│   ├── pages/                       # 12 صفحة HTML
│   ├── js/                          # JavaScript modules
│   ├── css/                         # Tailwind CSS + custom
│   └── admin/                       # لوحة الإدارة
│
└── price-proxy/                      # خادم وسيط للأسعار
    └── server.js                    # Node.js + Express
```

---

## 🛠️ التقنيات المستخدمة

### Backend
- **ASP.NET Core 8** - Web API
- **Entity Framework Core** - ORM
- **SQL Server** - قاعدة البيانات الرئيسية
- **Redis** - التخزين المؤقت
- **SignalR** - التحديثات اللحظية
- **JWT** - المصادقة
- **Hangfire** - المهام المجدولة
- **Serilog** - التسجيل

### Price Proxy
- **Node.js 18+** - JavaScript Runtime
- **Express** - Web Framework
- **Cheerio** - HTML Parsing
- **node-fetch** - HTTP Client

### Frontend
- **HTML5 + JavaScript** - الواجهة الأمامية
- **Tailwind CSS** - التصميم
- **Chart.js** - الرسوم البيانية
- **TradingView Widget** - أسعار فورية
- **Gemini AI** - التحليل الذكي
- **RTL Support** - دعم العربية الكامل

---

## 🚀 تشغيل المشروع

### 1️⃣ تشغيل Backend

```powershell
# الانتقال لمجلد Backend
cd Backend

# استعادة الحزم
dotnet restore

# إنشاء قاعدة البيانات
dotnet ef database update --project MrGoldenBader.Infrastructure --startup-project MrGoldenBader.API

# تشغيل التطبيق
dotnet run --project MrGoldenBader.API
```

سيعمل على: `https://localhost:5001` و `http://localhost:5000`

### 2️⃣ تشغيل Price Proxy

```bash
# الانتقال لمجلد price-proxy
cd price-proxy

# تثبيت الحزم
npm install

# تشغيل الخادم
npm start
```

سيعمل على: `http://localhost:3001`

### 3️⃣ تشغيل Frontend

```bash
# الانتقال لمجلد frontend
cd frontend

# تشغيل خادم محلي (يمكن استخدام live-server أو http-server)
# تثبيت http-server إذا لم يكن مثبتاً
npm install -g http-server

# تشغيل الخادم
http-server -p 3000
```

سيعمل على: `http://localhost:3000`

---

## 📡 نقاط النهاية (API Endpoints)

### Backend APIs

| Method | Endpoint | الوصف |
|--------|----------|-------|
| POST | `/api/auth/login` | تسجيل الدخول |
| GET | `/api/prices/global` | سعر الأونصة العالمي |
| GET | `/api/prices/kuwait` | أسعار الكويت |
| GET | `/api/recommendations/active` | التوصية النشطة |
| GET | `/api/alerts/unread` | التنبيهات غير المقروءة |

### AI Services

| Method | Endpoint | الوصف |
|--------|----------|-------|
| POST | `/api/predict` | التنبؤ بالسعر |
| POST | `/api/analyze-news` | تحليل خبر |
| POST | `/api/recommend` | إصدار توصية |

### SignalR Hub

- `/hubs/goldprice` - التحديثات اللحظية للأسعار والتنبيهات

---

## 🎨 الهوية البصرية

### الألوان الأساسية
- **ذهبي أساسي**: `#D4AF37`
- **ذهبي داكن**: `#B8860B`
- **أسود فحمي**: `#0A0E17`
- **أخضر صعود**: `#10B981`
- **أحمر نبيذي**: `#8B0000`

### القواعد
- اتجاه RTL كامل
- لغة عربية فصحى
- تصميم مالي احترافي
- لا مبالغة أو زخرفة

---

## 📝 ملاحظات التطوير

### القواعد الصارمة
- جميع التعليقات بالعربية
- لا بيانات وهمية
- كل مرحلة تُبنى فوق السابقة
- Production-Ready فقط

### المراحل القادمة
1. ✅ المرحلة 1: الهيكل العام (مكتمل)
2. ⏳ المرحلة 2: قواعد البيانات والنماذج
3. ⏳ المرحلة 3: جلب أسعار الذهب
4. ⏳ المرحلة 4: واجهة الأسعار
5. ⏳ المرحلة 5: نظام الأخبار
6. ⏳ المرحلة 6: محرك التنبؤ
7. ⏳ المرحلة 7: دمج الأخبار والأسعار
8. ⏳ المرحلة 8: محرك التوصيات
9. ⏳ المرحلة 9: التنبيهات اللحظية
10. ⏳ المرحلة 10: التقارير والإنتاج

---

## 📚 الوثائق الإضافية

- **[SECURITY_REPORT.md](./SECURITY_REPORT.md)** - تقرير الأمان الشامل
- **[TECHNICAL_ANALYSIS.md](./TECHNICAL_ANALYSIS.md)** - التحليل التقني المفصل
- **[ARABIC_SUMMARY.md](./ARABIC_SUMMARY.md)** - ملخص المراجعة بالعربية
- **[DEPLOYMENT.md](./DEPLOYMENT.md)** - دليل النشر
- **[SYSTEM_SETUP.md](./SYSTEM_SETUP.md)** - إعداد النظام

---

## 👤 المطور

تم تطويره لمالك واحد - نظام خاص ومخصص

---

## 📄 الترخيص

جميع الحقوق محفوظة © 2024


اهم محلات بيع الذهب في الكويت 
مع بانر صور و روابط مواقعهم للتوجه لمقارنة الاسعار بمنصتنا

مؤشر طلوع بعده طلوع 
