# ScreenTranslator

**Select a region of your screen → OCR → translate → floating popup.**
**Выдели область экрана → распознавание → перевод → всплывающее окно.**

---

## What it is / Что это

**EN:** A Windows tray utility. Press a global hotkey (default `Win+Shift+D`), drag a rectangle over anything on screen — a game, a PDF, a video, an untranslatable error dialog — and it screenshots that region, runs Windows' built-in OCR on it, sends the text to a translation provider, and shows the result in a small always-on-top popup next to your selection. No browser, no clipboard dance, no uploading screenshots anywhere. The stack: C# / .NET 8 (`net8.0-windows10.0.19041.0`), WPF for all windows, WinForms only for the tray icon (`NotifyIcon`), `Windows.Media.Ocr` (WinRT) for text recognition, `System.Drawing` (`Graphics.CopyFromScreen` + bicubic upscale) for capture, `user32.dll` `RegisterHotKey` via P/Invoke for the global hotkey, `HttpClient` for the three translation backends (Google / DeepL / LibreTranslate), and `System.Text.Json` for settings. Zero NuGet packages — the whole thing builds against the SDK and the OS. The app's own UI text is in Russian.

**RU:** Утилита для Windows, живущая в трее. Нажимаешь глобальную горячую клавишу (по умолчанию `Win+Shift+D`), выделяешь мышью прямоугольник на экране — в игре, в PDF, в видео, в неперевариваемом окне ошибки — программа делает снимок этой области, прогоняет через встроенный OCR Windows, отправляет текст в переводчик и показывает результат в маленьком окне поверх всех окон рядом с выделением. Без браузера, без танцев с буфером обмена, без загрузки скриншотов куда-либо. Стек: C# / .NET 8 (`net8.0-windows10.0.19041.0`), WPF для всех окон, WinForms только ради иконки в трее (`NotifyIcon`), `Windows.Media.Ocr` (WinRT) для распознавания, `System.Drawing` (`Graphics.CopyFromScreen` + бикубическое увеличение) для захвата, `RegisterHotKey` из `user32.dll` через P/Invoke для горячей клавиши, `HttpClient` для трёх бэкендов перевода (Google / DeepL / LibreTranslate) и `System.Text.Json` для настроек. Ноль NuGet-пакетов — всё собирается на голом SDK и API системы. Интерфейс самого приложения — на русском.

Pipeline / Конвейер:

```
hotkey / tray  ->  OverlayWindow (select)  ->  CaptureService.Capture
      ->  CaptureService.Upscale (x1..x3)  ->  OcrService.RecognizeAsync
      ->  TranslatorFactory -> ITranslator.TranslateAsync  ->  ResultWindow
```

Features / Возможности:

- Global hotkey, re-bindable by pressing the combo in Settings (`Ctrl`/`Shift`/`Alt`/`Win` + letter, digit, numpad digit or `F1`–`F24`).
- Single instance, enforced by a named `Mutex`; tray icon is drawn at runtime (blue circle with a white `T`), no icon file needed.
- Multi-monitor and DPI aware — the selection rect is converted from WPF DIPs to physical pixels using the overlay's actual DPI scale.
- Optional upscaling of the captured region (×1 … ×3, default ×2, hard-capped at 5000 px per side) to help OCR read small text.
- OCR language: auto (user profile languages) or an explicit installed recognizer language.
- Three providers: Google (free, keyless, **unofficial** endpoint), DeepL (Free or Pro key), LibreTranslate (public or self-hosted URL + optional key).
- Result popup: draggable, `Copy` button, `Show original` toggle, configurable font size, optional auto-copy to clipboard, optional auto-hide after N seconds (cancelled if you hover it), `Esc` to close.
- Friendly failure paths: missing OCR language pack, no text recognized, and provider errors each show a red-bordered info popup with a hint instead of crashing.

---

## Install / Установка

**EN:** You need Windows 10 version 2004 (build 19041) or newer — or Windows 11 — plus the .NET 8 SDK to build it. Because the target framework is Windows-specific and uses WinRT OCR, this cannot be built or run on Linux or macOS. There is no `.sln` file: point `dotnet` at the folder or at `ScreenTranslator.csproj` directly. You also need at least one Windows OCR language pack installed, otherwise the app starts but every capture ends with an "OCR unavailable" popup — install one via Settings → Time & language → Language & region → Add a language, then restart the app. Nothing else to install: there are no NuGet dependencies and no API key is required for the default Google provider.

**RU:** Нужна Windows 10 версии 2004 (сборка 19041) или новее — либо Windows 11 — и .NET 8 SDK для сборки. Целевая платформа привязана к Windows и использует WinRT OCR, поэтому собрать или запустить это на Linux или macOS невозможно. Файла `.sln` в проекте нет: указывай `dotnet` на папку или прямо на `ScreenTranslator.csproj`. Ещё потребуется хотя бы один языковой пакет распознавания Windows, иначе приложение запустится, но каждый снимок будет заканчиваться окном «OCR недоступен» — поставь пакет через Параметры → Время и язык → Язык и регион → Добавить язык и перезапусти приложение. Больше ничего ставить не надо: NuGet-зависимостей нет, а для провайдера Google по умолчанию не нужен API-ключ.

