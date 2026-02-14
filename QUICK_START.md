# 🚀 دليل البدء السريع - Mr. Golden Bader

## المتطلبات الأساسية

### البرامج المطلوبة:
- ✅ .NET 8.0 SDK
- ✅ Node.js 18+ و npm
- ✅ SQL Server (أو Docker)
- ✅ Redis (أو Docker)
- ✅ Git

---

## الخطوة 1: استنساخ المشروع

```bash
git clone https://github.com/smanuai2025-lgtm/sab-gold.git
cd sab-gold
```

---

## الخطوة 2: إعداد التكوينات

### أ) إعداد Backend

```bash
cd Backend/MrGoldenBader.API

# نسخ ملف التكوين النموذجي
cp appsettings.example.json appsettings.json

# تعديل الملف وإضافة:
# - JWT Secret Key (يجب أن يكون 32 حرف على الأقل)
# - SQL Server connection string
# - Redis connection string
# - API Keys للخدمات الخارجية
```

**مثال على `appsettings.json`:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MrGoldenBaderDb;Trusted_Connection=True;",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Key": "YOUR-SUPER-SECRET-KEY-MIN-32-CHARS",
    "Issuer": "MrGoldenBader",
    "Audience": "MrGoldenBaderClient"
  },
  "ExternalApis": {
    "AlphaVantageApi": {
      "ApiKey": "YOUR_KEY_HERE"
    }
  },
  "NewsApis": {
    "AlphaVantage": { "ApiKey": "YOUR_KEY" },
    "NewsData": { "ApiKey": "YOUR_KEY" },
    "GNews": { "ApiKey": "YOUR_KEY" }
  }
}
```

### ب) الحصول على API Keys

1. **AlphaVantage** (مجاني): https://www.alphavantage.co/support/#api-key
2. **NewsData.io** (200 طلب/يوم): https://newsdata.io/register
3. **GNews** (100 طلب/يوم): https://gnews.io/register
4. **Gemini AI** (للفرونت إند): https://makersuite.google.com/app/apikey

---

## الخطوة 3: تشغيل قواعد البيانات

### خيار أ: باستخدام Docker (موصى به)

```bash
# من المجلد الجذري
docker-compose up -d sqlserver redis

# انتظر 30 ثانية حتى تجهز SQL Server
```

### خيار ب: تثبيت محلي

1. تثبيت SQL Server Express
2. تثبيت Redis (Windows: استخدم WSL أو Redis on Windows port)

---

## الخطوة 4: تشغيل Backend

```bash
cd Backend

# استعادة الحزم
dotnet restore

# تشغيل Migration لإنشاء قاعدة البيانات
dotnet ef database update --project MrGoldenBader.Infrastructure --startup-project MrGoldenBader.API

# تشغيل التطبيق
dotnet run --project MrGoldenBader.API
```

✅ **Backend يعمل الآن على:**
- API: http://localhost:5000
- HTTPS: https://localhost:5001
- Swagger: http://localhost:5000/swagger
- Hangfire: http://localhost:5000/hangfire

---

## الخطوة 5: إنشاء مستخدم

⚠️ **مهم:** لا يوجد مستخدم افتراضي، يجب إنشاؤه يدوياً.

### خيار أ: باستخدام SQL

```sql
USE MrGoldenBaderDb;

INSERT INTO Users (Id, Username, Email, PasswordHash, FullName, IsActive, CreatedAt, UpdatedAt)
VALUES (
    NEWID(),
    'admin',
    'admin@sabeekagold.com',
    'jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=', -- كلمة المرور: admin123
    'Administrator',
    1,
    GETUTCDATE(),
    GETUTCDATE()
);
```

### خيار ب: باستخدام API

```bash
# إضافة user عبر SQL أولاً ثم استخدم endpoint للـ login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

---

## الخطوة 6: تشغيل Price Proxy

```bash
cd price-proxy

# تثبيت المتطلبات
npm install

# إنشاء ملف .env (اختياري)
echo "ALLOWED_ORIGINS=http://localhost,http://localhost:3000" > .env

# تشغيل الخادم
npm start
```

✅ **Price Proxy يعمل على:** http://localhost:3001

---

## الخطوة 7: تشغيل Frontend

