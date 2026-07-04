#!/usr/bin/env python3
"""
Switch - Smart Keyboard Layout Text Converter
Author: AI Assistant
Description: A background script that fixes text typed in the wrong keyboard layout
(Arabic 101 <-> English QWERTY) using a hotkey (Ctrl + Shift + Space).
"""

import os
import sys
import time
import threading
import subprocess
import ctypes
import pystray
from PIL import Image
import keyboard
import pyperclip

def resource_path(relative_path):
    """ Get absolute path to resource, works for dev and for PyInstaller """
    try:
        # PyInstaller creates a temp folder and stores path in _MEIPASS
        base_path = sys._MEIPASS
    except Exception:
        base_path = os.path.abspath(".")
    return os.path.join(base_path, relative_path)

# Redirect stdout/stderr to DummyStream if they are None (which happens in windowless pythonw/PyInstaller executables)
class DummyStream:
    def write(self, x):
        pass
    def flush(self):
        pass

if sys.stdout is None:
    sys.stdout = DummyStream()
if sys.stderr is None:
    sys.stderr = DummyStream()

# Reconfigure stdout/stderr to UTF-8 on Windows to prevent UnicodeEncodeError when printing Arabic
if sys.platform.startswith('win'):
    try:
        sys.stdout.reconfigure(encoding='utf-8')
        sys.stderr.reconfigure(encoding='utf-8')
    except Exception:
        pass

# --- Configuration ---
HOTKEY = "ctrl+shift+space"
COPY_PASTE_DELAY = 0.05  # Delay in seconds to allow OS clipboard operations to complete

# --- Keyboard Layout Mappings ---
# English QWERTY to Arabic 101 Mapping
en_to_ar = {
    # Lowercase & Standard keys
    "q": "ض", "w": "ص", "e": "ث", "r": "ق", "t": "ف", "y": "غ", "u": "ع", "i": "ه", "o": "خ", "p": "ح",
    "[": "ج", "]": "د", "\\": "\\",
    "a": "ش", "s": "س", "d": "ي", "f": "ب", "g": "ل", "h": "ا", "j": "ت", "k": "ن", "l": "م", ";": "ك", "'": "ط",
    "z": "ئ", "x": "ء", "c": "ؤ", "v": "ر", "b": "لا", "n": "ى", "m": "ة", ",": "و", ".": "ز", "/": "ظ",
    "`": "ذ",
    
    # Uppercase & Shifted keys
    "Q": "َ", "W": "ً", "E": "ُ", "R": "ٌ", "T": "ﻹ", "Y": "إ", "U": "`", "I": "÷", "O": "×", "P": "؛",
    "{": "<", "}": ">", "|": "|",
    "A": "ِ", "S": "ٍ", "D": "]", "F": "[", "G": "ﻷ", "H": "أ", "J": "ـ", "K": "،", "L": "/", ":": ":", '"': '"',
    "Z": "~", "X": "ْ", "C": "}", "V": "{", "B": "ﻵ", "N": "آ", "M": "'", "<": ",", ">": ".", "?": "؟",
    "~": "ّ",
    
    # Mirroring parenthesis/braces for RTL language alignment
    "(": ")", ")": "(",
}

# Generate reverse mapping (Arabic to English) for single characters
ar_to_en = {v: k for k, v in en_to_ar.items() if len(v) == 1}

# Manual reverse mapping for Arabic ligatures which decompose to multiple characters
# We map both standard decomposed output (Laam + Alif variant) and composed forms
ar_ligatures = {
    # Decomposed forms (standard Windows output)
    "\u0644\u0627": "b",  # ل + ا
    "\u0644\u0625": "T",  # ل + إ
    "\u0644\u0623": "G",  # ل + أ
    "\u0644\u0622": "B",  # ل + آ
    
    # Composed presentation forms (compatibility)
    "\ufefb": "b",        # لا
    "\ufefc": "b",
    "\ufef9": "T",        # ﻹ
    "\ufefa": "T",
    "\ufef7": "G",        # ﻷ
    "\ufef8": "G",
    "\ufef5": "B",        # ﻵ
    "\ufef6": "B"
}

# Lock to prevent concurrent execution of the hotkey handler
processing_lock = threading.Lock()


def is_arabic_char(char: str) -> bool:
    """
    Checks if a character belongs to any Arabic Unicode block.
    Covers standard Arabic, supplement, extended, and presentation forms A & B.
    """
    code = ord(char)
    return (
        (0x0600 <= code <= 0x06FF) or  # Arabic standard
        (0x0750 <= code <= 0x077F) or  # Arabic Supplement
        (0x08A0 <= code <= 0x08FF) or  # Arabic Extended-A
        (0xFB50 <= code <= 0xFDFF) or  # Arabic Presentation Forms-A
        (0xFE70 <= code <= 0xFEFF)     # Arabic Presentation Forms-B
    )


