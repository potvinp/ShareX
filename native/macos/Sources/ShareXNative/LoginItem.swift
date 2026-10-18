// ShareX - A program that allows you to take screenshots and share any file type
// Copyright (c) 2007-2026 ShareX Team
// Licensed under the GNU General Public License v3. See LICENSE.txt in the repository root.

// "Open at login" via SMAppService, which lists ShareX under System Settings > General > Login Items.

import Foundation
import ServiceManagement

/// 0 = not registered, 1 = enabled, 2 = registered but switched off by the user in Login Items.
@_cdecl("sharex_login_item_status")
public func sharex_login_item_status() -> Int32 {
    switch SMAppService.mainApp.status {
    case .enabled:
        return 1
    case .requiresApproval:
        return 2
    default:
        return 0
    }
}

/// Registers or unregisters ShareX as a login item. Returns 0, or the NSError code on failure.
@_cdecl("sharex_login_item_set")
public func sharex_login_item_set(_ enabled: Int32) -> Int32 {
    do {
        if enabled != 0 {
            try SMAppService.mainApp.register()
        } else {
            try SMAppService.mainApp.unregister()
        }
        return ShareXStatus.ok.rawValue
    } catch {
        return Int32(truncatingIfNeeded: (error as NSError).code)
    }
}

/// Opens System Settings > General > Login Items.
@_cdecl("sharex_open_login_items_settings")
public func sharex_open_login_items_settings() {
    SMAppService.openSystemSettingsLoginItems()
}