```bash
cd frontend

# تثبيت http-server إذا لم يكن مثبتاً
npm install -g http-server

# تشغيل الخادم
http-server -p 3000

# أو استخدم أي خادم محلي آخر:
# python -m http.server 3000
# php -S localhost:3000
```

✅ **Frontend يعمل على:** http://localhost:3000

---

## الخطوة 8: تسجيل الدخول

1. افتح المتصفح: http://localhost:3000
2. اذهب لصفحة Login
3. استخدم:
   - Username: `admin`
   - Password: `admin123`

---

## ✅ التحقق من العمل

### اختبار Backend
```bash
# Health Check
curl http://localhost:5000/health

# Global Price
curl http://localhost:5000/api/prices/global

# Kuwait Prices
curl http://localhost:5000/api/prices/kuwait
```

### اختبار Price Proxy
```bash
# Gold Price
curl http://localhost:3001/api/gold/price

# Kuwait Price
curl http://localhost:3001/api/gold/kuwait
```

---

## 🐛 حل المشاكل الشائعة

### مشكلة: "Cannot connect to SQL Server"
**الحل:**
```bash
# تحقق من أن SQL Server يعمل
docker ps  # إذا استخدمت Docker

# أو تحقق من الـ connection string في appsettings.json
```

### مشكلة: "Redis connection failed"
**الحل:**
```bash
# تحقق من Redis
docker ps  # للـ Docker
redis-cli ping  # يجب أن يرجع PONG

# أو عطّل Redis مؤقتاً (الكود يعمل بدونه لكن بطيء)
```

### مشكلة: "401 Unauthorized" في Frontend
**الحل:**
- تأكد من إنشاء مستخدم في قاعدة البيانات
- تحقق من username/password
- تأكد من أن JWT Key في appsettings.json صحيح

### مشكلة: "CORS error" في المتصفح
**الحل:**
```bash
# في Backend/MrGoldenBader.API/Program.cs
# تأكد من وجود CORS policy للـ localhost:3000
```

### مشكلة: "Migration pending"
**الحل:**
```bash
cd Backend
dotnet ef database update --project MrGoldenBader.Infrastructure --startup-project MrGoldenBader.API
```

---

## 📊 الخطوات التالية

بعد التشغيل الناجح:

1. ✅ **استكشف Dashboard** - شاهد الأسعار الحية
2. ✅ **جرب Portfolio** - أضف عملية شراء
3. ✅ **شاهد Technical Analysis** - RSI, MACD
4. ✅ **استخدم Calculator** - احسب سعر الذهب بالعيار
5. ✅ **تابع News** - اقرأ أخبار الذهب
6. ✅ **أضف Alerts** - اضبط تنبيهات أسعار

---

## 🔧 أوامر مفيدة

### Backend
```bash
# بناء المشروع
dotnet build

# تشغيل Tests (عند إضافتها)
dotnet test

# إنشاء Migration جديد
dotnet ef migrations add MigrationName --project MrGoldenBader.Infrastructure --startup-project MrGoldenBader.API

# مسح قاعدة البيانات
dotnet ef database drop --project MrGoldenBader.Infrastructure --startup-project MrGoldenBader.API
```

### Docker
```bash
# تشغيل كل شيء
docker-compose up -d

# إيقاف كل شيء
docker-compose down

# مشاهدة Logs
docker-compose logs -f

# إعادة بناء الـ images
docker-compose build --no-cache
```

---

## 📚 مراجع إضافية

- [SECURITY_REPORT.md](./SECURITY_REPORT.md) - التقرير الأمني
- [TECHNICAL_ANALYSIS.md](./TECHNICAL_ANALYSIS.md) - التحليل التقني
- [ARABIC_SUMMARY.md](./ARABIC_SUMMARY.md) - الملخص بالعربية

---

## 💡 نصائح

1. **استخدم environment variables** بدلاً من hardcoding في production
2. **غيّر كلمات المرور الافتراضية** فوراً
3. **فعّل HTTPS** في production
4. **راجع SECURITY_REPORT.md** قبل النشر
5. **أضف unit tests** قبل إضافة ميزات جديدة

---

**آخر تحديث:** 2026-02-14  
**الحالة:** جاهز للتطوير ✅