def convert_text(text: str) -> str:
    """
    Converts text between Arabic 101 and English QWERTY layouts bidirectionally.
    Determines direction automatically based on character counts.
    """
    if not text:
        return text

    # Normalize curly/smart quotes and punctuation first
    replacements = {
        "’": "'",
        "‘": "'",
        "”": '"',
        "“": '"',
        "–": "-",  # en-dash
        "—": "-",  # em-dash
    }
    for k, v in replacements.items():
        text = text.replace(k, v)

    # Count Arabic and English characters in the text
    arabic_chars = 0
    english_chars = 0
    
    for char in text:
        if is_arabic_char(char):
            arabic_chars += 1
        elif char.isalpha():
            english_chars += 1

    # Choose layout transformation direction
    if arabic_chars >= english_chars:
        # Convert Arabic -> English
        # 1. Replace multi-character ligatures first
        converted = text
        for ligature, english_key in ar_ligatures.items():
            converted = converted.replace(ligature, english_key)
            
        # 2. Map remaining characters
        result = []
        for char in converted:
            result.append(ar_to_en.get(char, char))
        return "".join(result)
    else:
        # Convert English -> Arabic
        result = []
        for char in text:
            result.append(en_to_ar.get(char, char))
        return "".join(result)


def simulate_copy():
    """Simulates Ctrl+C using Windows keybd_event to avoid layout/scan code conflicts."""
    # Release Shift (0x10) and Space (0x20) so they don't interfere
    ctypes.windll.user32.keybd_event(0x10, 0, 2, 0) # KEYEVENTF_KEYUP
    ctypes.windll.user32.keybd_event(0x20, 0, 2, 0) # KEYEVENTF_KEYUP
    time.sleep(0.01)
    
    # Press Ctrl (0x11)
    ctypes.windll.user32.keybd_event(0x11, 0, 0, 0)
    # Press C (0x43)
    ctypes.windll.user32.keybd_event(0x43, 0, 0, 0)
    time.sleep(0.01)
    
    # Release C (0x43)
    ctypes.windll.user32.keybd_event(0x43, 0, 2, 0) # KEYEVENTF_KEYUP
    # Release Ctrl (0x11)
    ctypes.windll.user32.keybd_event(0x11, 0, 2, 0) # KEYEVENTF_KEYUP


def simulate_paste():
    """Simulates Ctrl+V using Windows keybd_event to avoid layout/scan code conflicts."""
    # Release Shift (0x10) and Space (0x20) so they don't interfere
    ctypes.windll.user32.keybd_event(0x10, 0, 2, 0) # KEYEVENTF_KEYUP
    ctypes.windll.user32.keybd_event(0x20, 0, 2, 0) # KEYEVENTF_KEYUP
    time.sleep(0.01)
    
    # Press Ctrl (0x11)
    ctypes.windll.user32.keybd_event(0x11, 0, 0, 0)
    # Press V (0x56)
    ctypes.windll.user32.keybd_event(0x56, 0, 0, 0)
    time.sleep(0.01)
    
    # Release V (0x56)
    ctypes.windll.user32.keybd_event(0x56, 0, 2, 0) # KEYEVENTF_KEYUP
    # Release Ctrl (0x11)
    ctypes.windll.user32.keybd_event(0x11, 0, 2, 0) # KEYEVENTF_KEYUP


def on_hotkey_pressed():
    """
    Hotkey event handler.
    1. Copies currently selected text using Ctrl+C simulation via keybd_event.
    2. Translates the text layout.
    3. Pastes the translated text using Ctrl+V simulation via keybd_event.
    4. Restores original clipboard contents.
    """
    # Prevent concurrent execution
    if not processing_lock.acquire(blocking=False):
        return

    try:
        # Save original clipboard content so we can restore it later
        try:
            original_clipboard = pyperclip.paste()
        except Exception:
            original_clipboard = ""

        # Wait for modifier keys to be physically released (up to 1.0 second)
        # to avoid sending simulated events while keys are held down,
        # which prevents conflicts like ghost volume-down commands.
        start_time = time.time()
        while (keyboard.is_pressed("ctrl") or keyboard.is_pressed("shift") or keyboard.is_pressed("space")) and (time.time() - start_time < 1.0):
            time.sleep(0.01)
        time.sleep(COPY_PASTE_DELAY)

        # Clear clipboard to detect if Ctrl+C successfully copies new text
        pyperclip.copy("")
        time.sleep(COPY_PASTE_DELAY)

        # Simulate Copy (Ctrl + C) using native keybd_event
        simulate_copy()
        time.sleep(COPY_PASTE_DELAY * 2)  # Give OS slightly more time to copy

        # Retrieve selected text
        selected_text = pyperclip.paste()

        # If nothing was selected, do not perform conversion and restore clipboard
        if not selected_text:
            if original_clipboard:
                pyperclip.copy(original_clipboard)
            return

        # Perform layout conversion
        converted_text = convert_text(selected_text)

        # If conversion results in the same text, do nothing
        if converted_text == selected_text:
            if original_clipboard:
                pyperclip.copy(original_clipboard)
            return

        # Copy converted text to clipboard
        pyperclip.copy(converted_text)
        time.sleep(COPY_PASTE_DELAY)

        # Simulate Paste (Ctrl + V) using native keybd_event
        simulate_paste()
        time.sleep(COPY_PASTE_DELAY * 2)  # Give OS time to complete paste

        # Restore original clipboard content
        if original_clipboard:
            pyperclip.copy(original_clipboard)
        else:
            pyperclip.copy("")

    except Exception as e:
        print(f"Error during conversion: {e}", file=sys.stderr)
    finally:
        processing_lock.release()


