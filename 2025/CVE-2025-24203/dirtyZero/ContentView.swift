//
//  ContentView.swift
//  dirtyZero
//
//  Created by Skadz on 5/8/25.
//

import SwiftUI
import DeviceKit
import notify

struct ContentView: View {
    let device = Device.current
    
    var body: some View {
        NavigationStack {
            VStack {
                List {
                    Section(header: HStack {
                        Image(systemName: "terminal.fill")
                        Text("Logs")
                    }) {
                        HStack {
                            Spacer()
                            ZStack {
                                LogView()
                                    .padding(3)
                                    .frame(width: 340, height: 260)
                            }
                            Spacer()
                        }
                        .onAppear(perform: {
                            print("[*] Welcome to dirtyZero!\n[*] Running on \(device.systemName!) \(device.systemVersion!), \(device.description)")
                        })
                    }
                    
                    Section(header: HStack {
                        Image(systemName: "hammer.fill")
                        Text("Tweaks")
                    }, footer: Text("All tweaks are done in memory, so if something goes wrong, you can force reboot to revert changes.\n\nExploit discovered by Ian Beer of Google Project Zero. Created by the jailbreak.party team.")) {
                        VStack {
                            HStack {
                                Button(action: {
                                    dirtyZeroHide(path: "/System/Library/PrivateFrameworks/CoreMaterial.framework/dockDark.materialrecipe")
                                    dirtyZeroHide(path: "/System/Library/PrivateFrameworks/CoreMaterial.framework/dockLight.materialrecipe")
                                }) {
                                    HStack {
                                        Image(systemName: "dock.rectangle")
                                        Text("Hide Dock")
                                    }
                                }
                                .buttonStyle(TintedButton(color: .accent, fullWidth: false))
                                
                                Button(action: {
                                    dirtyZeroHide(path: "/System/Library/PrivateFrameworks/MaterialKit.framework/Assets.car")
                                }) {
                                    HStack {
                                        Image(systemName: "line.3.horizontal")
                                        Text("Hide Home Bar")
                                    }
                                }
                                .buttonStyle(TintedButton(color: .accent, fullWidth: false))
                            }
                            
                            Button(action: {
                                Alertinator.shared.prompt(title: "Enter custom path", placeholder: "/path/to/the/file/to/hide") { path in
                                    if let _ = path, !path!.isEmpty {
                                        dirtyZeroHide(path: path!)
                                    } else {
                                        Alertinator.shared.alert(title: "Invalid path", body: "Enter an actual path to what you want to hide/zero.")
                                    }
                                }
                            }) {
                                HStack {
                                    Image(systemName: "terminal.fill")
                                    Text("Custom Path")
                                }
                            }
                            .buttonStyle(TintedButton(color: .red, fullWidth: true))
                        }
                    }
                }
            }
            .navigationTitle("dirtyZero")
        }
    }
    
    func dirtyZeroHide(path: String) {
        let args = ["permasign", path]
        var argv = args.map { strdup($0) }
        
        _ = permasign(Int32(args.count), &argv)
    }
}

// i skidded this stuff from cowabunga, sorry lemin.
struct MaterialView: UIViewRepresentable {
    let material: UIBlurEffect.Style

    init(_ material: UIBlurEffect.Style) {
        self.material = material
    }

    func makeUIView(context: Context) -> UIVisualEffectView {
        UIVisualEffectView(effect: UIBlurEffect(style: material))
    }

    func updateUIView(_ uiView: UIVisualEffectView, context: Context) {
        uiView.effect = UIBlurEffect(style: material)
    }
}

struct TintedButton: ButtonStyle {
    var color: Color
    var material: UIBlurEffect.Style?
    var fullWidth: Bool = false
    
    func makeBody(configuration: Configuration) -> some View {
        ZStack {
            if fullWidth {
                configuration.label
                    .padding(15)
                    .frame(maxWidth: .infinity)
                    .background(material == nil ? AnyView(color.opacity(0.2)) : AnyView(MaterialView(material!)))
                    .cornerRadius(8)
                    .foregroundColor(color)
            } else {
                configuration.label
                    .padding(15)
                    .background(material == nil ? AnyView(color.opacity(0.2)) : AnyView(MaterialView(material!)))
                    .cornerRadius(8)
                    .foregroundColor(color)
            }
        }
    }
    
    init(color: Color = .blue, fullWidth: Bool = false) {
        self.color = color
        self.fullWidth = fullWidth
    }
    init(color: Color = .blue, material: UIBlurEffect.Style, fullWidth: Bool = false) {
        self.color = color
        self.material = material
        self.fullWidth = fullWidth
    }
}

#Preview {
    ContentView()
}
