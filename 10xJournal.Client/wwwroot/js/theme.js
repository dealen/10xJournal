// Theme utility functions for 10xJournal
// Provides secure JS interop for theme management without using eval

window.appTheme = {
    /**
     * Sets the theme attribute on the document root element
     * @param {string} theme - Either 'light' or 'dark'
     */
    setTheme: (theme) => {
        document.documentElement.setAttribute('data-theme', theme);
    },
    
    /**
     * Checks if the user's system prefers dark mode
     * @returns {boolean} - True if dark mode is preferred, false otherwise
     */
    getSystemPreference: () => {
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    }
};