def run_tests():
    """
    Runs self-test cases to verify the translation logic.
    """
    print("Running self-tests for keyboard layout conversion...")
    
    test_cases = [
        # English -> Arabic
        ("ahmed", "شاةثي"),
        ("hello", "اثممخ"),
        ("qwe", "ضصث"),
        ("ctrl + shift + space", "ؤفقم + ساهبف + سحشؤث"),
        ("Ctrl + Shift + Space", "}فقم + ٍاهبف + ٍحشؤث"),
        
        # Arabic -> English
        ("شاةثي", "ahmed"),
        ("أثممخ", "Hello"),
        ("اثممخ", "hello"),
        ("ضصث", "qwe"),
        ("لا", "b"),
        ("ﻹ", "T"),
        ("ﻷ", "G"),
        ("ﻵ", "B"),
        ("ِأ’ُ] ـِ’ِ/ ~ِ،÷", "AHMED JAMAL ZAKI"),
        
        # Mixed / layout-independent text preservation
        ("ahmed 123!", "شاةثي 123!"),
    ]
    
    failed = 0
    for i, (input_val, expected) in enumerate(test_cases, 1):
        actual = convert_text(input_val)
        if actual == expected:
            print(f"Test {i} passed: '{input_val}' -> '{actual}'")
        else:
            print(f"Test {i} FAILED!")
            print(f"  Input:    '{input_val}'")
            print(f"  Expected: '{expected}'")
            print(f"  Actual:   '{actual}'")
            failed += 1
            
    if failed == 0:
        print("\nAll tests passed successfully!")
        sys.exit(0)
    else:
        print(f"\n{failed} tests failed.")
        sys.exit(1)


def show_startup_message():
    """Shows a native Windows info dialog when the program starts."""
    title = "Switch - EN - AR"
    message = (
        "تم تشغيل برنامج Switch بنجاح وهو يعمل الآن في الخلفية.\n\n"
        "• طريقة الاستخدام:\n"
        "  1. حدد النص الذي كتبته بالخطأ.\n"
        "  2. اضغط على: Ctrl + Shift + Space وسيتم تصحيحه فوراً.\n\n"
        "• التحكم في البرنامج:\n"
        "  انقر بالزر الأيمن على أيقونة البرنامج في شريط المهام (بجانب الساعة) للخروج.\n\n"
        "  حقوق النشر محفوظة لـ ahmedjamalzaki@ ©"
    )
    # MB_OK (0) | MB_ICONINFORMATION (0x40) | MB_SYSTEMMODAL (0x1000) | MB_RIGHT (0x00080000) | MB_RTLREADING (0x00100000)
    ctypes.windll.user32.MessageBoxW(0, message, title, 0x40 | 0x1000 | 0x00080000 | 0x00100000)


def on_tray_exit(icon, item):
    """Exit handler for system tray menu."""
    icon.stop()
    os._exit(0)


def setup_tray_icon():
    """Sets up the system tray icon and menu."""
    try:
        icon_path = resource_path("logo.ico")
        if not os.path.exists(icon_path):
            image = Image.new('RGB', (64, 64), color=(0, 128, 255))
        else:
            image = Image.open(icon_path)
            
        menu = pystray.Menu(
            pystray.MenuItem("\u200f(Exit) خروج", on_tray_exit)
        )
        
        icon = pystray.Icon("Switch", image, "Switch - محول النص الذكي", menu)
        icon.run()
    except Exception as e:
        print(f"Error starting system tray icon: {e}", file=sys.stderr)


def main():
    # If run with --test, execute unit tests
    if "--test" in sys.argv:
        run_tests()
        return

    # Show startup message box in the main thread (blocks until user clicks OK to guarantee visibility)
    show_startup_message()

    # Start system tray icon in a separate thread
    threading.Thread(target=setup_tray_icon, daemon=True).start()

    print("==================================================")
    print(" Switch - Smart Keyboard Layout Text Converter    ")
    print("==================================================")
    print(f"Status: Active and listening in the background...")
    print(f"Hotkey: {HOTKEY.upper()}")
    print("To stop, close this console window, press Ctrl+C, or use System Tray.")
    print("==================================================")

    # Register the keyboard hook
    keyboard.add_hotkey(HOTKEY, on_hotkey_pressed)

    # Keep the script running (keyboard.wait blocks indefinitely)
    try:
        keyboard.wait()
    except KeyboardInterrupt:
        print("\nSwitch has been stopped.")


if __name__ == "__main__":
    main()