```powershell
git clone https://github.com/<you>/ScreenTranslator.git
cd ScreenTranslator

# run from source / запуск из исходников
dotnet run

# release build / релизная сборка
dotnet build -c Release

# single-file exe, framework-dependent / один exe, нужен установленный .NET 8 Desktop Runtime
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

# fully self-contained, no runtime needed / полностью автономная сборка, рантайм не нужен
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## Usage / Использование

**EN:** Launch the exe — nothing visible happens, that's correct: it's a tray app with a hidden zero-size main window that exists only to own the hotkey message loop. Press `Win+Shift+D` (or double-click the tray icon, or pick "Перевести область" from its right-click menu). The screen dims; drag with the left mouse button to select a region, and a badge shows the selection size in pixels. `Esc` or right-click cancels; a selection smaller than 10×10 px is treated as a cancel. Release the button and the translation popup appears beside your selection, flipping to the other side if it would run off-screen. Open "Настройки" from the tray menu to change the hotkey (click the field and just press the combination), the provider, languages, capture scale, font size, auto-copy and auto-hide. Settings are saved as JSON at `%APPDATA%\ScreenTranslator\settings.json` and are applied immediately, including re-registering the hotkey. Note: DeepL and LibreTranslate keys are stored in that file in **plain text**, and the Google provider talks to an undocumented endpoint that can stop working at any time or be blocked on your network.

**RU:** Запусти exe — визуально ничего не произойдёт, и это нормально: это приложение в трее со скрытым главным окном нулевого размера, которое существует только чтобы принимать сообщения о горячей клавише. Нажми `Win+Shift+D` (или двойной клик по иконке в трее, или «Перевести область» из её контекстного меню). Экран притемнится; удерживая левую кнопку мыши, выдели область — рядом появится плашка с размером выделения в пикселях. `Esc` или правая кнопка отменяют; выделение меньше 10×10 px тоже считается отменой. Отпусти кнопку — окно перевода появится рядом с выделением, а если не помещается — с другой стороны. «Настройки» из меню трея позволяют сменить горячую клавишу (кликни в поле и просто нажми сочетание), провайдера, языки, масштаб снимка, размер шрифта, автокопирование и автоскрытие. Настройки сохраняются в JSON по пути `%APPDATA%\ScreenTranslator\settings.json` и применяются сразу, включая перерегистрацию горячей клавиши. Учти: ключи DeepL и LibreTranslate лежат в этом файле **в открытом виде**, а провайдер Google обращается к недокументированному эндпоинту, который в любой момент может отвалиться или быть заблокирован в твоей сети.

Settings reference / Справочник настроек (`settings.json`):

| Key | Default | Meaning / Значение |
| --- | --- | --- |
| `Hotkey` | `Win+Shift+D` | Global hotkey; needs ≥1 modifier + letter/digit/`NumN`/`F1`–`F24`. If it's already taken by another app, you get a tray balloon warning. |
| `Provider` | `Google` | `Google`, `DeepL` or `LibreTranslate`. |
| `DeeplApiKey` / `DeeplIsFreeKey` | `""` / `true` | Free keys (`...:fx`) hit `api-free.deepl.com`, Pro keys hit `api.deepl.com`. |
| `LibreTranslateUrl` / `LibreTranslateApiKey` | `https://libretranslate.com/translate` / `""` | Must be a full `http(s)` URL ending in `/translate`. |
| `SourceLanguage` | `auto` | `auto` or a language code. DeepL only uses the part before `-` (`pt-br` → `PT`). |
| `TargetLanguage` | `ru` | Target language code, lowercased on save. |
| `OcrLanguage` | `""` | Empty = auto from your Windows profile languages; otherwise an installed recognizer tag like `en-US`. |
| `ImageScale` | `2.0` | Bicubic upscale factor 1–3 before OCR. Raise it for small text. |
| `AutoCopyResult` | `false` | Copy the translation to the clipboard automatically. |
| `AutoHideSeconds` | `0` | `0` = never auto-close; 1–600 seconds otherwise. |
| `ResultFontSize` | `14.0` | Popup body font size, 8–40. |

---

## Structure / Структура

**EN:** Flat and small — 24 files, no DI container, no MVVM framework, no abstractions beyond a single `ITranslator` interface. `App.xaml.cs` is the orchestrator: it owns the mutex, the hidden main window, the tray icon, the hotkey service and the whole capture→OCR→translate→display sequence in `RunTranslation()` (App.xaml.cs:73). Everything reusable lives under `Services/`, the settings POCO under `Models/`, and the three windows sit in the project root as classic XAML + code-behind pairs.

