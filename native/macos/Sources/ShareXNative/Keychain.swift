// ShareX - A program that allows you to take screenshots and share any file type
// Copyright (c) 2007-2026 ShareX Team
// Licensed under the GNU General Public License v3. See LICENSE.txt in the repository root.

// Stores the random key ShareX uses to encrypt secrets in its settings files (the macOS
// counterpart of Windows DPAPI). The key never leaves the user's login keychain except into memory.

import Foundation
import Security

private func readKey(_ service: String, _ account: String) -> (Data?, OSStatus) {
    let query: [String: Any] = [
        kSecClass as String: kSecClassGenericPassword,
        kSecAttrService as String: service,
        kSecAttrAccount as String: account,
        kSecReturnData as String: true,
        kSecMatchLimit as String: kSecMatchLimitOne
    ]
    var item: CFTypeRef?
    let status = SecItemCopyMatching(query as CFDictionary, &item)
    return (item as? Data, status)
}

/// Copies the key stored under service/account into `output`, creating a random key of `length`
/// bytes first if none exists. Returns 0 or an OSStatus.
@_cdecl("sharex_keychain_get_or_create_key")
public func sharex_keychain_get_or_create_key(_ servicePointer: UnsafePointer<CChar>,
                                              _ accountPointer: UnsafePointer<CChar>,
                                              _ output: UnsafeMutablePointer<UInt8>,
                                              _ length: Int32) -> Int32 {
    let service = String(cString: servicePointer)
    let account = String(cString: accountPointer)
    let size = Int(length)
    if size <= 0 {
        return ShareXStatus.invalidArgument.rawValue
    }

    var (key, status) = readKey(service, account)

    if status == errSecItemNotFound {
        var bytes = [UInt8](repeating: 0, count: size)
        let randomStatus = SecRandomCopyBytes(kSecRandomDefault, size, &bytes)
        if randomStatus != errSecSuccess {
            return Int32(randomStatus)
        }
        let attributes: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: account,
            kSecAttrLabel as String: "ShareX settings encryption key",
            kSecAttrAccessible as String: kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly,
            kSecValueData as String: Data(bytes)
        ]
        let addStatus = SecItemAdd(attributes as CFDictionary, nil)
        if addStatus != errSecSuccess && addStatus != errSecDuplicateItem {
            return Int32(addStatus)
        }
        // Re-read so a key added concurrently by another ShareX process wins consistently.
        (key, status) = readKey(service, account)
    }

    guard status == errSecSuccess, let keyData = key else {
        return Int32(status)
    }
    guard keyData.count == size else {
        return ShareXStatus.invalidArgument.rawValue
    }
    keyData.copyBytes(to: output, count: size)
    return ShareXStatus.ok.rawValue
}
