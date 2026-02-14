# 🚀 SabeekaGold - دليل النشر للإنتاج

## 📋 المتطلبات
- Docker & Docker Compose
- دومين مسجل
- شهادة SSL (Let's Encrypt مجانية أو مدفوعة)

---

## 🏆 أفضل الاستضافات اضمن و لمدفوعة (موصى بها)

### ⭐ الخيار الموصى به: Hostinger VPS (الأفضل - كل شيء في مكان واحد)

| الميزة | التفاصيل |
|--------|----------|
| **السعر** | **$4.99/شهر** (عرض -64%) |
| **المواصفات** | 4GB RAM, 1 vCPU, 50GB NVMe, 4TB bandwidth |
| **Docker Manager** | ✅ واجهة رسومية سهلة لإدارة الحاويات |
| **الدومين** | ✅ دومين .cloud مجاني لسنة |
| **SSL** | ✅ Let's Encrypt مجاني |
| **Backups** | ✅ نسخ احتياطية أسبوعية مجانية |
| **لوحة تحكم** | ✅ hPanel سهلة جداً |
| **الدعم** | ✅ 24/7 دعم فني |

```
التكلفة الشهرية:
- VPS KVM 1: $4.99
- دومين: مجاني (سنة أولى)
- SSL: مجاني
- المجموع: ~$5/شهر فقط!
```

**رابط التسجيل:** https://www.hostinger.com/vps-hosting

#### 📦 خطوات النشر على Hostinger:

```bash
# 1. اشترِ VPS KVM 1 واختر OS: Ubuntu 22.04

# 2. اتصل بالخادم
ssh root@your-hostinger-ip

# 3. Docker مثبت مسبقاً! تحقق:
docker --version
docker compose version

# 4. ارفع المشروع
mkdir -p /opt/sabeekagold
cd /opt/sabeekagold

# 5. انسخ الملفات (من جهازك)
scp -r ./* root@your-ip:/opt/sabeekagold/

# 6. أعد الإعدادات
cp .env.example .env
nano .env

# 7. شغّل!
docker compose up -d

# 8. فعّل SSL
apt install certbot -y
certbot --standalone -d yourdomain.com
```

---

### 🥇 الخيار الأول: DigitalOcean (الأفضل للمشاريع المتوسطة)

| الميزة | التفاصيل |
|--------|----------|
| **السعر** | $24-48/شهر |
| **Droplet** | 4GB RAM, 2 vCPU, 80GB SSD |
| **المميزات** | سريع جداً، لوحة تحكم سهلة، Docker مدمج |
| **الدومين** | من Namecheap أو GoDaddy |
| **SSL** | Let's Encrypt مجاني أو Cloudflare |
| **الموقع** | Frankfurt أو Amsterdam (قريب من الكويت) |

```
التكلفة الشهرية:
- Droplet: $24
- Managed Database (اختياري): $15
- المجموع: ~$24-40/شهر
```

**رابط التسجيل:** https://www.digitalocean.com

---

### 🥈 الخيار الثاني: Hetzner (الأرخص والأسرع)

| الميزة | التفاصيل |
|--------|----------|
| **السعر** | €8-20/شهر |
| **Server** | 4GB RAM, 2 vCPU, 40GB NVMe |
| **المميزات** | أرخص الأسعار، سرعة خارقة، مراكز بيانات ألمانية |
| **الموقع** | Nuremberg أو Helsinki |

```
التكلفة الشهرية:
- Cloud Server CX21: €5.83
- أو Cloud Server CX31: €10.59
- المجموع: ~€6-15/شهر
```

**رابط التسجيل:** https://www.hetzner.com/cloud

---

### 🥉 الخيار الثالث: Azure (للمؤسسات)

| الميزة | التفاصيل |
|--------|----------|
| **السعر** | $50-150/شهر |
| **المميزات** | موثوقية عالية، Azure SQL، Azure App Service |
| **مراكز البيانات** | UAE North (دبي) - الأقرب للكويت! |
| **SSL** | مدمج مع App Service |

```
التكلفة الشهرية:
- App Service B1: $13
- Azure SQL Basic: $5
- Storage: $5
- المجموع: ~$25-50/شهر
```

**رابط التسجيل:** https://azure.microsoft.com

---

### 🏅 الخيار الرابع: Vultr (أداء ممتاز)

| الميزة | التفاصيل |
|--------|----------|
| **السعر** | $24-48/شهر |
| **المميزات** | 17 موقع حول العالم، NVMe SSD |
| **الموقع** | Amsterdam أو Frankfurt |

**رابط التسجيل:** https://www.vultr.com

---

## 🌐 شراء الدومين (Domain)

### الخيارات الموصى بها:

| المزود | السعر السنوي | المميزات |
|--------|-------------|----------|
| **Namecheap** | $8-12 | حماية WHOIS مجانية |
| **Cloudflare Registrar** | $8-10 | بسعر التكلفة، DNS سريع |
| **GoDaddy** | $10-15 | الأشهر عالمياً |
| **Porkbun** | $7-10 | الأرخص |

**نصيحة:** اشترِ `.com` أو `.gold` مثل:
- `sabeekagold.com`
- `sabeekaذهب.com`

---

## 🔒 شهادة SSL

### الخيارات:

| الخيار | السعر | المميزات |
|--------|-------|----------|
| **Let's Encrypt** | مجاني | تجديد تلقائي كل 90 يوم |
| **Cloudflare** | مجاني | حماية + CDN + SSL |
| **Comodo PositiveSSL** | $9/سنة | للمواقع التجارية |

**التوصية:** استخدم **Cloudflare** (مجاني) - يوفر:
- شهادة SSL
- CDN سريع
- حماية DDoS
- تحليلات

---

## 📦 خطوات النشر

### 1. إعداد الخادم (DigitalOcean مثالاً)

```bash
# تسجيل الدخول للخادم
ssh root@your-server-ip

# تثبيت Docker
curl -fsSL https://get.docker.com | sh

# تثبيت Docker Compose
apt install docker-compose-plugin

# إنشاء مجلد التطبيق
mkdir -p /opt/sabeekagold
cd /opt/sabeekagold
```

### 2. رفع الملفات

```bash
# من جهازك المحلي
scp -r "c:\Users\OMAR1\Desktop\Mr.Golden Bader\*" root@your-server-ip:/opt/sabeekagold/
```

### 3. إعداد المتغيرات البيئية

```bash
cd /opt/sabeekagold
cp .env.example .env
nano .env  # عدّل القيم
```

### 4. تشغيل التطبيق

```bash
chmod +x deploy.sh
./deploy.sh
```

### 5. إعداد SSL مع Certbot

```bash
# تثبيت Certbot
apt install certbot python3-certbot-nginx

# الحصول على الشهادة
certbot --nginx -d sabeekagold.com -d www.sabeekagold.com
```

---

## 🔧 إعداد Cloudflare (موصى به)

1. **أنشئ حساب:** https://cloudflare.com
2. **أضف الدومين**
3. **غيّر Nameservers** في Namecheap/GoDaddy
4. **فعّل:**
   - SSL/TLS: Full (Strict)
   - Always Use HTTPS: ON
   - Auto Minify: ON
   - Brotli: ON

---

## 📊 مقارنة سريعة

| الاستضافة | السعر/شهر | الأداء | السهولة | التوصية |
|-----------|-----------|--------|---------|---------|
| DigitalOcean | $24-48 | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | **الأفضل للمبتدئين** |
| Hetzner | €6-15 | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | **الأفضل للسعر** |
| Azure | $25-100 | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | **للمؤسسات** |
| Vultr | $24-48 | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | بديل جيد |

---

## 💡 توصيتي النهائية

### للبداية:
**Hetzner Cloud CX21** (€5.83/شهر) + **Cloudflare** (مجاني) + **Namecheap Domain** ($10/سنة)

**التكلفة الإجمالية: ~$8/شهر**

### للإنتاج الكامل:
**DigitalOcean Droplet** ($24/شهر) + **Managed Database** ($15/شهر) + **Cloudflare Pro** ($20/شهر)

**التكلفة الإجمالية: ~$60/شهر**

---

## 🆘 الدعم

للمساعدة في النشر، تواصل معي!
