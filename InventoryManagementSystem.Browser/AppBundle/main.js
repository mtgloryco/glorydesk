import { dotnet } from './_framework/dotnet.js'

const { setModuleImports, getAssemblyExports, getConfig, Module, runMain } = await dotnet
    .withDiagnosticTracing(false)
    .create();

const config = getConfig();

// Mount IndexedDB to persist /InventoryManagementSystem database directory
if (Module && Module.FS) {
    const FS = Module.FS;
    const dbPath = '/InventoryManagementSystem';
    try {
        FS.mkdir(dbPath);
    } catch (e) {
        // Directory might already exist
    }
    
    console.log("Mounting IndexedDB (IDBFS) on virtual folder: " + dbPath);
    FS.mount(FS.filesystems.IDBFS, {}, dbPath);

    // Sync from browser IndexedDB to Emscripten virtual file system
    await new Promise((resolve) => {
        FS.syncfs(true, (err) => {
            if (err) {
                console.error("Error loading files from IndexedDB:", err);
            } else {
                console.log("Files loaded from IndexedDB successfully.");
            }
            resolve();
        });
    });

    // Auto-save the virtual filesystem to IndexedDB every 3 seconds
    setInterval(() => {
        FS.syncfs(false, (err) => {
            if (err) {
                console.error("Error auto-saving files to IndexedDB:", err);
            }
        });
    }, 3000);
} else {
    console.warn("Emscripten FS not available. Database files will not persist.");
}

// Start the Avalonia application. Use runMain() rather than dotnet.run() (a shorthand for
// runMainAndExit()): Avalonia's browser backend installs persistent requestAnimationFrame /
// input callbacks and keeps running after Main()'s Task completes. runMainAndExit() tears the
// whole WASM runtime down the instant that Task finishes, so those callbacks then try to call
// back into an already-exited runtime — observed as "MONO_WASM: Assert failed: .NET runtime
// already exited" cascading into a fatal "Uncaught RuntimeError: unreachable" a few seconds
// after every page load, regardless of what happened during startup.
//
// Deliberately not awaited: Program.Main's Task (StartBrowserAppAsync) runs Avalonia's UI loop
// and is designed to stay pending for the app's whole lifetime, same as desktop's Run(). Code
// after "await runMain()" would never execute, so the splash below is removed once the app is
// kicked off rather than once Main "finishes" (it never does).
runMain().catch((err) => console.error("Fatal error starting the application:", err));

// Hide loading splash screen
const splash = document.getElementById("splash");
if (splash) {
    splash.style.opacity = "0";
    setTimeout(() => splash.remove(), 500);
}
