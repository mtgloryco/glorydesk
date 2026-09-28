import { dotnet } from './_framework/dotnet.js'

const progressBar = document.getElementById("progress-bar");
const progressPercent = document.getElementById("progress-percent");
const loadingPhrase = document.getElementById("loading-phrase");

let currentPercent = 5;

const phrases = [
    "Preparing your business workspace...",
    "Loading your store catalog & items...",
    "Setting up POS checkout register...",
    "Preparing inventory & stock records...",
    "Securing your offline store vault...",
    "Configuring fast local search...",
    "Almost ready to sell..."
];

let phraseIndex = 0;
function nextPhrase() {
    if (!loadingPhrase) return;
    loadingPhrase.style.opacity = "0";
    setTimeout(() => {
        phraseIndex = (phraseIndex + 1) % phrases.length;
        loadingPhrase.textContent = phrases[phraseIndex];
        loadingPhrase.style.opacity = "1";
    }, 200);
}
const phraseTimer = setInterval(nextPhrase, 2200);

function setProgress(val, customText) {
    if (val > currentPercent) {
        currentPercent = Math.min(100, Math.round(val));
        if (progressBar) progressBar.style.width = currentPercent + "%";
        if (progressPercent) progressPercent.textContent = currentPercent + "%";
    }
    if (customText && loadingPhrase) {
        loadingPhrase.textContent = customText;
    }
}

// Initial progress bump
setProgress(10, phrases[0]);

const { setModuleImports, getAssemblyExports, getConfig, Module, runMain } = await dotnet
    .withDiagnosticTracing(false)
    .withModuleConfig({
        onDownloadResourceProgress: (loaded, total) => {
            if (total > 0) {
                // Map downloads to 15% - 85% range
                const pct = 15 + Math.round((loaded / total) * 70);
                setProgress(pct);
            }
        }
    })
    .create();

setModuleImports("main.js", {
    session: {
        get: () => {
            try {
                return localStorage.getItem("glorydesk_session");
            } catch (e) {
                console.warn("[glorydesk] Failed to read session from localStorage:", e);
                return null;
            }
        },
        set: (data) => {
            try {
                localStorage.setItem("glorydesk_session", data);
            } catch (e) {
                console.warn("[glorydesk] Failed to save session to localStorage:", e);
            }
        },
        clear: () => {
            try {
                localStorage.removeItem("glorydesk_session");
            } catch (e) {
                console.warn("[glorydesk] Failed to clear session from localStorage:", e);
            }
        }
    }
});

setProgress(88, "Securing offline store vault...");

// Mount IndexedDB to persist /GloryDesk database directory
if (Module && Module.FS) {
    const FS = Module.FS;
    const dbPath = '/GloryDesk';
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

setProgress(96, "Opening store dashboard...");

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

setProgress(100, "Ready for business!");
clearInterval(phraseTimer);

// Hide loading splash screen
const splash = document.getElementById("splash");
if (splash) {
    setTimeout(() => {
        splash.style.opacity = "0";
        setTimeout(() => splash.remove(), 500);
    }, 300);
}
