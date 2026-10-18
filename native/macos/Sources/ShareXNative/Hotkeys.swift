// ShareX - A program that allows you to take screenshots and share any file type
// Copyright (c) 2007-2026 ShareX Team
// Licensed under the GNU General Public License v3. See LICENSE.txt in the repository root.

// System-wide hotkeys through Carbon's RegisterEventHotKey. Unlike CGEventTap this needs no
// Accessibility permission. Events are delivered on the main run loop, which Avalonia drives.

import Carbon

public typealias ShareXHotkeyCallback = @convention(c) (UInt32) -> Void

private let hotkeySignature: OSType = 0x5348_5258 // 'SHRX'
private var hotkeyCallback: ShareXHotkeyCallback?
private var hotkeyRefs: [UInt32: EventHotKeyRef] = [:]
private var hotkeyHandler: EventHandlerRef?

/// Installs the hotkey event handler. `callback` receives the id passed to sharex_hotkey_register
/// and is always invoked on the main thread. Returns 0 or an OSStatus.
@_cdecl("sharex_hotkeys_initialize")
public func sharex_hotkeys_initialize(_ callback: ShareXHotkeyCallback?) -> Int32 {
    return onMain {
        hotkeyCallback = callback
        if hotkeyHandler != nil {
            return ShareXStatus.ok.rawValue
        }

        var eventType = EventTypeSpec(eventClass: OSType(kEventClassKeyboard), eventKind: UInt32(kEventHotKeyPressed))
        let status = InstallEventHandler(GetApplicationEventTarget(), { _, event, _ in
            var hotkeyID = EventHotKeyID()
            let result = GetEventParameter(event, EventParamName(kEventParamDirectObject), EventParamType(typeEventHotKeyID),
                                           nil, MemoryLayout<EventHotKeyID>.size, nil, &hotkeyID)
            if result == noErr && hotkeyID.signature == hotkeySignature {
                hotkeyCallback?(hotkeyID.id)
            }
            return noErr
        }, 1, &eventType, nil, &hotkeyHandler)
        return Int32(status)
    }
}

/// Registers a hotkey. `keyCode` is a macOS virtual key code (kVK_*), `modifiers` is a mask of Carbon
/// cmdKey/shiftKey/optionKey/controlKey. Returns 0, or an OSStatus such as eventHotKeyExistsErr.
@_cdecl("sharex_hotkey_register")
public func sharex_hotkey_register(_ id: UInt32, _ keyCode: UInt32, _ modifiers: UInt32) -> Int32 {
    return onMain {
        if hotkeyRefs[id] != nil {
            return Int32(eventHotKeyExistsErr)
        }
        var reference: EventHotKeyRef?
        let status = RegisterEventHotKey(keyCode, modifiers, EventHotKeyID(signature: hotkeySignature, id: id),
                                         GetApplicationEventTarget(), 0, &reference)
        if status == noErr, let reference {
            hotkeyRefs[id] = reference
        }
        return Int32(status)
    }
}

@_cdecl("sharex_hotkey_unregister")
public func sharex_hotkey_unregister(_ id: UInt32) -> Int32 {
    return onMain {
        guard let reference = hotkeyRefs.removeValue(forKey: id) else {
            return ShareXStatus.notFound.rawValue
        }
        return Int32(UnregisterEventHotKey(reference))
    }
}