**RU:** Плоско и компактно — 24 файла, без DI-контейнера, без MVVM-фреймворка, без абстракций сверх одного интерфейса `ITranslator`. `App.xaml.cs` — дирижёр: он держит мьютекс, скрытое главное окно, иконку в трее, сервис горячих клавиш и всю цепочку снимок→OCR→перевод→показ в методе `RunTranslation()` (App.xaml.cs:73). Всё переиспользуемое лежит в `Services/`, модель настроек — в `Models/`, а три окна расположены в корне проекта классическими парами XAML + code-behind.

```text
ScreenTranslator/
├─ ScreenTranslator.csproj              net8.0-windows10.0.19041.0, WPF + WinForms, zero NuGet deps
├─ app.manifest                         asInvoker, dpiAware, longPathAware, Win10+ compat GUID
├─ App.xaml / App.xaml.cs               entry point, single-instance mutex, tray + menu, runtime-drawn
│                                       icon, hotkey wiring, the full translation pipeline
├─ OverlayWindow.xaml / .xaml.cs        fullscreen dimmed selector across the whole virtual screen,
│                                       drag-to-select, size badge, Esc / RMB cancel, 10px minimum
├─ ResultWindow.xaml / .xaml.cs         borderless always-on-top popup; ShowTranslation() (blue border)
│                                       and ShowInfo() (red border), anchoring, fade-in, drag, copy,
│                                       original toggle, auto-hide timer
├─ SettingsWindow.xaml / .xaml.cs       settings form, hotkey capture, per-provider panels, validation
├─ Models/
│  └─ AppSettings.cs                    settings POCO + TranslationProvider enum + Clone()
└─ Services/
   ├─ CaptureService.cs                 Graphics.CopyFromScreen + HighQualityBicubic upscale (5000px cap)
   ├─ HotkeyService.cs                  RegisterHotKey/UnregisterHotKey P/Invoke, WM_HOTKEY hook,
   │                                    combination parser and Key -> token formatter
   ├─ OcrService.cs                     Windows.Media.Ocr wrapper: availability probe, language list,
   │                                    Bitmap -> SoftwareBitmap conversion, line joining
   ├─ SettingsService.cs                JSON load/save in %APPDATA%\ScreenTranslator, never throws on load
   └─ Translation/
      ├─ ITranslator.cs                 Name + TranslateAsync(text, source, target)
      ├─ TranslatorFactory.cs           settings.Provider -> concrete translator
      ├─ TransHttp.cs                   shared static HttpClient, 20s timeout, custom User-Agent
      ├─ GoogleTranslator.cs            unofficial translate_a/single?client=gtx endpoint, segment join
      ├─ DeepLTranslator.cs             POST /v2/translate, free vs pro host, lang code normalisation
      └─ LibreTranslateTranslator.cs    POST JSON {q, source, target, format, api_key}
```

---

## ⚠️ AI Slop / ⚠️ ИИ-слоп

**EN:** Full disclosure: **this entire project was written by an AI.** Every class, every XAML attribute, every Russian error message. The human whose name is on this repository typed a prompt, watched code appear, checked that a window showed up, and committed it. They do not know what most of it does. Nobody has reviewed it. It might be broken nonsense — and there are already obvious smells: the Google backend is an undocumented endpoint that can die or get rate-limited without warning, your DeepL key is written to disk in plain text, exceptions are swallowed in several places with bare `catch { }`, there is not a single test anywhere in the repo, and no one has verified behaviour on mixed-DPI multi-monitor setups. If it works for you, that's luck, not craftsmanship. If it doesn't work, or you look at `RunTranslation()` and feel physical pain: **please fork it and fix it.** Rip out what's dumb, add tests, send a PR — or just keep your fork and never look back. Genuinely, that's the best outcome for this repo.

**RU:** Признаюсь честно: **весь этот проект написан ИИ.** Каждый класс, каждый атрибут XAML, каждое сообщение об ошибке по-русски. Человек, чьё имя стоит на этом репозитории, ввёл промпт, посмотрел, как появляется код, убедился, что окно открывается, и закоммитил. Что делает большая часть этого кода, он не знает. Никто это не ревьюил. Возможно, это сломанная бессмыслица — и очевидные душки уже видны: бэкенд Google — недокументированный эндпоинт, который может отвалиться или упереться в лимиты без предупреждения; ключ DeepL пишется на диск открытым текстом; в нескольких местах исключения глотаются пустым `catch { }`; тестов в репозитории нет ни одного; поведение на мультимониторных конфигурациях со смешанным DPI никто не проверял. Если у тебя всё работает — это везение, а не мастерство. Если не работает или ты посмотрел на `RunTranslation()` и почувствовал физическую боль: **пожалуйста, форкни и почини.** Выкинь глупости, добавь тесты, пришли PR — или просто оставь свой форк и забудь про этот. Серьёзно, это лучшее, что может случиться с этим репозиторием

а кстати, я фанат некопары o_0