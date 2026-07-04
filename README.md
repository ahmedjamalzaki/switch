<div align="center">

<img src="logo.png" alt="Switch Logo" width="120"/>

# Switch — محول تخطيط لوحة المفاتيح

**حوّل النص المكتوب بخطأ من العربية إلى الإنجليزية أو العكس بضغطة واحدة!**

[![Release](https://img.shields.io/github/v/release/ahmedjamalzaki/switch?label=إصدار&color=blue)](https://github.com/ahmedjamalzaki/switch/releases/latest)
[![Platform](https://img.shields.io/badge/نظام_التشغيل-Windows-blue?logo=windows)](https://github.com/ahmedjamalzaki/switch/releases/latest)
[![License](https://img.shields.io/badge/الرخصة-MIT-green)](LICENSE)

</div>

---

## 📖 عن البرنامج

**Switch** هو برنامج خفيف يعمل في خلفية نظام ويندوز، يتيح لك تصحيح النص الذي كتبته بلغة خاطئة بضغطة اختصار واحدة.

مثال عملي:
- كتبت `شاثي` وأنت تقصد `done` ؟ → حدد النص واضغط `Ctrl + Shift + Space` وسيتحول فوراً إلى `done`!
- كتبت `hello` وأنت تقصد `اثممخ` ؟ → نفس الاختصار يحوّله للعربية بنفس السرعة!

---

## ✨ المميزات

- ⚡ **سريع جداً** — تحويل فوري بدون تأخير ملحوظ
- 🔄 **تحويل ذكي** — يكتشف تلقائياً هل النص عربي أو إنجليزي ويحوّله للآخر
- 🖥️ **يعمل في الخلفية** — لا يزعجك بأي نوافذ أثناء العمل
- 🔔 **أيقونة في شريط المهام** — سهل الوصول والتحكم
- 📋 **يحافظ على الحافظة** — يستعيد محتوى الـ Clipboard الأصلي بعد التحويل
- 🚀 **يبدأ مع ويندوز** — اختياري عند التثبيت

---

## 📥 التنزيل والتثبيت

> **للتثبيت السهل والسريع:**

[![تنزيل الآن](https://img.shields.io/badge/⬇️_تنزيل_Switch_Setup.exe-blue?style=for-the-badge)](https://github.com/ahmedjamalzaki/switch/releases/latest)

1. حمّل ملف `Switch_Setup.exe` من [صفحة الإصدارات](https://github.com/ahmedjamalzaki/switch/releases/latest)
2. شغّل الملف وأكمل خطوات التثبيت
3. ابدأ استخدام البرنامج فوراً!

> لا يحتاج البرنامج صلاحيات مدير (Admin) للتثبيت.

---

## 🎮 طريقة الاستخدام

| الخطوة | الإجراء |
|--------|---------|
| 1️⃣ | اكتب النص بأي تطبيق (Word, Notepad, متصفح...) |
| 2️⃣ | حدد النص المكتوب بخطأ بالفأرة أو `Ctrl+A` |
| 3️⃣ | اضغط `Ctrl + Shift + Space` |
| ✅ | سيتحول النص تلقائياً للغة الصحيحة! |

---

## 🛠️ البناء من المصدر

إذا أردت تشغيل البرنامج من الكود المصدري:

```bash
# 1. تثبيت المتطلبات
pip install keyboard pyperclip pystray pillow

# 2. تشغيل البرنامج
python switch.py

# 3. تشغيل الاختبارات
python switch.py --test

# 4. بناء ملف exe
python -m PyInstaller --noconsole --onefile --add-data "logo.ico;." --icon=logo.ico switch.py
```

---

## 🗂️ هيكل المشروع

```
switch/
├── switch.py          # الكود الرئيسي للبرنامج
├── setup.iss          # ملف بناء المثبت (Inno Setup)
├── logo.png           # شعار البرنامج
├── logo.ico           # أيقونة البرنامج
├── dist/
│   └── switch.exe     # الملف التنفيذي المُجمَّع
└── Output/
    └── Switch_Setup.exe  # مثبت البرنامج
```

---

## 📋 المتطلبات

- نظام ويندوز 7 أو أحدث (32/64 بت)
- لا يحتاج تثبيت Python أو أي برامج إضافية (المثبت يتضمن كل شيء)

---

## 📄 الرخصة

هذا البرنامج مرخص تحت رخصة [MIT](LICENSE).

**حقوق النشر محفوظة © 2025 [ahmedjamalzaki](https://github.com/ahmedjamalzaki)**
