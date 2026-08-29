<div align="center">

<img src="logo.png" alt="Switch Logo" width="120"/>

# Switch — محول تخطيط لوحة المفاتيح

**تطبيق C# لويندوز يحول النص المكتوب بتخطيط عربي أو إنجليزي خاطئ بضغطة واحدة.**

</div>

## الاستخدام

1. شغّل `Switch.exe`.
2. حدد النص في أي تطبيق.
3. اضغط `Ctrl + Shift + Space`.

سيستبدل Switch النص المحدد بالناتج المحول بين Arabic 101 وEnglish QWERTY. يظهر التطبيق كأيقونة في منطقة الإشعارات؛ اضغط عليها بالزر الأيمن ثم اختر **خروج** لإيقافه.

## البنية

```text
Switch/
├── Program.cs                  نقطة تشغيل تطبيق Windows Forms (STA)
├── HotkeyWindow.cs             الاختصار العام، الحافظة، وأيقونة شريط المهام
├── KeyboardLayoutConverter.cs  خريطة Arabic 101 ↔ QWERTY ومنطق التحويل
├── NativeMethods.cs            استدعاءات Win32 الضرورية
├── SelfTests.cs                اختبارات التحويل المدمجة
├── ErrorLog.cs                 سجل أخطاء محلي لا يحفظ محتوى الحافظة
└── Switch.csproj               مشروع .NET Framework 4.8

Switch.Tests/
└── Switch.Tests.csproj         اختبارات التحويل المستقلة دون صلاحيات المسؤول

SwitchStartup/
├── Program.cs                  مشغّل غير مرتفع لمدخل Startup Apps
└── SwitchStartup.csproj        مشروع مشغّل بدء التشغيل
```

## المتطلبات والبناء

- Windows مع **.NET Framework 4.8**.
- Visual Studio 2022 أو Build Tools مع **.NET Framework 4.8 Developer Pack / Targeting Pack** للبناء.
- Inno Setup اختياريًا لبناء المثبّت.

```powershell
.\build.ps1
```

يبني السكربت التطبيق ويشغل الاختبارات المدمجة ثم يستدعي Inno Setup إذا كان `iscc.exe` مثبتًا. ينتج الملف التنفيذي في `Switch\bin\Release\Switch.exe` والمثبّت في `Output\Switch_Setup.exe`.

## ملاحظات تقنية

- لا توجد حزم NuGet أو عمليات شبكة؛ يعتمد التطبيق على Windows Forms وWin32 فقط.
- يُراقَب الاختصار عبر `WH_KEYBOARD_LL`، وتُنفَّذ عمليات الحافظة على خيط STA مع إعادة المحاولة خارج خيط واجهة المستخدم.
- تُحفظ بيانات الحافظة وتُستعاد بعد التحويل، بما في ذلك الصور والملفات متى أمكن تمثيلها بواسطة `IDataObject`.
- ينشئ المثبّت Scheduled Task بصلاحية المسؤول ليبدأ التطبيق تلقائيًا في الخلفية عند تسجيل الدخول إلى ويندوز، ويضيف اختصارًا ظاهرًا في مجلد Startup كي يظهر Switch في قائمة Startup Apps. الاختصار يشغّل برنامجًا مساعدًا صغيرًا يستدعي المهمة المرتفعة؛ وإذا تعذر الوصول إلى المهمة يستخدم تشغيلًا احتياطيًا بصلاحية المسؤول. يمكن مراجعة المهمة من Task Scheduler باسم `Switch`، أو داخل مجلد `Microsoft\Windows\Switch` على الأجهزة التي لا تتيح مجلد المهام الجذر.

## الرخصة

مرخص تحت [MIT](LICENSE).
