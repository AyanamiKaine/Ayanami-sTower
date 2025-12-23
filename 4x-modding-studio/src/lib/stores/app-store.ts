// App-wide state store using Svelte 5 runes
// This store persists the X4 data directory path in localStorage

const STORAGE_KEY = 'x4-modding-studio-config';

export interface AppConfig {
    x4DataPath: string | null;
    recentFiles: string[];
    theme: 'dark' | 'light';
}

function loadConfig(): AppConfig {
    if (typeof localStorage === 'undefined') {
        return { x4DataPath: null, recentFiles: [], theme: 'dark' };
    }

    try {
        const stored = localStorage.getItem(STORAGE_KEY);
        if (stored) {
            return JSON.parse(stored);
        }
    } catch {
        // Ignore parse errors
    }
    return { x4DataPath: null, recentFiles: [], theme: 'dark' };
}

function saveConfig(config: AppConfig) {
    if (typeof localStorage === 'undefined') return;
    localStorage.setItem(STORAGE_KEY, JSON.stringify(config));
}

// Create a reactive store
export function createAppStore() {
    let config = $state<AppConfig>(loadConfig());

    return {
        get x4DataPath() { return config.x4DataPath; },
        get recentFiles() { return config.recentFiles; },
        get theme() { return config.theme; },

        setX4DataPath(path: string | null) {
            config = { ...config, x4DataPath: path };
            saveConfig(config);
        },

        addRecentFile(path: string) {
            const recent = [path, ...config.recentFiles.filter(f => f !== path)].slice(0, 10);
            config = { ...config, recentFiles: recent };
            saveConfig(config);
        },

        clearRecentFiles() {
            config = { ...config, recentFiles: [] };
            saveConfig(config);
        },

        setTheme(theme: 'dark' | 'light') {
            config = { ...config, theme };
            saveConfig(config);
        }
    };
}

// Singleton instance
let appStoreInstance: ReturnType<typeof createAppStore> | null = null;

export function getAppStore() {
    if (!appStoreInstance) {
        appStoreInstance = createAppStore();
    }
    return appStoreInstance;
}
